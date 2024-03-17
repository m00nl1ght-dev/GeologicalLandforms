using System;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using Verse;

namespace GeologicalLandforms;

public class BiomeVariantsGenStep : GenStep
{
    public static readonly GenStepDef Def = new() { genStep = new BiomeVariantsGenStep(), order = 225 };

    public override int SeedPart => 27405;

    public override void Generate(Map map, GenStepParams parms)
    {
        var biomeGrid = map.BiomeGrid();
        var props = map.Biome.Properties();

        if (biomeGrid == null) return;

        if (props.applyToCaves) biomeGrid.Enabled = true;
        if (!props.AllowBiomeTransitions) return;

        if (Landform.GeneratingTile is WorldTileInfo { HasBiomeVariants: true } tile)
        {
            try
            {
                var layers = Landform.GeneratingTile.BiomeVariants.SelectMany(v => v.layers).OrderByDescending(l => l.priority).ToList();

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
                        var ctx = new CtxMapCell(tile, map, pos);

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
}
