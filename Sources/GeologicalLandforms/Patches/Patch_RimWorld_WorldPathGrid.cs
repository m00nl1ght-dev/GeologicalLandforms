using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldPathGrid))]
internal static class Patch_RimWorld_WorldPathGrid
{
    public const float ImpassableMovementDifficultyOffset = 9f;

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
            .Insert(CodeInstruction.Call(typeof(Patch_RimWorld_WorldPathGrid), nameof(ShouldTileBePassable)))
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

    private static bool ShouldTileBePassable(int tile)
    {
        var world = Find.World;

        if (world.grid[tile].hilliness != Hilliness.Impassable) return true;

        if (world.HasFinishedGenerating())
        {
            var tileInfo = WorldTileInfo.Get(tile);
            if (tileInfo.Biome.Properties().allowSettlementsOnImpassableTerrain) return true;
            if (tileInfo.HasLandforms() && tileInfo.Landforms.Any(lf => !lf.IsLayer)) return true;
            if (tileInfo.WorldObject != null) return true;
        }

        return false;
    }
}
