using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (MainMenuDrawer))]
internal static class RimWorld_Debug
{
    [HarmonyPatch(nameof(MainMenuDrawer.Init))]
    private static void Postfix()
    {
        Find.WindowStack.Add(new LandformGraphEditor());
    }
}