using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapGenerator))]
internal static class Patch_Verse_MapGenerator
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    [HarmonyPriority(Priority.First)]
    private static void GenerateContentsIntoMap_Prefix(Map map, int seed)
    {
        Landform.PrepareMapGen(map);
    }

    [HarmonyPostfix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    [HarmonyPriority(Priority.Last)]
    private static void GenerateContentsIntoMap_Postfix(Map map, int seed)
    {
        Landform.CleanUp();
        map.BiomeGrid()?.UpdateOpenGroundFraction();
    }
}
