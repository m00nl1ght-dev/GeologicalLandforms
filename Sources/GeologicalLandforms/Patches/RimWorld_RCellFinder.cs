using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Profile;

// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(RCellFinder))]
internal static class RimWorld_RCellFinder
{
    private static readonly Dictionary<Map, ushort[]> _cache = new();

    [HarmonyPatch("TryFindRandomExitSpot")]
    [HarmonyPriority(Priority.High)]
    [HarmonyPrefix]
    private static bool TryFindRandomExitSpot(ref bool __result, Pawn pawn, ref IntVec3 spot, TraverseMode mode)
    {
        if (!ModInstance.Settings.EnableCellFinderOptimization) return true;
        
        var map = pawn?.Map;
        if (map == null)
        {
            __result = false;
            spot = IntVec3.Invalid;
            return false;
        }
        
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
    
    [HarmonyPatch("TryFindBestExitSpot")]
    [HarmonyPriority(Priority.Low)]
    [HarmonyPostfix]
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
        
        Log.Message(ModInstance.LogPrefix + "Found " + buffer.Count + " walkable map edge cells. Caching enabled: " + underLimit);
        if (Prefs.DevMode) foreach (var cellval in buffer)
        {
            map.debugDrawer.FlashCell(ValToVec(map, cellval));
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
    
    [HarmonyPatch(typeof(Game), "DeinitAndRemoveMap")]
    [HarmonyPriority(Priority.Low)]
    [HarmonyPostfix]
    private static void DeinitAndRemoveMap(Map map)
    {
        _cache.Remove(map);
    }
    
    [HarmonyPatch(typeof(MemoryUtility), "ClearAllMapsAndWorld")]
    [HarmonyPriority(Priority.Low)]
    [HarmonyPostfix]
    private static void ClearAllMapsAndWorld()
    {
        _cache.Clear();
    }
    
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    [HarmonyPriority(Priority.High)]
    [HarmonyPostfix]
    private static void FloatMenuMakerMap_AddHumanlikeOrders(ref Vector3 clickPos, ref Pawn pawn, ref List<FloatMenuOption> opts)
    {
        if (!ModInstance.Settings.ShowWorldTileDebugInfo) return;
        foreach (LocalTargetInfo target in GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true))
        {
            Pawn localpawn = pawn;
            LocalTargetInfo dest = target;
            Pawn pTarg = (Pawn)dest.Thing;
            
            void ActionBest()
            {
                if (!RCellFinder.TryFindBestExitSpot(pTarg, out IntVec3 dest1))
                    dest1 = pTarg.Position;
                Job job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                localpawn.jobs.TryTakeOrderedJob(job1);
            }
            
            void ActionRandom()
            {
                if (!RCellFinder.TryFindRandomExitSpot(pTarg, out IntVec3 dest1))
                    dest1 = pTarg.Position;
                Job job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                localpawn.jobs.TryTakeOrderedJob(job1);
            }
            
            void ActionMapInfo()
            {
                RimWorld_WildPlantSpawner.LogInfo(localpawn.Map, localpawn.Position);
            }
            
            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find best map exit", ActionBest, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg));
            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find random map exit", ActionRandom, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg));
            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Print map info", ActionMapInfo, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg));
        }
    }
}