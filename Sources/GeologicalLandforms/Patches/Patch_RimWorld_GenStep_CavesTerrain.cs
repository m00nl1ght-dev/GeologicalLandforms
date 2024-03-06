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
                var terrainData = cavesTerrainModule.ValueAt(cell.x, cell.z);
                if (terrainData.HasTerrain) map.terrainGrid.SetTerrain(cell, terrainData.Terrain);
            }
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.Low)]
    private static void Generate_Postfix(Map map, GenStepParams parms)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid != null) Patch_RimWorld_GenStep_Terrain.ApplyBiomeVariants(biomeGrid);
    }
}
