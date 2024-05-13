using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using RimWorld;
using Verse;

namespace GeologicalLandforms;

public class GenStep_BiomeVariants : GenStep
{
    public override int SeedPart => 27405;

    public override void Generate(Map map, GenStepParams parms)
    {
        var biomeGrid = map.BiomeGrid();
        var props = map.Biome.Properties();

        if (biomeGrid == null) return;

        if (props.applyToCaves) biomeGrid.Enabled = true;

        var tileInfo = Landform.GeneratingTile;

        if (tileInfo != null)
        {
            try
            {
                var layers = LayersFor(tileInfo, props);

                if (layers.Count > 0)
                {
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
                            var ctx = new CtxMapGenCell(pos);

                            if (conditions == null || conditions.Get(ctx))
                            {
                                var terrainDef = overrides.Get(ctx);
                                if (terrainDef != null) map.terrainGrid.SetTerrain(pos, terrainDef);
                            }
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

        LandformGraphEditor.ActiveEditor?.Close();

        var tile = WorldTileInfo.Get(map, false);

        if (biomeProps.applyToCaves) biomeGrid.Enabled = true;

        var layers = LayersFor(tile, biomeProps);

        if (tile.HasLandforms() || layers.Count > 0)
        {
            try
            {
                Landform.Prepare(map);
                MapGenerator.data.Clear();
                MapGenerator.mapBeingGenerated = map;
                Rand.PushState();

                var seed = SeedRerollData.GetMapSeed(Find.World, map.Tile);
                var genStep = new GenStep_ElevationFertility();
                Rand.Seed = Gen.HashCombineInt(seed, genStep.SeedPart);
                genStep.Generate(map, new GenStepParams());

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

                if (layers.Count > 0)
                {
                    GeologicalLandformsAPI.Logger.Log($"Restoring biome layers for map on tile {map.Tile}");

                    biomeGrid.ApplyVariantLayers(layers);
                    biomeGrid.Enabled = true;
                }
            }
            finally
            {
                Rand.PopState();
                MapGenerator.mapBeingGenerated = null;
                MapGenerator.data.Clear();
                Landform.CleanUp();
            }
        }
    }

    private static List<BiomeVariantLayer> LayersFor(IWorldTileInfo tileInfo, BiomeProperties biomeProps)
    {
        var layers = new List<BiomeVariantLayer>();

        if (biomeProps.biomeLayers != null)
            layers.AddRange(biomeProps.biomeLayers);

        if (tileInfo.HasBiomeVariants())
            layers.AddRange(tileInfo.BiomeVariants.SelectMany(v => v.layers));

        layers.SortByDescending(l => l.priority);

        return layers;
    }
}
