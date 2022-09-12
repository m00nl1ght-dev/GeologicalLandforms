using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

[HarmonyPatch]
internal static class RimWorld_GenStep_Scatterer
{
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(GenStep_ScatterLumpsMineable), nameof(GenStep_ScatterLumpsMineable.Generate))]
    private static void Prefix(Map map, GenStepParams parms, ref FloatRange ___countPer10kCellsRange)
    {
        if (!Landform.AnyGenerating) return;

        double? scatterAmount = Landform.GetFeature(l => l.OutputScatterers?.GetMineables());
        if (scatterAmount.HasValue)
        {
            ___countPer10kCellsRange = new FloatRange((float) scatterAmount.Value, (float) scatterAmount.Value);
        }
    }
}