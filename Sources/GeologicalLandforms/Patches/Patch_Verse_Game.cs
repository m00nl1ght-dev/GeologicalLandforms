using System;
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
                if (map.BiomeGrid() is { Enabled: false } && map.Tile >= 0 && landformData.HasData(map.Tile))
                {
                    GenStep_BiomeVariants.RegenerateBiomeGrid(map);
                }
            }
            catch (Exception e)
            {
                GeologicalLandformsAPI.Logger.Warn($"Failed to restore biome grid for map on tile {map.Tile}", e);
            }
        }
    }
}
