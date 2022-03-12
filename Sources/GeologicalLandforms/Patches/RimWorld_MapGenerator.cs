using GeologicalLandforms.TerrainGraph;
using HarmonyLib;
using RimWorld;
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
    }
    
    [HarmonyPatch(nameof(MapGenerator.GenerateContentsIntoMap))]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Map map, int seed)
    {
        Landform.CleanUpMapGen();
    }
}