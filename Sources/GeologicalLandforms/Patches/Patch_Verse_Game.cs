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
        var worldTileInfo = WorldTileInfo.Get(map.Tile);
        world.LandformData()?.Commit(map.Tile, worldTileInfo, true);
    }
}
