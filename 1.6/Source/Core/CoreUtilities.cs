using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.ComponentModel;
using Verse;

namespace RatkinAnomalyMonolithFix.Core
{
    public static class CoreUtilities
    {
        public static void ShowPopup(string text, Action onConfirm = null)
        {
            Dialog_MessageBox dialog = new Dialog_MessageBox(
                text,
                "OK",
                onConfirm,
                null,
                null,
                null,
                false,
                null);
            Find.WindowStack.Add(dialog);
        }

        public static class CaptureFunction
        {
            internal static void CaptureCloseMetalHellFunc()
            {
                FuncCloseMetalHell = AccessTools.MethodDelegate<Action<Pawn>>(
                    AccessTools.Method(
                        typeof(VoidAwakeningUtility),
                        "CloseMetalHell",
                        new Type[] { typeof(Pawn) }
                    )
                );
            }
            internal static Action<Pawn> FuncCloseMetalHell;
        }
    }
    public class Data
    {
        public static bool ifRatkinEnd = false;
        public static bool ifDisruptTheLink = false;
        public static Pawn HeroPawn = null;
    }
    public class ComponentData : GameComponent
    {
        public ComponentData(Game game)
        {
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Data.ifRatkinEnd, "ifRatkinEnd", false);
            Scribe_Values.Look(ref Data.ifDisruptTheLink, "ifDisruptTheLink", false);
            Scribe_References.Look(ref Data.HeroPawn, "HeroPawn", false);
        }
    }
}