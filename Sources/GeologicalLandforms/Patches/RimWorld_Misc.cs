using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

/// <summary>
/// Collection of small misc patches across the RimWorld codebase.
/// </summary>
[HarmonyPatch]
internal static class RimWorld_Misc
{
    [HarmonyPatch(typeof(LearningReadout))]
    [HarmonyPatch(nameof(LearningReadout.LearningReadoutOnGUI))]
    private static bool Prefix()
    {
        return !LandformGraphEditor.IsEditorOpen;
    }
}