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
internal static class Patch_RimWorld_IncidentWorker
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(IncidentWorker_MechCluster), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_HerdMigration), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_FarmAnimalsWanderIn), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_MeteoriteImpact), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_ShipChunkDrop), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_TravelerGroup), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_VisitorGroup), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_NeutralGroup), "CanFireNowSub");
        yield return AccessTools.Method(typeof(IncidentWorker_Ambush), "CanFireNowSub");
    }

    [HarmonyPostfix]
    private static void CanFireNowSub(IncidentParms parms, ref bool __result)
    {
        if (parms.target is { Tile: >= 0 } && Find.WorldGrid[parms.target.Tile].hilliness == Hilliness.Impassable)
        {
            __result = false;
        }
    }
}
