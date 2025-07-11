#if !RW_1_6_OR_GREATER

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Command_SetPlantToGrow))]
internal static class Patch_Verse_Command_SetPlantToGrow
{
    [HarmonyPrefix]
    [HarmonyPatch("IsPlantAvailable")]
    [HarmonyPriority(Priority.VeryLow)]
    private static bool IsPlantAvailable_Prefix(ThingDef plantDef, Map map, ref bool __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = IsPlantAvailable_LocalBiomeAware(plantDef, map, biomeGrid);
        return false;
    }

    [HarmonyPatch("IsPlantAvailable")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    internal static bool IsPlantAvailable_LocalBiomeAware(ThingDef plantDef, Map map, BiomeGrid biomeGrid)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pattern = TranspilerPattern.Build("AllWildPlants")
                .Match(OpCodes.Ldarg_1).Replace(OpCodes.Ldarg_2)
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .MatchCall(typeof(BiomeDef), "get_AllWildPlants").Remove()
                .Insert(CodeInstruction.Call(typeof(BiomeGrid), "get_AllPotentialPlants"));

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
        return false;
    }
}

#endif
