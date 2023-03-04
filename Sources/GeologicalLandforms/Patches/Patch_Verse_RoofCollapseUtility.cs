using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RoofCollapseUtility))]
internal static class Patch_Verse_RoofCollapseUtility
{
    [HarmonyPrefix]
    [HarmonyPatch("WithinRangeOfRoofHolder")]
    private static bool WithinRangeOfRoofHolder(IntVec3 c, Map map, ref bool __result)
    {
        if (c.GetRoof(map) is not { isThickRoof: true } || !map.HasStableCaveRoofs()) return true;
        __result = true;
        return false;
    }
}
