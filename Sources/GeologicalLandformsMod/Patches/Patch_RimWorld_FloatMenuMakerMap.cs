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
        if (!Prefs.DevMode || !GeologicalLandformsMod.Settings.EnableMapDebugPawnCommands) return;

        foreach (var target in GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true))
        {
            var localpawn = pawn;
            var dest = target;
            var pTarg = (Pawn) dest.Thing;

            void ActionBest()
            {
                if (!RCellFinder.TryFindBestExitSpot(pTarg, out var dest1))
                    dest1 = pTarg.Position;
                var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                localpawn.jobs.TryTakeOrderedJob(job1);
            }

            void ActionRandom()
            {
                if (!RCellFinder.TryFindRandomExitSpot(pTarg, out var dest1))
                    dest1 = pTarg.Position;
                var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
                job1.exitMapOnArrival = false;
                localpawn.jobs.TryTakeOrderedJob(job1);
            }

            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find best map exit", ActionBest, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg));
            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("[debug] Find random map exit", ActionRandom, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg));
        }
    }
}
