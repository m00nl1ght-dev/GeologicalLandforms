using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(TerrainPatchMaker), "Init")]
internal static class RimWorld_TerrainPatchMaker
{
    private static int _instanceIdx;
    
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool Prefix(Map map, ref ModuleBase ___noise, ref Map ___currentlyInitializedForMap, 
        ref float ___perlinFrequency, ref float ___perlinLacunarity, ref float ___perlinPersistence, ref int ___perlinOctaves)
    {
        int seed = Landform.MakeSeed(map.Tile, 9305 + _instanceIdx);
        ___noise = new Perlin(___perlinFrequency, ___perlinLacunarity, ___perlinPersistence, ___perlinOctaves, seed, QualityMode.Medium);
        ___currentlyInitializedForMap = map;
        _instanceIdx++;
        return false;
    }

    public static void Reset()
    {
        _instanceIdx = 0;
    }
}