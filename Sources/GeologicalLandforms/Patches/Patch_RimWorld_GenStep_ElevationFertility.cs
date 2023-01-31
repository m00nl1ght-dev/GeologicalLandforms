using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_ElevationFertility))]
internal static class Patch_RimWorld_GenStep_ElevationFertility
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyBefore("com.configurablemaps.rimworld.mod")]
    private static bool Generate_Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (!Landform.AnyGenerating) return true;

        var elevationModule = Landform.GetFeatureScaled(l => l.OutputElevation?.Get());
        var fertilityModule = Landform.GetFeatureScaled(l => l.OutputFertility?.Get());

        if (elevationModule == null && fertilityModule == null) return true;

        var elevationSeed = Rand.Range(0, int.MaxValue);
        var fertilitySeed = Rand.Range(0, int.MaxValue);

        elevationModule ??= NodeInputBase.BuildVanillaElevationGrid(Landform.GeneratingTile, elevationSeed);
        fertilityModule ??= NodeInputBase.BuildVanillaFertilityGrid(Landform.GeneratingTile, fertilitySeed);

        var elevation = MapGenerator.Elevation;
        var fertility = MapGenerator.Fertility;

        foreach (var cell in map.AllCells)
        {
            elevation[cell] = (float) elevationModule.ValueAt(cell.x, cell.z);
            fertility[cell] = (float) fertilityModule.ValueAt(cell.x, cell.z);
        }

        return false;
    }

    /// <summary>
    /// Disables vanilla cliff generation on mountainous tiles.
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.First)]
    private static IEnumerable<CodeInstruction> Generate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patched = false;
        var foundElevStr = false;

        var method = AccessTools.Method(typeof(Patch_RimWorld_GenStep_ElevationFertility), nameof(AdjustHillinessCheck));
        if (method == null) throw new Exception("missing AdjustHillinessCheck");

        var field = AccessTools.Field(typeof(Tile), nameof(Tile.hilliness));
        if (field == null) throw new Exception("missing Tile.hilliness");

        foreach (var instruction in instructions)
        {
            if (instruction.LoadsConstant("elev world-factored")) foundElevStr = true;

            if (!patched && foundElevStr && instruction.LoadsField(field))
            {
                patched = true;
                yield return new CodeInstruction(OpCodes.Call, method);
                continue;
            }

            yield return instruction;
        }

        if (patched == false)
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_GenStep_ElevationFertility");
    }

    private static int AdjustHillinessCheck(Tile tile)
    {
        if (GeologicalLandformsAPI.DisableVanillaMountainGeneration && tile.hilliness == Hilliness.Mountainous)
        {
            return (int) Hilliness.LargeHills;
        }

        return (int) tile.hilliness;
    }
}
