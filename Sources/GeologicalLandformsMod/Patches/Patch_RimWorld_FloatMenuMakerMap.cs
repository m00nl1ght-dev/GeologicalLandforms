#if DEBUG
#if !RW_1_6_OR_GREATER

using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(FloatMenuMakerMap))]
internal static class Patch_RimWorld_FloatMenuMakerMap
{
    [HarmonyPostfix]
    [HarmonyPatch("AddHumanlikeOrders")]
    [HarmonyPriority(Priority.High)]
    private static void AddHumanlikeOrders(ref Vector3 clickPos, ref Pawn pawn, ref List<FloatMenuOption> opts)
    {
        if (!Prefs.DevMode) return;

        foreach (var target in GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true))
        {
            var pawn1 = pawn;
            var dest = target;
            var pawn2 = (Pawn) dest.Thing;

            void ActionBest()
            {
                if (!RCellFinder.TryFindBestExitSpot(pawn2, out var dest1))
                    dest1 = pawn2.Position;
                var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                pawn1.jobs.TryTakeOrderedJob(job1);
            }

            void ActionRandom()
            {
                if (!RCellFinder.TryFindRandomExitSpot(pawn2, out var dest1))
                    dest1 = pawn2.Position;
                var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                pawn1.jobs.TryTakeOrderedJob(job1);
            }

            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find best map exit", ActionBest, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pawn2));
            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find random map exit", ActionRandom, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pawn2));
        }
    }
}

#endif
#endif
