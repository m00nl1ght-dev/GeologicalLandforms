using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using MapPreview;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapGenerator))]
internal static class Patch_Verse_MapGenerator
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    [HarmonyPriority(Priority.First)]
    private static void GenerateContentsIntoMap_Prefix(Map map, ref IEnumerable<GenStepWithParams> genStepDefs)
    {
        LandformGraphEditor.ActiveEditor?.Close();

        Landform.PrepareMapGen(map);

        var bvGenStepDef = new GenStepDef { order = 225, generated = true };
        bvGenStepDef.genStep = new GenStep_BiomeVariants { def = bvGenStepDef };

        genStepDefs = genStepDefs.Concat(new GenStepWithParams(bvGenStepDef, new GenStepParams()));

        if (Landform.AnyGenerating)
        {
            foreach (var node in Landform.GeneratingLandforms.SelectMany(lf => lf.CustomGenSteps))
            {
                if (node.GenStepDef != null && (!MapPreviewAPI.IsGeneratingPreview || node.Order < 230))
                {
                    genStepDefs = genStepDefs.Concat(new GenStepWithParams(node.GenStepDef, new GenStepParams()));
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    [HarmonyPriority(Priority.Last)]
    private static void GenerateContentsIntoMap_Postfix(Map map)
    {
        Landform.CleanUp();
    }
}
