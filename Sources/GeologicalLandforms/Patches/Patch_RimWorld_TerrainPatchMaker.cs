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
        if (offsetFunc == null) return;

        ___noise = new Add(___noise, offsetFunc.AsModule());
    }
}
