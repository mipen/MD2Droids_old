using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MD2
{
    public static class DroidJobDefs
    {
        public static JobDef CremateJob
        {
            get
            {
                return DefDatabase<JobDef>.GetNamed("MD2DroidCremateCorpse");
            }
        }
    }
}
