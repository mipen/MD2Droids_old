using Backstories;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MD2
{
    public class ListerDroids : MapComponent
    {
        public static ListerDroids listerDroids;
        private List<Droid> allDroids = new List<Droid>();
        private Dictionary<BackstoryDef, int> droidTypesDict = new Dictionary<BackstoryDef, int>();

        public ListerDroids()
        {
            ListerDroids.listerDroids = this;
        }

        public static List<Droid> AllDroids
        {
            get
            {
                return listerDroids.allDroids;
            }
        }

        public static void RegisterDroid(Droid droid)
        {
            if (!listerDroids.allDroids.Contains(droid))
                listerDroids.allDroids.Add(droid);
        }

        public static void DeregisterDroid(Droid droid)
        {
            if (listerDroids.allDroids.Contains(droid))
                listerDroids.allDroids.Remove(droid);
        }

        public static string GetNumberedNameFor(BackstoryDef backstoryDef, string titleShort)
        {
            int num = 1;
            if (!listerDroids.droidTypesDict.TryGetValue(backstoryDef, out num))
            {
                num = 1;
                listerDroids.droidTypesDict.Add(backstoryDef, 2);
            }
            else
            {
                listerDroids.droidTypesDict[backstoryDef]++;
            }
            return titleShort + " " + num.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            try
            {
                Scribe_Collections.LookDictionary(ref droidTypesDict, "droidTypesDict", LookMode.DefReference, LookMode.Value);
            }
            catch
            {
                Log.Warning("Unable to load ListerDroids dictionary. Resetting it... (This message is harmless)");
                droidTypesDict = new Dictionary<BackstoryDef, int>();
            }
        }


    }
}
