using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RaidStrategyWorker))]
internal static class Patch_RimWorld_RaidStrategyWorker
{
    [HarmonyPostfix]
    [HarmonyPatch("CanUseWith")]
    [HarmonyPriority(Priority.Low)]
    private static void CanUseWith(RaidStrategyWorker __instance, IncidentParms parms, ref bool __result)
    {
        if (__result && parms.target != null && parms.target.Tile >= 0)
        {
            var tileInfo = WorldTileInfo.Get(parms.target.Tile);
            if (tileInfo.HasLandforms())
            {
                foreach (var landform in tileInfo.Landforms)
                {
                    var mapIncidents = landform.MapIncidents;
                    if (mapIncidents != null && !mapIncidents.CanUseRaidStrategyNow(__instance))
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
