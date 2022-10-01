using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_ScatterLumpsMineable))]
internal static class Patch_RimWorld_GenStep_ScatterLumpsMineable
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void Generate(Map map, GenStepParams parms, ref FloatRange ___countPer10kCellsRange)
    {
        if (!Landform.AnyGenerating) return;

        double? scatterAmount = Landform.GetFeature(l => l.OutputScatterers?.GetMineables());
        if (scatterAmount.HasValue)
        {
            ___countPer10kCellsRange = new FloatRange((float) scatterAmount.Value, (float) scatterAmount.Value);
        }
    }
}