#if RW_1_6_OR_GREATER

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Map))]
internal static class Patch_Verse_Map
{
    [HarmonyPrefix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch(nameof(Map.Biomes), MethodType.Getter)]
    private static bool GetBiomes_Prefix(Map __instance, ref IEnumerable<BiomeDef> __result)
    {
        var biomeGrid = __instance.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = biomeGrid.Entries.Select(e => e.Biome);
        return false;
    }
}

#endif
