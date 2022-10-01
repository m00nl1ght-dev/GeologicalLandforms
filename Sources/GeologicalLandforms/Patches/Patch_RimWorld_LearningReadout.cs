using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(LearningReadout))]
internal static class Patch_RimWorld_LearningReadout
{
    [HarmonyPrefix]
    [HarmonyPatch("LearningReadoutOnGUI")]
    private static bool LearningReadoutOnGUI()
    {
        return !LandformGraphEditor.IsEditorOpen;
    }
}