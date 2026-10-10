using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

#if RW_1_6_OR_GREATER
using RimWorld;
using System.Reflection;
#else
using RimWorld.QuestGen;
#endif

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

    #if RW_1_6_OR_GREATER
    private static bool CanSettleOnTile(PlanetTile tile)
    #else
    private static bool CanSettleOnTile(int tile)
    #endif
    {
        var world = Find.World;

        if (world.grid[tile].hilliness != Hilliness.Impassable) return true;

        #if RW_1_6_OR_GREATER

        // keep vanilla behavior unless player is actively selecting a tile to settle/land on
        if (!PlayerSelectingTileContext.Inside) return false;

        #else

        // old 1.5 check, prevents quest sites from spawning on impassable tiles
        if (QuestGen.Working) return false;

        #endif

        if (world.HasFinishedGenerating())
        {
            var tileInfo = WorldTileInfo.Get(tile);
            if (tileInfo.Biome.Properties().allowSettlementsOnImpassableTerrain) return true;
            if (tileInfo.HasLandforms() && tileInfo.Landforms.Any(lf => !lf.IsLayer)) return true;
        }

        return false;
    }

    #if RW_1_6_OR_GREATER

    /// <summary>
    /// Keeps track of whether IsValidTileForNewSettlement runs in the context of the player selecting a world tile to settle, camp or land on.
    /// </summary>
    [PatchGroup("Main")]
    [HarmonyPatch]
    internal static class PlayerSelectingTileContext
    {
        public static bool Inside => ctxDepth > 0;

        [ThreadStatic]
        private static int ctxDepth;

        [HarmonyTargetMethods]
        private static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TilePicker), nameof(TilePicker.TileSelectorOnGUI));
            yield return AccessTools.Method(typeof(Page_SelectStartingSite), nameof(Page_SelectStartingSite.CanDoNext));
            yield return AccessTools.Method(typeof(SettleInEmptyTileUtility), nameof(SettleInEmptyTileUtility.SettleCommand));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(Camp), nameof(Camp.GetGizmos)));
        }

        [HarmonyPrefix]
        [HarmonyPriority(900)]
        private static void Prefix()
        {
            ctxDepth++;
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            if (ctxDepth > 0) ctxDepth--;
        }
    }

    #endif
}
