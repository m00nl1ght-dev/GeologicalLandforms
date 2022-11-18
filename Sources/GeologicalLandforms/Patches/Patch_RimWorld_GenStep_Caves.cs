using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Caves))]
internal static class Patch_RimWorld_GenStep_Caves
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyBefore("com.configurablemaps.rimworld.mod")]
    private static bool Generate_Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (!Landform.AnyGenerating) return true;

        var cavesModule = Landform.GetFeatureScaled(l => l.OutputCaves?.Get());

        if (cavesModule == null) return true;

        var caves = MapGenerator.Caves;

        foreach (var cell in map.AllCells)
        {
            caves[cell] = (float) cavesModule.ValueAt(cell.x, cell.z);
        }
        
        return false;
    }
}