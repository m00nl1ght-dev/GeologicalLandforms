using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldPathGrid))]
internal static class Patch_RimWorld_WorldPathGrid
{
    public const float ImpassableMovementDifficultyOffset = 50f;

    [HarmonyTranspiler]
    [HarmonyPatch("CalculatedMovementDifficultyAt")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> CalculatedMovementDifficultyAt_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var brtrueSkip = new CodeInstruction(OpCodes.Brtrue_S);

        var pattern = TranspilerPattern.Build("CanSettleOnTile")
            .MatchLdloc().Replace(OpCodes.Ldarg_0)
            .MatchLoad(typeof(Tile), "hilliness").Remove()
            .Match(OpCodes.Ldc_I4_5).Remove()
            .Match(OpCodes.Bne_Un_S).StoreOperandIn(brtrueSkip).Remove()
            .Insert(CodeInstruction.Call(typeof(Patch_RimWorld_TileFinder), nameof(Patch_RimWorld_TileFinder.CanSettleOnTile)))
            .Insert(brtrueSkip);

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyPostfix]
    [HarmonyPatch("HillinessMovementDifficultyOffset")]
    [HarmonyPriority(Priority.Low)]
    private static void HillinessMovementDifficultyOffset(Hilliness hilliness, ref float __result)
    {
        if (hilliness == Hilliness.Impassable) __result = ImpassableMovementDifficultyOffset;
    }
}
