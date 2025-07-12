#if !RW_1_6_OR_GREATER

using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_CavesTerrain))]
internal static class Patch_RimWorld_GenStep_CavesTerrain
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool Generate_Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (!Landform.AnyGenerating) return true;

        var cavesTerrainModule = Landform.GetFeatureScaled(l => l.OutputTerrain?.GetCave());

        if (cavesTerrainModule == null) return true;

        var caves = MapGenerator.Caves;

        foreach (var cell in map.AllCells)
        {
            if (caves[cell] > 0.0 && !cell.GetTerrain(map).IsRiver)
            {
                var terrainDef = cavesTerrainModule.ValueAt(cell.x, cell.z);
                if (terrainDef != null) map.terrainGrid.SetTerrain(cell, terrainDef);
            }
        }

        return false;
    }
}

#endif
