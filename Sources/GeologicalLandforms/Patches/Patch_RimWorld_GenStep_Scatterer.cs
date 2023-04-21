using System;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Scatterer))]
internal static class Patch_RimWorld_GenStep_Scatterer
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.High)]
    private static void Generate_Prefix(GenStep_Scatterer __instance, Map map, ref State __state)
    {
        if (Landform.AnyGeneratingNonLayer)
        {
            __state.hasData = true;

            __state.minEdgeDist = __instance.minEdgeDist;
            __state.minEdgeDistPct = __instance.minEdgeDistPct;

            __state.minDistToPlayerStart = __instance.minDistToPlayerStart;
            __state.minDistToPlayerStartPct = __instance.minDistToPlayerStartPct;

            __instance.minEdgeDist = Math.Min(__instance.minEdgeDist, 10);
            __instance.minEdgeDistPct = Math.Min(__instance.minEdgeDistPct, 0.1f);

            __instance.minDistToPlayerStart = 0;
            __instance.minDistToPlayerStartPct = 0f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.Low)]
    private static void Generate_Postfix(GenStep_Scatterer __instance, Map map, State __state)
    {
        if (__state.hasData)
        {
            __instance.minEdgeDist = __state.minEdgeDist;
            __instance.minEdgeDistPct = __state.minEdgeDistPct;

            __instance.minDistToPlayerStart = __state.minDistToPlayerStart;
            __instance.minDistToPlayerStartPct = __state.minDistToPlayerStartPct;
        }
    }

    private struct State
    {
        public bool hasData;

        public int minEdgeDist;
        public float minEdgeDistPct;

        public int minDistToPlayerStart;
        public float minDistToPlayerStartPct;
    }
}
