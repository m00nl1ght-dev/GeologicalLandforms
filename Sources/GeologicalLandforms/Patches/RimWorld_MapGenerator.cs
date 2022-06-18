using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(MapGenerator))]
internal static class RimWorld_MapGenerator
{
    [HarmonyPatch(nameof(MapGenerator.GenerateContentsIntoMap))]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Map map, int seed)
    {
        Landform.PrepareMapGen(map);
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    [HarmonyPatch(nameof(MapGenerator.GenerateContentsIntoMap))]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Map map, int seed)
    {
        Landform.CleanUp();
        RimWorld_TerrainPatchMaker.Reset();
        map.GetComponent<BiomeGrid>()?.UpdateOpenGroundFraction();
    }
}