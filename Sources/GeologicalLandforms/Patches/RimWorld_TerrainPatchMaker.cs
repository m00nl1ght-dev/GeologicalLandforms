using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

/// <summary>
/// Fixes an inconsistency in Map Reroll code that causes all TerrainPatchMakers to have the same seed,
/// making biomes with multiple of them (r.g. swamps) look ugly.
/// </summary>
[HarmonyPatch(typeof(TerrainPatchMaker), "Init")]
internal static class RimWorld_TerrainPatchMaker
{
    private static int _instanceIdx;

    public static bool UseStableSeed;
    
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool Prefix(Map map, ref ModuleBase ___noise, ref Map ___currentlyInitializedForMap, TerrainPatchMaker __instance)
    {
        int seed = MakeSeed(__instance, map.Tile);
        ___noise = new Perlin(__instance.perlinFrequency, __instance.perlinLacunarity, __instance.perlinPersistence, __instance.perlinOctaves, seed, QualityMode.Medium);
        NoiseDebugUI.RenderSize = new IntVec2(map.Size.x, map.Size.z);
        NoiseDebugUI.StoreNoiseRender(___noise, "TerrainPatchMaker " + _instanceIdx);
        ___currentlyInitializedForMap = map;
        _instanceIdx++;
        return false;
    }

    private static int MakeSeed(TerrainPatchMaker tpm, int tile)
    {
        if (!UseStableSeed) return Find.World.info.Seed ^ tile ^ 9305 + _instanceIdx;
        return Find.World.info.Seed ^ tile ^ GenText.StableStringHash(tpm.thresholds[0].terrain.defName);
    }

    public static void Reset()
    {
        _instanceIdx = 0;
    }
}