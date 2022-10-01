using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(World))]
internal static class Patch_RimWorld_World
{
    public static int LastKnownInitialWorldSeed { get; private set; }
    
    [HarmonyPostfix]
    [HarmonyPatch("ConstructComponents")]
    private static void ConstructComponents(WorldInfo ___info)
    {
        LastKnownInitialWorldSeed = ___info.Seed;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.HasCaves))]
    [HarmonyAfter("zylle.MapDesigner")]
    private static bool HasCaves(ref bool __result, int tile)
    {
        var worldTileInfo = WorldTileInfo.Get(tile);
        var landform = worldTileInfo.Landforms?.FirstOrDefault(l => !l.IsLayer);
        if (landform == null) return true;

        int seed = worldTileInfo.MakeSeed(8266);
        __result = Rand.ChanceSeeded(landform.WorldTileReq.CaveChance, seed);
        return false;
    }
}