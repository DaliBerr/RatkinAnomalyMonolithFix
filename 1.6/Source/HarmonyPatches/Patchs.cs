using Verse;
using HarmonyLib;
using RimWorld;
using RatkinAnomaly;
using System.Linq;
using RimWorld.Utility;
using System.Drawing.Text;
using System;
using RatkinAnomalyMonolithFix.Core;
namespace RatkinAnomalyMonolithFix.HarmonyPatches
{

    public static class Patches
    {
        // public static bool ifRatkinEnd = false;
        // public static bool ifDisruptTheLink = false;
        // public static Pawn HeroPawn = null;
        [HarmonyPatch(typeof(VoidAwakeningUtility), "DisruptTheLink")]
        public static class Prefix_DisruptTheLink
        {
            [HarmonyPrefix]
            public static bool DisruptTheLink_Prefix(Pawn pawn)
            {
                Data.ifDisruptTheLink = true;
                if (!Data.ifRatkinEnd)
                {
                    Data.HeroPawn = pawn;
                    CoreUtilities.CaptureFunction.CaptureCloseMetalHellFunc();
                    CoreUtilities.CaptureFunction.FuncCloseMetalHell(pawn);
                    Find.Anomaly.monolith.quest.End(QuestEndOutcome.Success, sendLetter: true, playSound: false);
                    Find.Anomaly.SetLevel(MonolithLevelDefOf.Embraced);
                    CoreUtilities.ShowPopup("DisruptTheLinkFailedByRatkinAnomalyText".Translate(), () =>
                    {
                        Log.Message("[RatkinAnomalyMonolithFix] DisruptTheLink Failed By RatkinAnomaly");
                        // Log.Warning("[RatkinAnomalyMonolithFix] Flags States: " + "ifRatkinEnd" + Data.ifRatkinEnd + "ifDisruptTheLink" + Data.ifDisruptTheLink + "HeroPawn" + Data.HeroPawn.Name);
                    });
                    GameVictoryUtility.ShowCredits("DisruptTheLinkCredits".Translate(pawn.Named("PAWN")), null);
                    return false;
                }
                else
                {
                    // Find.Anomaly.monolith.quest.End(QuestEndOutcome.Success, sendLetter: true, playSound: false);
                    Find.Anomaly.SetLevel(MonolithLevelDefOf.Disrupted);
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.ClosedTheVoid);
                    TaleRecorder.RecordTale(TaleDefOf.ClosedTheVoid, pawn);
                    foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction)
                    {
                        if (item.Inhumanized())
                        {
                            item.Rehumanize();
                        }

                        item.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.VoidClosed);
                        item.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.ClosedTheVoidOpinion, pawn);
                    }

                    Find.Anomaly.monolith.Collapse();
                    // GameVictoryUtility.ShowCredits("DisruptTheLinkCredits".Translate(pawn.Named("PAWN")), null);
                    Find.Anomaly.metalHellReturnLetterText = "DisruptedLinkText".Translate(pawn.Named("PAWN"));
                    CoreUtilities.ShowPopup("DisruptedLinkTextSuccessText".Translate(), () =>
                    {
                        Log.Message("[RatkinAnomalyMonolithFix] DisruptedLinkText Success");
                        // Log.Warning("[RatkinAnomalyMonolithFix] Flags States: " + "ifRatkinEnd" + Data.ifRatkinEnd + "ifDisruptTheLink" + Data.ifDisruptTheLink + "HeroPawn" + Data.HeroPawn.Name);
                    });
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(RatkinAnomaly.RAComponent), "EndPlay")]
        public static class Postfix_EndPlay
        {
            [HarmonyPostfix]
            public static void EndPlay_Postfix(RAComponent __instance)
            {
                Data.ifRatkinEnd = true;
                VoidAwakeningUtility.DisruptTheLink(Data.HeroPawn);
                return;
            }
        }
        

    }
}
