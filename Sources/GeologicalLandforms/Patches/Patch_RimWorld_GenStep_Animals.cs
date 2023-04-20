using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Animals))]
internal static class Patch_RimWorld_GenStep_Animals
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.High)]
    private static void Generate_Prefix(Map map)
    {
        map.BiomeGrid()?.UpdateOpenGroundFraction();
    }
}
