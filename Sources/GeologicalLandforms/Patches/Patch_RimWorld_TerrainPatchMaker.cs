using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(TerrainPatchMaker))]
internal static class Patch_RimWorld_TerrainPatchMaker
{
    [HarmonyPostfix]
    [HarmonyPatch("Init")]
    private static void Init(ref ModuleBase ___noise)
    {
        if (!Landform.AnyGenerating) return;

        var offsetFunc = Landform.GetFeatureScaled(l => l.OutputTerrainPatches?.GetOffset());
        var frequencyFactor = Landform.GetFeature(l => l.OutputTerrainPatches?.GetFrequencyFactor()) ?? 1d;
        var lacunarityFactor = Landform.GetFeature(l => l.OutputTerrainPatches?.GetLacunarityFactor()) ?? 1d;
        var persistenceFactor = Landform.GetFeature(l => l.OutputTerrainPatches?.GetPersistenceFactor()) ?? 1d;

        if (___noise is Perlin perlin)
        {
            perlin.Frequency *= frequencyFactor;
            perlin.Lacunarity *= lacunarityFactor;
            perlin.Persistence *= persistenceFactor;
        }

        if (offsetFunc != null)
        {
            ___noise = new Add(___noise, offsetFunc.AsModule());
        }
    }
}
