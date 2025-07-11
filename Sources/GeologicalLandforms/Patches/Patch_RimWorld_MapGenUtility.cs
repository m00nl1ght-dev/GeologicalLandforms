#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapGenUtility))]
internal static class Patch_RimWorld_MapGenUtility
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(MapGenUtility.BiomeAt))]
    private static bool BiomeAt_Prefix(Map map, IntVec3 c, ref BiomeDef __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = biomeGrid.BiomeAt(c);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(MapGenUtility.IsMixedBiome))]
    private static bool IsMixedBiome_Prefix(Map map, ref bool __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = biomeGrid.Entries.Count > 1;
        return false;
    }
}

#endif
