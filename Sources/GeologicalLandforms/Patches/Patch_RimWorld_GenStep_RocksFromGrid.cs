using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_RocksFromGrid))]
internal static class Patch_RimWorld_GenStep_RocksFromGrid
{
    internal static readonly Type Self = typeof(Patch_RimWorld_GenStep_RocksFromGrid);

    [HarmonyTranspiler]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyAfter("rimworld.biomes.core")]
    private static IEnumerable<CodeInstruction> Generate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var setRoofs = TranspilerPattern.Build("SetRoofsFromLandform")
            .MatchNewobj(typeof(BoolGrid), [typeof(Map)]).Keep()
            .Match(OpCodes.Stfld).Keep()
            .Insert(OpCodes.Ldarg_1)
            .Insert(CodeInstruction.Call(Self, nameof(SetRoofsFromLandform)));

        var adjustMineables = TranspilerPattern.Build("AdjustMineables")
            .Insert(OpCodes.Ldarg_1)
            .Insert(CodeInstruction.Call(Self, nameof(AdjustMineables)))
            .MatchStore(typeof(GenStep_Scatterer), "countPer10kCellsRange").Keep();

        return TranspilerPattern.Apply(instructions, setRoofs, adjustMineables);
    }

    private static void SetRoofsFromLandform(Map map)
    {
        if (!Landform.AnyGenerating) return;

        var roofGridModule = Landform.GetFeatureScaled(l => l.OutputRoofGrid?.Get());
        if (roofGridModule == null) return;

        foreach (var cell in map.AllCells)
        {
            map.roofGrid.SetRoof(cell, roofGridModule.ValueAt(cell.x, cell.z).Roof);
        }
    }

    private static FloatRange AdjustMineables(FloatRange original, Map map)
    {
        if (!Landform.AnyGenerating) return original;

        double? scatterAmount = Landform.GetFeature(l => l.OutputScatterers?.GetMineables());
        return scatterAmount.HasValue ? new FloatRange((float) scatterAmount.Value, (float) scatterAmount.Value) : original;
    }
}
