using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

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