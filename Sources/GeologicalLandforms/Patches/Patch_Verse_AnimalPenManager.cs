#if !RW_1_6_OR_GREATER

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(AnimalPenManager))]
internal static class Patch_Verse_AnimalPenManager
{
    [HarmonyPrefix]
    [HarmonyPatch("GetFixedAutoCutFilter")]
    [HarmonyPriority(Priority.VeryLow)]
    private static bool GetFixedAutoCutFilter_Prefix(AnimalPenManager __instance, Map ___map, ref ThingFilter __result)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = GetFixedAutoCutFilter_LocalBiomeAware(__instance, biomeGrid);
        return false;
    }

    [HarmonyPatch("GetFixedAutoCutFilter")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static ThingFilter GetFixedAutoCutFilter_LocalBiomeAware(AnimalPenManager instance, BiomeGrid biomeGrid)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pattern = TranspilerPattern.Build("AllWildPlants")
                .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldarg_1)
                .MatchLoad(typeof(AnimalPenManager), "map").Remove()
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .MatchCall(typeof(BiomeDef), "get_AllWildPlants").Remove()
                .Insert(CodeInstruction.Call(typeof(BiomeGrid), "get_AllPotentialPlants"));

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
        return null;
    }
}

#endif
