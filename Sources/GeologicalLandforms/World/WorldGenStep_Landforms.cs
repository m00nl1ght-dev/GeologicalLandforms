using System.Diagnostics;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;
using static GeologicalLandforms.BiomeProperties;

namespace GeologicalLandforms;

public class WorldGenStep_Landforms : WorldGenStep
{
    public override int SeedPart => 494807164;

    public override void GenerateFresh(string seed)
    {
        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) return;

        WorldTileInfo.CreateNewCache();

        var world = Find.World;
        var grid = world.grid;

        if (world.worldObjects == null) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < grid.TilesCount; ++tileIdx)
        {
            var info = WorldTileInfo.Get(tileIdx);
            if (info.Biome == null) return;

            var tile = grid[tileIdx];

            var fromBiome = info.Biome.Properties().worldTileOverrides;
            if (fromBiome != null) ApplyOverrides(tile, info, fromBiome);

            if (info.HasBiomeVariants())
            {
                foreach (var biomeVariant in info.BiomeVariants)
                {
                    var fromBiomeVariant = biomeVariant.worldTileOverrides;
                    if (fromBiomeVariant != null) ApplyOverrides(tile, info, fromBiomeVariant);
                }
            }
        }

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Calculation of initial world tile data took " + stopwatch.ElapsedMilliseconds + " ms.");
    }

    public override void GenerateFromScribe(string seed)
    {
        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) return;

        WorldTileInfo.CreateNewCache();
    }

    private void ApplyOverrides(Tile tile, IWorldTileInfo info, WorldTileOverrides overrides)
    {
        var ctx = new CtxTile(info);

        overrides.elevation?.Apply(ctx, ref tile.elevation);
        overrides.temperature?.Apply(ctx, ref tile.temperature);
        overrides.rainfall?.Apply(ctx, ref tile.rainfall);

        if (ModLister.BiotechInstalled)
        {
            overrides.pollution?.Apply(ctx, ref tile.pollution);
        }
    }

    internal static void Register()
    {
        var def = new WorldGenStepDef
        {
            defName = "GL_InitLandformData",
            modContentPack = GeologicalLandformsAPI.LunarAPI.Component.LatestVersionProvidedBy.ModContentPack,
            worldGenStep = new WorldGenStep_Landforms(),
            order = 5000
        };

        DefGenerator.AddImpliedDef(def);
    }
}
