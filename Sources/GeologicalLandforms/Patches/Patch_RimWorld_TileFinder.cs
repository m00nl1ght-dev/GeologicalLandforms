using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(TileFinder))]
internal static class Patch_RimWorld_TileFinder
{
    internal static readonly Type Self = typeof(Patch_RimWorld_TileFinder);

    [HarmonyTranspiler]
    [HarmonyPatch("IsValidTileForNewSettlement")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> IsValidTileForNewSettlement_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var brtrueSkip = new CodeInstruction(OpCodes.Brtrue_S);

        var pattern = TranspilerPattern.Build("CanSettleOnTile")
            .MatchLdloc().Replace(OpCodes.Ldarg_0)
            .MatchLoad(typeof(Tile), "hilliness").Remove()
            .Match(OpCodes.Ldc_I4_5).Remove()
            .Match(OpCodes.Bne_Un_S).StoreOperandIn(brtrueSkip).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(CanSettleOnTile)))
            .Insert(brtrueSkip);

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static bool CanSettleOnTile(int tile)
    {
        var world = Find.World;

        if (world.grid[tile].hilliness != Hilliness.Impassable) return true;

        if (QuestGen.Working) return false; // prevent quest sites from spawning on impassable tiles

        if (world.HasFinishedGenerating())
        {
            var tileInfo = WorldTileInfo.Get(tile);
            if (tileInfo.Biome.Properties().allowSettlementsOnImpassableTerrain) return true;
            if (tileInfo.HasLandforms() && tileInfo.Landforms.Any(lf => !lf.IsLayer)) return true;
        }

        return false;
    }
}
