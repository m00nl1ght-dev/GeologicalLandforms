using HarmonyLib;
using Verse;

// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(Game))]
public class RimWorld_Game
{
    [HarmonyPatch("AddMap")]
    [HarmonyPostfix]
    private static void AddMap(Map map)
    {
        var world = Find.World;
        if (world == null || map == null || map.Tile < 0) return;
        var worldTileInfo = WorldTileInfo.Get(map.Tile);
        world.LandformData()?.Commit(map.Tile, worldTileInfo, true);
    }
}