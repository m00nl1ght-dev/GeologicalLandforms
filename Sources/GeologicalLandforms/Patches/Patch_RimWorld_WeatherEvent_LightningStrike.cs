using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

/// <summary>
/// Reduce frequency of lightning strikes on impassable world tiles.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(WeatherEvent_LightningStrike))]
internal static class Patch_RimWorld_WeatherEvent_LightningStrike
{
    [HarmonyPrefix]
    [HarmonyPatch("FireEvent")]
    private static bool FireEvent(Map ___map)
    {
        return ___map.TileInfo.hilliness != Hilliness.Impassable || Rand.Value < 0.3f;
    }
}
