using Verse;
using HarmonyLib;
using RimWorld.Utility;
using System;
using RatkinAnomalyMonolithFix.Core;

namespace RatkinAnomalyMonolithFix.Startup
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            if (!ModsConfig.IsActive("fxz.ratkinanomaly.update"))
            {
                Log.Warning("[RatkinAnomalyMonolithFix] RatkinAnomaly not found,patch disabled");
                return;
            }
            if (Data.ifRatkinEnd == false)
            {
                Log.Warning("[RatkinAnomalyMonolithFix] RatkinAnomaly not finished");
            }
            var harmony = new Harmony("com.RatkinAnomalyMonolithFix");
            harmony.PatchAll();
            // Log.Message("[RatkinAnomalyMonolithFix] Harmony patches applied");
        }

    }
    

}
