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
        if (world == null || map == null) return;

        if (!world.HasFinishedGenerating())
        {
            GeologicalLandformsAPI.Logger.Warn("Map is being added before world was finalized!");
            return;
        }

        try
        {
            var tileInfo = WorldTileInfo.Get(map, false);

            if (map.Tile < 0)
            {
                GeologicalLandformsAPI.Logger.Log($"Map added - {tileInfo}");
                return;
            }

            var landformData = world.LandformData();

            if (landformData == null)
            {
                GeologicalLandformsAPI.Logger.Warn("World is missing LandformData component!");
                return;
            }

            var storedData = landformData.HasData(map.Tile);

            GeologicalLandformsAPI.Logger.Log($"Map added on tile - {tileInfo} ({(storedData ? "L" : "F")})");
            landformData.Commit(map.Tile, new LandformData.TileData(tileInfo));
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Error while getting landform data for new map", e);
        }
    }
}
