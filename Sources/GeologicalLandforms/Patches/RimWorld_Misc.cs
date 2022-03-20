using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

[HarmonyPatch]
internal static class RimWorld_Misc
{
    public static int LastKnownInitialWorldSeed { get; private set; }
    
    [HarmonyPatch(typeof(MainMenuDrawer))]
    [HarmonyPatch(nameof(MainMenuDrawer.Init))]
    private static void Postfix()
    {
        if (Prefs.DevMode) Find.WindowStack.Add(new LandformGraphEditor());
    }
    
    [HarmonyPatch(typeof(LearningReadout))]
    [HarmonyPatch(nameof(LearningReadout.LearningReadoutOnGUI))]
    private static bool Prefix()
    {
        return !LandformGraphEditor.IsEditorOpen;
    }
    
    [HarmonyPatch(typeof(World))]
    [HarmonyPatch(nameof(World.ConstructComponents))]
    private static void Postfix(WorldInfo ___info)
    {
        LastKnownInitialWorldSeed = ___info.Seed;
    }
}