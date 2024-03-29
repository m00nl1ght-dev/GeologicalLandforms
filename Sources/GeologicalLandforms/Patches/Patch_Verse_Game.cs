using System;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Game))]
internal static class Patch_Verse_Game
{
    [HarmonyPostfix]
    [HarmonyPatch("AddMap")]
    private static void AddMap(Map map)
    {
        var world = Find.World;
        if (world == null || map == null || map.Tile < 0) return;

        try
        {
            var landformData = world.LandformData();

            if (landformData == null)
            {
                GeologicalLandformsAPI.Logger.Warn("World is missing LandformData component!");
                return;
            }

            if (!world.HasFinishedGenerating())
            {
                GeologicalLandformsAPI.Logger.Warn("Map is being added before world was finalized!");
                return;
            }

            var worldTileInfo = WorldTileInfo.Get(map.Tile, false);
            var storedData = landformData.HasData(map.Tile);

            GeologicalLandformsAPI.Logger.Log($"Map added on tile - {worldTileInfo} ({(storedData ? "L" : "F")})");
            landformData.Commit(map.Tile, new LandformData.TileData(worldTileInfo));
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Error while getting landform data for new map", e);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("LoadGame")]
    private static void LoadGame()
    {
        var landformData = Current.Game.World.LandformData();
        if (landformData == null) return;

        foreach (var map in Current.Game.Maps)
        {
            try
            {
                var biomeGrid = map.BiomeGrid();
                var biomeProps = map.Biome.Properties();

                if (biomeGrid is { Enabled: false } && map.Tile >= 0 && landformData.HasData(map.Tile))
                {
                    var tile = WorldTileInfo.Get(map.Tile, false);

                    if (biomeProps.applyToCaves) biomeGrid.Enabled = true;

                    if (tile.HasLandforms && tile.Landforms.Any(lf => lf.OutputBiomeGrid != null))
                    {
                        Landform.PrepareMapGen(map);

                        var mapSize = Landform.GeneratingMapSize;
                        var biomeFunc = Landform.GetFeatureScaled(l => l.OutputBiomeGrid?.GetBiomeGrid());

                        bool hasBiomeTransition = false;

                        if (tile.HasBorderingBiomes)
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
                                BiomeTransition.PostProcessBiomeGrid(biomeGrid, tile, mapSize);
                            }
                        }

                        Landform.CleanUp();
                    }

                    if (tile.HasBiomeVariants && biomeProps.AllowBiomeTransitions)
                    {
                        var layers = tile.BiomeVariants.SelectMany(v => v.layers).OrderByDescending(l => l.priority).ToList();

                        GeologicalLandformsAPI.Logger.Log($"Restoring biome variants for map on tile {map.Tile}");

                        biomeGrid.ApplyVariantLayers(layers);
                        biomeGrid.Enabled = true;
                    }
                }
            }
            catch (Exception e)
            {
                GeologicalLandformsAPI.Logger.Warn($"Failed to restore biome grid for map on tile {map.Tile}", e);
            }
        }
    }
}
