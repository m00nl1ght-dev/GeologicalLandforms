using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using MapPreview;
using Verse;

#if DEBUG
using RimWorld;
#endif

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

        Landform.Prepare(map);

        if (!MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            var data = Find.World?.LandformData();
            var stored = data != null && map.Tile >= 0 && data.HasData(map.Tile);
            var indicator = data == null ? "X" : stored ? "L" : "F";

            GeologicalLandformsAPI.Logger.Log($"Map generator context: {Landform.GeneratingTile} ({indicator})");

            if (data != null && map.Tile >= 0)
            {
                data.CommitDirectly(map.Tile, new LandformData.TileData(Landform.GeneratingTile));
            }
        }

        var biomeGrid = map.BiomeGrid();
        if (biomeGrid != null)
        {
            biomeGrid.Primary.Set(map.Biome);
            biomeGrid.RefreshAllEntries(Landform.GeneratingTile);
        }

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

        #if DEBUG
        if (Prefs.DevMode) map.weatherManager.curWeather = WeatherDefOf.Clear;
        #endif
    }
}
