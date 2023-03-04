using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch]
internal static class Patch_RimWorld_RaidStrategyWorker
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RaidStrategyWorker_Siege), "CanUseWith");
        yield return AccessTools.Method(typeof(RaidStrategyWorker_SiegeMechanoid), "CanUseWith");
        yield return AccessTools.Method(typeof(RaidStrategyWorker_StageThenAttack), "CanUseWith");
    }

    [HarmonyPostfix]
    private static void CanUseWith(IncidentParms parms, ref bool __result)
    {
        if (parms.target is { Tile: >= 0 } && Find.WorldGrid[parms.target.Tile].hilliness == Hilliness.Impassable)
        {
            __result = false;
        }
    }
}
