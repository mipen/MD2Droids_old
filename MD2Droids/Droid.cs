using RimWorld;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;


namespace MD2
{
    public class Droid : Pawn, InternalCharge, IRepairable
    {
        public DroidUIOverlay overlay;

        private readonly Texture2D SDIcon = ContentFinder<Texture2D>.Get("UI/Commands/SelfDestructIcon");
        private readonly Texture2D StartIcon = ContentFinder<Texture2D>.Get("UI/Commands/BeginUI");
        private readonly Texture2D StopIcon = ContentFinder<Texture2D>.Get("UI/Commands/PauseUI");
        public Graphic bodyGraphic;
        public Graphic headGraphic;
        private float totalCharge = 1000f;
        public bool ChargingNow = false;
        public bool RequiresPower = true;
        protected bool active = true;

        private bool beingRepaired = false;
        private Building_RepairStation repairStation;

        public override void SpawnSetup()
        {
            ListerDroids.RegisterDroid(this);
            base.SpawnSetup();
            this.bodyGraphic = GraphicDatabase.Get<Graphic_Multi>(KindDef.standardBodyGraphicPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
            if (!KindDef.headGraphicPath.NullOrEmpty())
                this.headGraphic = GraphicDatabase.Get<Graphic_Multi>(KindDef.headGraphicPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
            DoGraphicChanges();
        }

        public float MaxEnergy
        {
            get
            {
                return KindDef.maxEnergy;
            }
        }

        public override void Tick()
        {
            RefillNeeds();
            CureDiseases();
            CheckPowerRemaining();
            ResetDrafter();
            base.Tick();
            if (!Active || BeingRepaired || (KindDef.disableOnSolarFlare && Find.MapConditionManager.ConditionIsActive(MapConditionDefOf.SolarFlare)))
            {
                Inactive();
            }
            else
            {
                if (!ChargingNow && !BeingRepaired)
                {
                    Deplete(KindDef.EnergyUseRate);
                }
            }
        }

        private void ResetDrafter()
        {
            if (this.playerController.Drafted)
            {
                if (Find.TickManager.TicksGame % 4800 == 0)
                {
                    AutoUndrafter undrafter = (AutoUndrafter)typeof(Drafter).GetField("autoUndrafter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this.playerController.drafter);
                    if (undrafter != null)
                    {
                        typeof(AutoUndrafter).GetField("lastNonWaitingTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(undrafter, Find.TickManager.TicksGame);
                        //Log.Message("Reset ticks " + ((int)typeof(AutoUndrafter).GetField("lastNonWaitingTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(undrafter)).ToString());
                    }
                }
            }
        }

        private void Inactive()
        {
            if (pather != null && pather.Moving)
                pather.StopDead();
            if (!BeingRepaired && (jobs.curJob == null || jobs.curJob.def != JobDriver_DroidDeactivated.Def) && !stances.FullBodyBusy)
            {
                jobs.StartJob(new Job(JobDriver_DroidDeactivated.Def), JobCondition.InterruptForced);
            }
        }

        public bool ToggleActive(bool state)
        {
            this.active = state;
            if (!active)
            {
                jobs.StopAll();
                jobs.StartJob(new Job(JobDriver_DroidDeactivated.Def));
            }
            else
            {
                jobs.StopAll();
            }
            return active;
        }

        public bool ToggleActive()
        {
            return ToggleActive(!active);
        }

        private void RefillNeeds()
        {
            if (Find.TickManager.TicksGame % 180 == 0)
            {
                foreach (Need n in this.needs.AllNeeds)
                {
                    if (n.CurLevel < 90)
                    {
                        n.CurLevel = 100f;
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<bool>(ref this.ChargingNow, "chargingNow");
            Scribe_Values.LookValue<float>(ref this.totalCharge, "TotalCharge");
            Scribe_Values.LookValue<bool>(ref this.active, "active");
        }

        public override IEnumerable<FloatMenuOption> GetExtraFloatMenuOptionsFor(IntVec3 sq)
        {
            foreach (var o in this.ExtraFloatMenuOptions(sq, base.GetExtraFloatMenuOptionsFor(sq)))
            {
                yield return o;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder str = new StringBuilder();
            //str.AppendLine(jobs.curJob.def.reportString);
            str.Append(base.GetInspectString());
            str.AppendLine(string.Format("Current energy: {0}W/{1}Wd", TotalCharge.ToString("0.0"), MaxEnergy));
            return str.ToString();
        }

        public override TipSignal GetTooltip()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine(this.LabelCap + " " + this.kindDef.label);
            s.AppendLine("Current energy: " + this.TotalCharge.ToString("0.0") + "W/" + this.MaxEnergy.ToString() + "Wd");
            if (this.equipment != null && this.equipment.Primary != null)
            {
                s.AppendLine(this.equipment.Primary.LabelCap);
            }
            s.AppendLine(HealthUtility.GetGeneralConditionLabel(this));
            return new TipSignal(s.ToString().TrimEnd(new char[]
    {
        '\n'
    }), this.thingIDNumber * 152317, TooltipPriority.Pawn);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (base.GetGizmos() != null)
            {
                foreach (Gizmo c in base.GetGizmos())
                {
                    yield return c;
                }
            }
            Command_Action com = new Command_Action();
            com.activateSound = SoundDefOf.Click;
            if (this.active)
            {
                com.defaultDesc = "Click to deactivate this droid and save power.";
                com.defaultLabel = "Deactivate";
                com.icon = this.StopIcon;
            }
            else
            {
                com.defaultDesc = "Click to activate this droid.";
                com.defaultLabel = "Activate";
                com.icon = this.StartIcon;
            }
            com.disabled = false;
            com.groupKey = 313740008;
            com.hotKey = Keys.DeactivateDroid;
            com.action = () =>
            {
                this.ToggleActive();
            };
            yield return com;

            Command_Action a = new Command_Action();
            a.action = () =>
            {
                Find.LayerStack.Add(new Dialog_Confirm("DroidSelfDestructPrompt".Translate(), delegate
                    {
                        this.Destroy(DestroyMode.Kill);
                    }));
            };
            a.activateSound = SoundDefOf.Click;
            a.defaultDesc = "Click this button to cause the droid to self destruct";
            a.defaultLabel = "Self Destruct";
            a.disabled = false;
            a.groupKey = 313740004;
            a.icon = this.SDIcon;
            yield return a;
        }

        public SettingsDef Settings
        {
            get
            {
                return ((DroidKindDef)kindDef).Settings;
            }
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            overlay.DrawGUIOverlay();
        }

        public virtual void DoGraphicChanges()
        {
            overlay = new DroidUIOverlay(this);
            this.drawer.renderer.graphics.ResolveGraphics();
            this.story.hairDef = DefDatabase<HairDef>.GetNamed("Shaved", true);
            if (headGraphic != null)
            {
                this.drawer.renderer.graphics.headGraphic = this.headGraphic;
                this.drawer.renderer.graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(this.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, this.story.hairColor);
            }
            this.drawer.renderer.graphics.nakedGraphic = this.bodyGraphic;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ListerDroids.DeregisterDroid(this);

            base.Destroy(mode);
            if (mode == DestroyMode.Kill && this.KindDef.explodeOnDeath)
            {
                Messages.Message(this.LabelBase + " was destroyed!", MessageSound.Negative);
                GenExplosion.DoExplosion(this.Position, KindDef.explosionRadius, DamageDefOf.Bomb, this);
            }

        }

        public virtual void CheckPowerRemaining()
        {
            if (TotalCharge < 1f || Downed)
            {
                this.Destroy(DestroyMode.Kill);
            }
        }

        public virtual DroidKindDef KindDef
        {
            get
            {
                return this.kindDef as DroidKindDef;
            }
        }

        public bool Active
        {
            get
            {
                return this.active;
            }
        }

        private void CureDiseases()
        {
            IEnumerable<Hediff_Staged> diseases = this.health.hediffSet.GetDiseases();
            if (diseases != null)
            {
                foreach (Hediff_Staged d in diseases)
                {
                    d.DirectHeal(1000f);
                }
            }
        }

        public SettingsDef Config
        {
            get
            {
                return ((DroidKindDef)this.kindDef).Settings;
            }
        }

        public float TotalCharge
        {
            get { return this.totalCharge; }
            set { this.totalCharge = value; }
        }

        public bool AddPowerDirect(float amount)
        {
            TotalCharge += amount;
            if (TotalCharge > MaxEnergy)
            {
                TotalCharge = MaxEnergy;
                return false;
            }
            return true;
        }

        public bool RemovePowerDirect(float amount)
        {
            TotalCharge -= amount;
            if (TotalCharge < 0)
            {
                TotalCharge = 0f;
                return false;
            }
            return true;
        }

        public bool Charge(float rate)
        {

            if (TotalCharge < MaxEnergy)
            {
                TotalCharge += (rate * CompPower.WattsToWattDaysPerTick * 2);
                if (TotalCharge > MaxEnergy)
                    TotalCharge = MaxEnergy;
                return true;
            }
            return false;

        }

        public bool Deplete(float rate)
        {
            if (TotalCharge > 0)
            {
                TotalCharge -= (rate * CompPower.WattsToWattDaysPerTick);
                if (TotalCharge < 0)
                    TotalCharge = 0;
                return true;
            }
            return false;
        }

        public bool DesiresCharge()
        {
            return this.TotalCharge < MaxEnergy;
        }

        public virtual bool BeingRepaired
        {
            get
            {
                return beingRepaired;
            }
            set
            {
                beingRepaired = value;
            }
        }

        public virtual Building_RepairStation RepairStation
        {
            get
            {
                if (repairStation != null)
                    return repairStation;
                else
                    throw new MissingReferenceException(this.ToString() + " tried to access its RepairStation when it was null");
            }
            set
            {
                repairStation = value;
            }
        }

        public virtual void RepairTick()
        {
            List<Hediff_Injury> allInjuries = health.hediffSet.GetHediffs<Hediff_Injury>().ToList();
            List<Hediff_MissingPart> allMissingParts = health.hediffSet.GetHediffs<Hediff_MissingPart>().ToList();

            float num = Rand.Value;

            if (num > 0.6 && allMissingParts.Count > 0 && RepairStation != null && RepairStation.HasEnoughOf(RepairStation.Def.repairThingDef, RepairStation.Def.repairCostAmount))
            {
                Hediff_MissingPart hediff = allMissingParts.RandomElement();
                if (RepairStation.TakeSomeOf(RepairStation.Def.repairThingDef, RepairStation.Def.repairCostAmount))
                {
                    health.hediffSet.RestorePart(hediff.Part);
                }
            }
            else if (allInjuries.Count > 0)
            {
                Hediff_Injury hediff = allInjuries.RandomElement();
                if (hediff.def.injuryProps.fullyHealableOnlyByTreatment)
                {
                    HediffComp_Treatable treatable = hediff.TryGetComp<HediffComp_Treatable>();
                    if (treatable != null && !treatable.treatedWithMedicine)
                    {
                        treatable.NewlyTreated(1f, ThingDefOf.Medicine);
                    }
                }
                hediff.DirectHeal(RepairStation.Def.repairAmount);
            }
        }

        public virtual bool ShouldGetRepairs
        {
            get
            {
                return (health.hediffSet.GetHediffs<Hediff_Injury>().Count() > 0 ||
                    health.hediffSet.HasFreshMissingPartsCommonAncestor() ||
                    health.hediffSet.GetHediffs<Hediff_MissingPart>().Count() > 0) &&
                    ((this.Faction == Faction.OfColony && !this.IsPrisonerOfColony) || (this.guest != null && this.guest.DoctorsCare));
            }
        }

        public Pawn Pawn
        {
            get
            {
                return this;
            }
        }

        public virtual int RepairsNeededCount
        {
            get
            {
                return health.hediffSet.GetHediffs<Hediff_Injury>().Count() + health.hediffSet.GetHediffs<Hediff_MissingPart>().Count();
            }
        }

        public override string LabelBase
        {
            get
            {
                return this.Nickname;
            }
        }

        public override string LabelBaseShort
        {
            get
            {
                return this.Nickname;
            }
        }

        public override string LabelCap
        {
            get
            {
                return this.Nickname;
            }
        }
    }
}
