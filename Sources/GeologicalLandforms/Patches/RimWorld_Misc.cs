using System;
using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

[HarmonyPatch]
internal static class RimWorld_Misc
{
    private static List<Action> _onMainMenu = new();

    public static void OnMainMenu(Action action)
    {
        _onMainMenu.Add(action);
    }
    
    [HarmonyPatch(typeof(MainMenuDrawer))]
    [HarmonyPatch(nameof(MainMenuDrawer.Init))]
    private static void Postfix()
    {
        RunOnMainMenuNow();
    }

    public static void RunOnMainMenuNow()
    {
        _onMainMenu.ForEach(e => e.Invoke());
        _onMainMenu.Clear();
    }
    
    [HarmonyPatch(typeof(LearningReadout))]
    [HarmonyPatch(nameof(LearningReadout.LearningReadoutOnGUI))]
    private static bool Prefix()
    {
        return !LandformGraphEditor.IsEditorOpen;
    }
}