using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace GeologicalLandforms.Patches;

/// <summary>
/// A collection of patches to improve various cell finder methods. Can be disabled in the mod settings.
/// The vanilla algorithms struggle on maps with little unroofed cells, such as maps with a cave landform.
/// The prefixes and transpilers are used to optimize search logic, while the postfixes are used to
/// provide fallbacks which only apply in case the vanilla algorithm failed to find a valid cell.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch]
internal static class Patch_RimWorld_CellFinder
{
    private const int UnroofedCacheLimit = 50;

    private static readonly Type Self = typeof(Patch_RimWorld_CellFinder);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RCellFinder), "TryFindRandomExitSpot")]
    [HarmonyPriority(Priority.High)]
    private static bool TryFindRandomExitSpot(ref bool __result, Pawn pawn, ref IntVec3 spot, TraverseMode mode)
    {
        var map = pawn?.Map;
        if (map == null)
        {
            __result = false;
            spot = IntVec3.Invalid;
            return false;
        }

        if (!GeologicalLandformsAPI.UseCellFinderOptimization()) return true;

        var cache = GetOrBuildEdgeCacheForMap(map);
        if (cache.Length == 0) return true;

        var maxDanger = Danger.Some;
        IntVec3 intVec3;
        int tries = 0;

        do
        {
            ++tries;
            if (tries > 40)
            {
                spot = pawn.Position;
                __result = false;
                return false;
            }

            if (tries > 15) maxDanger = Danger.Deadly;
            intVec3 = EdgeCellIdxToVec(map, cache[Rand.Range(0, cache.Length)]);
        }
        while (!intVec3.Standable(map) || !pawn.CanReach(intVec3, PathEndMode.OnCell, maxDanger, mode: mode));

        spot = intVec3;
        __result = true;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RCellFinder), "TryFindBestExitSpot")]
    [HarmonyPriority(Priority.Low)]
    private static void TryFindBestExitSpot(ref bool __result, Pawn pawn, ref IntVec3 spot, TraverseMode mode)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = RCellFinder.TryFindRandomExitSpot(pawn, out spot, mode);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CellFinder), "TryFindRandomPawnExitCell")]
    [HarmonyPriority(Priority.Low)]
    private static void TryFindRandomPawnExitCell(ref bool __result, Pawn searcher, ref IntVec3 result)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = RCellFinder.TryFindRandomExitSpot(searcher, out result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CellFinderLoose), "TryGetRandomCellWith")]
    [HarmonyPriority(Priority.Low)]
    private static void TryGetRandomCellWith(ref bool __result, Predicate<IntVec3> validator, Map map, ref IntVec3 result)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = TryGetRandomUnroofedCellFromCache(validator, map, out result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CellFinderLoose), "TryFindRandomNotEdgeCellWith")]
    [HarmonyPriority(Priority.Low)]
    private static void TryFindRandomNotEdgeCellWith(ref bool __result, Predicate<IntVec3> validator, Map map, ref IntVec3 result)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = TryGetRandomUnroofedCellFromCache(validator, map, out result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QuestNode_Root_ShuttleCrash_Rescue), "TryFindRaidWalkInPosition")]
    [HarmonyPriority(Priority.Low)]
    private static void TryFindRaidWalkInPosition(ref bool __result, Map map, ref IntVec3 spawnSpot)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = RCellFinder.TryFindRandomPawnEntryCell(out spawnSpot, map, CellFinder.EdgeRoadChance_Hostile);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RCellFinder), "FindSiegePositionFrom")]
    [HarmonyPriority(Priority.Low)]
    private static void FindSiegePositionFrom(ref IntVec3 __result, Map map)
    {
        if (__result.IsValid || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;
        __result = DropCellFinder.RandomDropSpot(map);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DropCellFinder), "FindSafeLandingSpot")]
    [HarmonyPriority(Priority.Low)]
    private static void FindSafeLandingSpot(ref bool __result, ref IntVec3 spot, Map map, IntVec2? size)
    {
        if (__result || !GeologicalLandformsAPI.UseCellFinderOptimization()) return;

        var center = DropCellFinder.RandomDropSpot(map);
        if (!center.IsValid) center = DropCellFinder.RandomDropSpot(map, false);
        if (!center.IsValid) return;

        DropCellFinder.TryFindDropSpotNear(center, map, out spot, false, false, false, size);
        __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RCellFinder), "TryFindRandomPawnEntryCell")]
    [HarmonyPriority(Priority.High)]
    private static bool TryFindRandomPawnEntryCell(ref bool __result, ref IntVec3 result, Map map, bool allowFogged, Predicate<IntVec3> extraValidator)
    {
        if (map.TileInfo.hilliness != Hilliness.Impassable) return true;

        __result = CellFinder.TryFindRandomEdgeCellWith(c =>
            c.Standable(map) && map.reachability.CanReachColony(c) && (allowFogged || !c.Fogged(map)) &&
            (extraValidator == null || extraValidator(c)), map, 0f, out result);

        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DropCellFinder), "FindRaidDropCenterDistant")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FindRaidDropCenterDistant_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ldlocNum = new CodeInstruction(OpCodes.Ldloc);
        var ldlocPos = new CodeInstruction(OpCodes.Ldloc);
        var stlocPos = new CodeInstruction(OpCodes.Stloc);

        var pattern = TranspilerPattern.Build("FindRaidDropCenterDistant")
            .MatchLdarg().Keep()
            .MatchCall(typeof(CellFinder), "RandomCell", new[] { typeof(Map) }).Keep()
            .MatchStloc().StoreOperandIn(ldlocPos, stlocPos).Keep()
            .MatchLdloc().StoreOperandIn(ldlocNum).Keep()
            .Match(OpCodes.Ldc_I4_1).Keep()
            .Match(OpCodes.Add).Keep()
            .MatchStloc().Keep()
            .Insert(OpCodes.Ldarg_0).Insert(ldlocPos).Insert(ldlocNum)
            .Insert(CodeInstruction.Call(Self, nameof(ConsiderCacheForRandomCell)))
            .Insert(stlocPos);

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WalkPathFinder), "TryFindWalkPath")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> TryFindWalkPath_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("TryFindWalkPath")
            .MatchCall(typeof(GridsUtility), "Roofed", new[] { typeof(IntVec3), typeof(Map) }).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(RoofedAndTileNotImpassable)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static bool TryGetRandomUnroofedCellFromCache(Predicate<IntVec3> validator, Map map, out IntVec3 result)
    {
        var cache = GetOrBuildUnroofedCacheForMap(map);
        var offset = Rand.Range(0, cache.Length);

        for (var i = 0; i < cache.Length; i++)
        {
            var c = cache[(i + offset) % cache.Length];
            if (validator(c))
            {
                result = c;
                return true;
            }
        }

        result = IntVec3.Invalid;
        return false;
    }

    private static ushort[] GetOrBuildEdgeCacheForMap(Map map)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid == null) return Array.Empty<ushort>();

        if (biomeGrid.WalkableEdgeCellsCache != null) return biomeGrid.WalkableEdgeCellsCache;

        try
        {
            if (map.Size.x != map.Size.z)
            {
                biomeGrid.WalkableEdgeCellsCache = Array.Empty<ushort>();
                return Array.Empty<ushort>();
            }

            var mapSize = map.Size.x;
            var buffer = new List<ushort>();
            var pathGrid = map.pathing.Normal.pathGrid;

            void FillBuffer(int x, int z, int val)
            {
                if (pathGrid.Walkable(new IntVec3(x, 0, z))) buffer.Add((ushort) val);
            }

            var m = mapSize - 1;
            for (int i = 1; i < m; i++)
            {
                FillBuffer(i, 0, 0 * mapSize + i);
                FillBuffer(i, m, 1 * mapSize + i);
                FillBuffer(0, i, 2 * mapSize + i);
                FillBuffer(m, i, 3 * mapSize + i);
            }

            var underLimit = buffer.Count < 2 * mapSize;
            var vecs = underLimit ? buffer.ToArray() : Array.Empty<ushort>();

            GeologicalLandformsAPI.Logger.Debug($"Found {buffer.Count} walkable map edge cells. Caching enabled: {underLimit}");
            if (Prefs.DevMode)
            {
                foreach (var cellval in buffer)
                {
                    map.debugDrawer.FlashCell(EdgeCellIdxToVec(map, cellval));
                }
            }

            biomeGrid.WalkableEdgeCellsCache = vecs;
            return vecs;
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Warn("Failed to cache walkable map edge cells.", e);
            biomeGrid.WalkableEdgeCellsCache = Array.Empty<ushort>();
            return Array.Empty<ushort>();
        }
    }

    private static IntVec3[] GetOrBuildUnroofedCacheForMap(Map map)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid == null) return Array.Empty<IntVec3>();

        if (biomeGrid.UnroofedCellsCache != null) return biomeGrid.UnroofedCellsCache;

        try
        {
            var roofGrid = map.roofGrid;
            var samples = new List<IntVec3>(UnroofedCacheLimit);

            foreach (var pos in map.cellsInRandomOrder.GetAll())
            {
                if (samples.Count >= UnroofedCacheLimit) break;
                if (!roofGrid.Roofed(pos)) samples.Add(pos);
            }

            GeologicalLandformsAPI.Logger.Debug($"Cached {samples.Count} unroofed map cells.");

            var cache = samples.ToArray();
            biomeGrid.UnroofedCellsCache = cache;
            return cache;
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Warn("Failed to cache unroofed map cells.", e);
            biomeGrid.UnroofedCellsCache = Array.Empty<IntVec3>();
            return Array.Empty<IntVec3>();
        }
    }

    private static IntVec3 EdgeCellIdxToVec(Map map, ushort val)
    {
        var s = map.Size.x;
        if (val < s * 1) return new IntVec3(val % s, 0, 0);
        if (val < s * 2) return new IntVec3(val % s, 0, s - 1);
        if (val < s * 3) return new IntVec3(0, 0, val % s);
        if (val < s * 4) return new IntVec3(s - 1, 0, val % s);
        return IntVec3.Invalid;
    }

    private static IntVec3 ConsiderCacheForRandomCell(Map map, IntVec3 pos, int failedTries)
    {
        if (failedTries < 200) return pos;
        if (failedTries > 500) throw new Exception("Failed to find valid cell after 500 tries");

        var cache = GetOrBuildUnroofedCacheForMap(map);
        if (cache.Length == 0) return pos;

        return cache[Rand.Range(0, cache.Length)];
    }

    private static bool RoofedAndTileNotImpassable(IntVec3 pos, Map map)
    {
        return pos.Roofed(map) && map.TileInfo.hilliness != Hilliness.Impassable;
    }
}
