﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Backstories;


namespace MD2
{
    public class DroidKindDef : PawnKindDef
    {
        public List<WorkTypeDef> allowedWorkTypeDefs;
        public BackstoryDef backstoryDef;
        public string headGraphicPath;
        public float maxEnergy;
        public float EnergyUseRate = 200f;
        public SettingsDef Settings;
        public bool explodeOnDeath = true;
        public float explosionRadius = 0.1f;
        public bool disableOnSolarFlare = false;
        public bool canManThings = true;
    }
}
