using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_CaveHives))]
internal static class Patch_RimWorld_GenStep_CaveHives
{
    private const float VanillaCaveCellsPerHive = 1000f;

    internal static readonly Type Self = typeof(Patch_RimWorld_GenStep_CaveHives);

    [HarmonyTranspiler]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryLow)]
    private static IEnumerable<CodeInstruction> Generate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("AdjustCaveHiveCount")
            .MatchLdloc().Keep()
            .Match(OpCodes.Conv_R4).Keep()
            .Match(OpCodes.Ldc_R4).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(CaveCellsPerHive)))
            .Match(OpCodes.Div).Keep()
            .Match(OpCodes.Call).Keep();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static float CaveCellsPerHive()
    {
        if (!Landform.AnyGenerating) return VanillaCaveCellsPerHive;

        double? value = Landform.GetFeature(l => l.OutputScatterers?.GetCaveHives());
        return value.HasValue ? VanillaCaveCellsPerHive / (float) value.Value : VanillaCaveCellsPerHive;
    }
}
