using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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