using System;
using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Profile;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RCellFinder))]
internal static class Patch_RimWorld_RCellFinder
{
    private static readonly Dictionary<Map, ushort[]> _cache = new();

    [HarmonyPrefix]
    [HarmonyPatch("TryFindRandomExitSpot")]
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

        var cache = GetOrBuildCacheForMap(map);
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
            intVec3 = ValToVec(map, cache[Rand.Range(0, cache.Length)]);
        }
        while (!intVec3.Standable(map) || !pawn.CanReach(intVec3, PathEndMode.OnCell, maxDanger, mode: mode));

        spot = intVec3;
        __result = true;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("TryFindBestExitSpot")]
    [HarmonyPriority(Priority.Low)]
    private static void TryFindBestExitSpot(ref bool __result, Pawn pawn, ref IntVec3 spot, TraverseMode mode)
    {
        if (__result) return; // vanilla algorithm already found an exit, no need to do anything
        __result = RCellFinder.TryFindRandomExitSpot(pawn, out spot, mode);
    }

    private static ushort[] GetOrBuildCacheForMap(Map map)
    {
        if (_cache.TryGetValue(map, out var array)) return array;

        if (map.Size.x != map.Size.z)
        {
            _cache[map] = Array.Empty<ushort>();
            return Array.Empty<ushort>();
        }

        var mapSize = map.Size.x;
        var buffer = new List<ushort>();

        void FillBuffer(int x, int z, int val)
        {
            if (map.pathing.Normal.pathGrid.Walkable(new IntVec3(x, 0, z))) buffer.Add((ushort) val);
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

        GeologicalLandformsAPI.Logger.Log("Found " + buffer.Count + " walkable map edge cells. Caching enabled: " + underLimit);
        if (Prefs.DevMode)
        {
            foreach (var cellval in buffer)
            {
                map.debugDrawer.FlashCell(ValToVec(map, cellval));
            }
        }

        _cache[map] = vecs;
        return vecs;
    }

    private static IntVec3 ValToVec(Map map, ushort val)
    {
        var s = map.Size.x;
        if (val < s * 1) return new IntVec3(val % s, 0, 0);
        if (val < s * 2) return new IntVec3(val % s, 0, s - 1);
        if (val < s * 3) return new IntVec3(0, 0, val % s);
        if (val < s * 4) return new IntVec3(s - 1, 0, val % s);
        return IntVec3.Invalid;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game), "DeinitAndRemoveMap_NewTemp")]
    [HarmonyPriority(Priority.Low)]
    private static void DeinitAndRemoveMap(Map map)
    {
        _cache.Remove(map);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MemoryUtility), "ClearAllMapsAndWorld")]
    [HarmonyPriority(Priority.Low)]
    private static void ClearAllMapsAndWorld()
    {
        _cache.Clear();
    }
}
