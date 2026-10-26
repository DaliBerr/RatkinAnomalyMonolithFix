using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using Verse;

namespace RatkinAnomalyMonolithFix.Core
{
    public static class CoreUtilities
    {
        public static void ShowPopup(string text, Action onConfirm = null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(text, "OK".Translate(), onConfirm));
        }

        internal static bool FinalizeDisruption(Pawn pawn)
        {
            Building_VoidMonolith monolith = Find.Anomaly?.monolith;
            if (monolith == null || monolith.Destroyed || !monolith.Spawned) return false;

            Find.Anomaly.SetLevel(MonolithLevelDefOf.Disrupted);
            if (pawn != null && !pawn.Destroyed)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.ClosedTheVoid);
                TaleRecorder.RecordTale(TaleDefOf.ClosedTheVoid, pawn);
            }
#if RIMWORLD_1_5
            var colonists = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction;
#else
            var colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
#endif
            foreach (Pawn item in colonists)
            {
                if (item.Inhumanized()) item.Rehumanize();
                item.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.VoidClosed);
                if (pawn != null && !pawn.Destroyed)
                    item.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.ClosedTheVoidOpinion, pawn);
            }

            // Finish this route directly: do not re-enter DisruptTheLink,
            // close Metal Hell a second time or replay the victory credits.
            Find.Anomaly.metalHellReturnLetterText = pawn != null && !pawn.Destroyed
                ? "DisruptedLinkText".Translate(pawn.Named("PAWN"))
                : "DisruptedLinkTextSuccessText".Translate();
            monolith.Collapse();
            ShowPopup("DisruptedLinkTextSuccessText".Translate());
            return true;
        }

        public static class CaptureFunction
        {
            internal static void CaptureCloseMetalHellFunc()
            {
                if (FuncCloseMetalHell == null)
                    FuncCloseMetalHell = AccessTools.MethodDelegate<Action<Pawn>>(
                        AccessTools.Method(typeof(VoidAwakeningUtility), "CloseMetalHell", new[] { typeof(Pawn) }));
            }
            internal static Action<Pawn> FuncCloseMetalHell;
        }
    }

    public static class Data
    {
        public static bool ifRatkinEnd;
        public static bool ifDisruptTheLink;
        public static Pawn HeroPawn;

        public static void Reset()
        {
            ifRatkinEnd = false;
            ifDisruptTheLink = false;
            HeroPawn = null;
        }
    }

    public class ComponentData : GameComponent
    {
        public ComponentData(Game game)
        {
            Data.Reset();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Keep these keys and this component's full name for old saves.
            Scribe_Values.Look(ref Data.ifRatkinEnd, "ifRatkinEnd", false);
            Scribe_Values.Look(ref Data.ifDisruptTheLink, "ifDisruptTheLink", false);
            Scribe_References.Look(ref Data.HeroPawn, "HeroPawn", false);
        }
    }
}
