using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace MD2
{
    public class JobGiver_DroidCharge : ThinkNode_JobGiver
    {
        public int stage = 0;

        public JobGiver_DroidCharge(int stage)
        {
            this.stage = stage;
        }

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {

            if (!(pawn is Droid))
            {
                return null;
            }
            Droid droid = (Droid)pawn;
            if (!droid.Active)
                return null;
            //Check the charge level
            float chargeThreshold;
            float distance;
            switch(stage)
            {
                case 1:
                    {
                        chargeThreshold = droid.meta.PowerSafeThreshold;
                        distance = 20f;
                        break;
                    }
                case 2:
                    {
                        chargeThreshold = droid.meta.PowerLowThreshold;
                        distance = 50f;
                        break;
                    }
                case 3:
                    {
                        chargeThreshold = droid.meta.PowerCriticalThreshold;
                        distance = 9999f;
                        break;
                    }
                default:
                    {
                        chargeThreshold = droid.meta.PowerLowThreshold;
                        distance = 50f;
                        break;
                    }
            }
            if (droid.TotalCharge < droid.MaxEnergy * chargeThreshold)
            {
                IEnumerable<Thing> chargers;
                List<Thing> list = new List<Thing>();
                foreach(var c in Find.ListerBuildings.AllBuildingsColonistOfClass<Building_DroidChargePad>())
                {
                    list.Add((Thing)c);
                }
                chargers = list.AsEnumerable();
                Predicate<Thing> pred = (Thing thing) => { return ((Building_DroidChargePad)thing).IsAvailable(droid); };
                Thing target = GenClosest.ClosestThing_Global_Reachable(pawn.Position, chargers, PathEndMode.OnCell, TraverseParms.For(pawn), distance, pred);
                if (target != null)
                {
                    return new Job(DefDatabase<JobDef>.GetNamed("MD2ChargeDroid"), new TargetInfo(target));
                }
            }
            return null;
        }
    }

    public class JobGiver_ChargeStaySafe : JobGiver_DroidCharge
    {
        public JobGiver_ChargeStaySafe() : base(1) { }
    }
    public class JobGiver_ChargeLow : JobGiver_DroidCharge
    {
        public JobGiver_ChargeLow() : base(2) { }
    }
    public class JobGiver_ChargeCritical : JobGiver_DroidCharge
    {
        public JobGiver_ChargeCritical() : base(3) { }
    }
}
