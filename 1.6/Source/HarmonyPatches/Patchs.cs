using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using Verse;
using RatkinAnomalyMonolithFix.Core;

namespace RatkinAnomalyMonolithFix.HarmonyPatches
{
    public static class Patches
    {
        [HarmonyPatch(typeof(VoidAwakeningUtility), nameof(VoidAwakeningUtility.DisruptTheLink))]
        public static class Prefix_DisruptTheLink
        {
            [HarmonyPrefix]
            public static bool DisruptTheLink_Prefix(Pawn pawn)
            {
                // After the Ratkin ending, the original game's ending is valid.
                // Missing context must not start a partially completed ending.
                if (Data.ifRatkinEnd || pawn == null || Find.Anomaly?.monolith == null
                    || Find.Anomaly.monolith.Destroyed) return true;

                CoreUtilities.CaptureFunction.CaptureCloseMetalHellFunc();
                CoreUtilities.CaptureFunction.FuncCloseMetalHell(pawn);
                Data.HeroPawn = pawn;
                Data.ifDisruptTheLink = true;
                Find.Anomaly.monolith.quest?.End(QuestEndOutcome.Success, sendLetter: true, playSound: false);
                Find.Anomaly.SetLevel(MonolithLevelDefOf.Embraced);
                CoreUtilities.ShowPopup("DisruptTheLinkFailedByRatkinAnomalyText".Translate());
                GameVictoryUtility.ShowCredits("DisruptTheLinkCredits".Translate(pawn.Named("PAWN")), null);
                return false;
            }
        }

        [HarmonyPatch]
        public static class Postfix_EndPlay
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                // Locate the installed Ratkin version instead of binding this
                // assembly to a different version's game/engine references.
                return AccessTools.Method(AccessTools.TypeByName("RatkinAnomaly.RAComponent"), "EndPlay", Type.EmptyTypes);
            }

            [HarmonyPrefix]
            public static void EndPlay_Prefix(object __instance, out Pawn __state)
            {
                // Ratkin's EndPlay clears actor before postfixes execute.
                __state = __instance == null ? null
                    : AccessTools.Field(__instance.GetType(), "actor")?.GetValue(__instance) as Pawn;
            }

            [HarmonyPostfix]
            public static void EndPlay_Postfix(Pawn __state)
            {
                Data.ifRatkinEnd = true;
                Pawn hero = Data.HeroPawn;
                if (hero == null || hero.Destroyed) hero = __state;
                if (CoreUtilities.FinalizeDisruption(hero))
                {
                    Data.ifDisruptTheLink = true;
                    if (hero != null && !hero.Destroyed) Data.HeroPawn = hero;
                }
            }
        }
    }
}
