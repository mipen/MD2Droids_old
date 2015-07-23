﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MD2
{
    public static class DroidWorkTypeDefs
    {
        public static IEnumerable<WorkTypeDef> DroidWorkTypeDefsInPriorityOrder
        {
            get
            {
                return (from d in DefDatabase<WorkTypeDef>.AllDefs
                        where d.visible && d != DefDatabase<WorkTypeDef>.GetNamed("Patient", false) || d == DefDatabase<WorkTypeDef>.GetNamed("MD2Maintenance", false)
                        orderby d.naturalPriority descending
                        select d);
                        
            }
        }
    }
}
