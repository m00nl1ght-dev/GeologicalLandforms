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

        if (landformData.TryGet(map.Tile, out var entry) && entry.Locked)
        {
            GeologicalLandformsAPI.Logger.Log("Map is being added on tile with existing landform " + entry.LandformId);
            return;
        }

        var worldTileInfo = WorldTileInfo.Get(map.Tile, false);
        landformData.Commit(map.Tile, worldTileInfo, true);
    }
}
