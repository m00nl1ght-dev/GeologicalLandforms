using System;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using Verse;

namespace GeologicalLandforms;

public class GenStep_BiomeVariants : GenStep
{
    public static readonly GenStepDef Def = new() { genStep = new GenStep_BiomeVariants(), order = 225 }; // TODO PostLoad is not called

    public override int SeedPart => 27405;

    public override void Generate(Map map, GenStepParams parms)
    {
        var biomeGrid = map.BiomeGrid();
        var props = map.Biome.Properties();

        if (biomeGrid == null) return;

        if (props.applyToCaves) biomeGrid.Enabled = true;
        if (!props.AllowBiomeTransitions) return;

        var tileInfo = Landform.GeneratingTile;

        if (tileInfo != null && tileInfo.HasBiomeVariants())
        {
            try
            {
                var layers = tileInfo.BiomeVariants.SelectMany(v => v.layers).OrderByDescending(l => l.priority).ToList();

                if (!MapPreviewAPI.IsGeneratingPreview)
                {
                    biomeGrid.ApplyVariantLayers(layers);
                    biomeGrid.Enabled = true;
                }

                foreach (var layer in layers.Where(layer => layer.terrainOverrides != null))
                {
                    var conditions = layer.mapGridConditions;
                    var overrides = layer.terrainOverrides;

                    foreach (var pos in map.AllCells)
                    {
                        var ctx = new CtxMapCell(tileInfo, map, pos);

                        if (conditions == null || conditions.Get(ctx))
                        {
                            var terrainDef = overrides.Get(ctx);
                            if (terrainDef != null) map.terrainGrid.SetTerrain(pos, terrainDef);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                GeologicalLandformsAPI.Logger.Error("Failed to apply biome variants!", e);
            }
        }
    }

    public static void RegenerateBiomeGrid(Map map)
    {
        var biomeGrid = map.BiomeGrid();
        var biomeProps = map.Biome.Properties();

        biomeGrid.Clear();

        var tile = WorldTileInfo.Get(map, false);

        if (biomeProps.applyToCaves) biomeGrid.Enabled = true;

        if (tile.HasLandforms() && tile.Landforms.Any(lf => lf.OutputBiomeGrid != null))
        {
            Landform.PrepareMapGen(map);

            var mapSize = Landform.GeneratingMapSize;
            var biomeFunc = Landform.GetFeatureScaled(l => l.OutputBiomeGrid?.GetBiomeGrid());

            bool hasBiomeTransition = false;

            if (tile.HasBorderingBiomes())
            {
                var baseFunc = biomeFunc;
                var transition = Landform.GetFeature(l => l.OutputBiomeGrid?.ApplyBiomeTransitions(tile, mapSize, baseFunc));
                if (transition != null)
                {
                    biomeFunc = transition;
                    hasBiomeTransition = true;
                }
            }

            if (biomeFunc != null)
            {
                biomeGrid.Enabled = true;
                biomeGrid.SetBiomes(biomeFunc);

                GeologicalLandformsAPI.Logger.Log($"Restoring biome grid for map on tile {map.Tile}");

                if (hasBiomeTransition)
                {
                    BiomeTransition.PostProcessBiomeGrid(biomeGrid, mapSize);
                }
            }

            Landform.CleanUp();
        }

        if (tile.HasBiomeVariants() && biomeProps.AllowBiomeTransitions)
        {
            var layers = tile.BiomeVariants.SelectMany(v => v.layers).OrderByDescending(l => l.priority).ToList();

            GeologicalLandformsAPI.Logger.Log($"Restoring biome variants for map on tile {map.Tile}");

            biomeGrid.ApplyVariantLayers(layers);
            biomeGrid.Enabled = true;
        }
    }
}
