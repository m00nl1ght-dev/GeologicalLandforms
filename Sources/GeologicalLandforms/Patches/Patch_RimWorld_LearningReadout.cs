using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

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
