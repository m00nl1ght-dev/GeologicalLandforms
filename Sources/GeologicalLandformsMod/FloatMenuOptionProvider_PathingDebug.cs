#if RW_1_6_OR_GREATER
#if DEBUG

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeologicalLandforms;

public class FloatMenuOptionProvider_PathingDebug : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool CanSelfTarget => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn != context.FirstSelectedPawn)
            yield break;

        void ActionBest()
        {
            if (!RCellFinder.TryFindBestExitSpot(clickedPawn, out var dest1))
                dest1 = clickedPawn.Position;
            var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
            job1.exitMapOnArrival = false;
            clickedPawn.jobs.TryTakeOrderedJob(job1);
        }

        void ActionRandom()
        {
            if (!RCellFinder.TryFindRandomExitSpot(clickedPawn, out var dest1))
                dest1 = clickedPawn.Position;
            var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest1);
            job1.exitMapOnArrival = false;
            clickedPawn.jobs.TryTakeOrderedJob(job1);
        }

        yield return new FloatMenuOption("[debug] Find best map exit", ActionBest);
        yield return new FloatMenuOption("[debug] Find random map exit", ActionRandom);
    }
}

#endif
#endif
