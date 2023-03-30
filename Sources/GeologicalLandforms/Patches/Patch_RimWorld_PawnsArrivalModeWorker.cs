using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(PawnsArrivalModeWorker))]
internal static class Patch_RimWorld_PawnsArrivalModeWorker
{
    [HarmonyPostfix]
    [HarmonyPatch("CanUseWith")]
    [HarmonyPriority(Priority.Low)]
    private static void CanUseWith(PawnsArrivalModeWorker __instance, IncidentParms parms, ref bool __result)
    {
        if (__result && parms.target is { Tile: >= 0 })
        {
            var tileInfo = WorldTileInfo.Get(parms.target.Tile);
            if (tileInfo.HasLandforms)
            {
                foreach (var landform in tileInfo.Landforms)
                {
                    var mapIncidents = landform.MapIncidents;
                    if (mapIncidents != null && !mapIncidents.CanUseArrivalModeNow(__instance))
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
