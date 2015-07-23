﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace MD2
{
    public class JobDriver_DroidCharge : JobDriver
    {

        protected override IEnumerable<Toil> MakeNewToils()
        {

            Building_DroidChargePad charger = (Building_DroidChargePad)TargetThingA;
            Toil goToPad = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            goToPad.AddFailCondition(() => { return !charger.IsAvailable(pawn); });
            yield return goToPad;
            Droid droid = (Droid)this.pawn;
            Toil charge = new Toil();
            charge.initAction = () =>
                {
                    if (charger.Position != droid.Position)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Errored);
                    }
                };
            charge.defaultCompleteMode = ToilCompleteMode.Never;
            charge.AddFailCondition(() =>
                { return !charger.IsAvailable(pawn); });
            charge.AddEndCondition(() =>
                {
                    if (!droid.DesiresCharge)
                    {
                        return JobCondition.Succeeded;
                    }
                    return JobCondition.Ongoing;
                });
            yield return charge;
        }
    }
}
