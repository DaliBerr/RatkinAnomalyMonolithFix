// Only engine services used by the linked production code are modeled here.
// Ratkin EndPlay's actor cleanup follows the supplied 1.5/1.6/1.7 sources.
using System.Reflection;
using System.Xml.Linq;

namespace Verse
{
    public class Game { }
    public class GameComponent { public virtual void ExposeData() { } }
    public class Pawn
    {
        public string Id = "pawn";
        public bool Destroyed;
        public bool IsInhumanized;
        public Needs needs = new();
        public bool Inhumanized() => IsInhumanized;
        public void Rehumanize() => IsInhumanized = false;
        public object Named(string name) => this;
    }
    public class Needs { public Mood mood = new(); }
    public class Mood { public Thoughts thoughts = new(); }
    public class Thoughts { public Memories memories = new(); }
    public class Memories
    {
        public List<string> Names = new();
        public void TryGainMemory(string name, Pawn other = null) => Names.Add(name);
    }
    public static class Find
    {
        public static RimWorld.AnomalyState Anomaly;
        public static WindowStack WindowStack;
    }
    public class WindowStack
    {
        public List<Dialog_MessageBox> Windows = new();
        public void Add(Dialog_MessageBox box) => Windows.Add(box);
    }
    public class Dialog_MessageBox
    {
        public Dialog_MessageBox(string text, string label, Action callback = null) { }
    }
    public static class Translation
    {
        public static string Translate(this string key, params object[] args) => key;
    }
    public enum LoadSaveMode { Inactive, Saving, LoadingVars }
    public static class Scribe
    {
        public static LoadSaveMode mode;
        public static XElement Data;
        public static Dictionary<string, Pawn> Pawns = new();
    }
    public static class Scribe_Values
    {
        public static void Look(ref bool value, string key, bool defaultValue)
        {
            if (Scribe.mode == LoadSaveMode.Saving) Scribe.Data.Add(new XElement(key, value));
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                value = Scribe.Data.Element(key) is XElement element ? bool.Parse(element.Value) : defaultValue;
        }
    }
    public static class Scribe_References
    {
        public static void Look(ref Pawn pawn, string key, bool saveDestroyed)
        {
            if (Scribe.mode == LoadSaveMode.Saving) Scribe.Data.Add(new XElement(key, pawn?.Id ?? "null"));
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                pawn = Scribe.Pawns.GetValueOrDefault(Scribe.Data.Element(key)?.Value ?? "null");
        }
    }
}
namespace RimWorld
{
    public enum QuestEndOutcome { Success }
    public class Quest
    {
        public int EndCalls;
        public void End(QuestEndOutcome outcome, bool sendLetter, bool playSound) => EndCalls++;
    }
    public class Building_VoidMonolith
    {
        public bool Destroyed;
        public bool Spawned = true;
        public Quest quest = new();
        public int CollapseCalls;
        public void Collapse() { CollapseCalls++; Destroyed = true; }
    }
    public class MonolithLevelDef { }
    public static class MonolithLevelDefOf
    {
        public static MonolithLevelDef Embraced = new();
        public static MonolithLevelDef Disrupted = new();
    }
    public class AnomalyState
    {
        public Building_VoidMonolith monolith;
        public MonolithLevelDef Level;
        public string metalHellReturnLetterText;
        public void SetLevel(MonolithLevelDef level) => Level = level;
    }
    public static class ThoughtDefOf
    {
        public const string ClosedTheVoid = "ClosedTheVoid", VoidClosed = "VoidClosed", ClosedTheVoidOpinion = "ClosedTheVoidOpinion";
    }
    public static class TaleDefOf { public const string ClosedTheVoid = "ClosedTheVoid"; }
    public static class TaleRecorder
    {
        public static int Count;
        public static void RecordTale(string tale, params object[] args) => Count++;
    }
    public static class PawnsFinder
    {
        public static List<Verse.Pawn> Pawns = new();
        public static IEnumerable<Verse.Pawn> AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction => Pawns;
        public static IEnumerable<Verse.Pawn> AllMapsCaravansAndTravellingTransporters_Alive_Colonists => Pawns;
    }
    public static class GameVictoryUtility
    {
        public static int Credits;
        public static void ShowCredits(string text, object callback) => Credits++;
    }
}
namespace RimWorld.Utility
{
    public static class VoidAwakeningUtility
    {
        public static int CloseCalls, VanillaDisruptCalls;
        public static void DisruptTheLink(Verse.Pawn pawn) => VanillaDisruptCalls++;
        private static void CloseMetalHell(Verse.Pawn pawn) => CloseCalls++;
    }
}
namespace RatkinAnomaly
{
    public class RAComponent
    {
        public Verse.Pawn actor;
        public void EndPlay() => actor = null;
    }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { }
    }
    public class HarmonyPrefix : Attribute { }
    public class HarmonyPostfix : Attribute { }
    public class HarmonyTargetMethod : Attribute { }
    public static class AccessTools
    {
        public static Type TypeByName(string name) => typeof(RatkinAnomaly.RAComponent).Assembly.GetType(name);
        public static MethodInfo Method(Type type, string name, Type[] parameters = null) => parameters == null
            ? type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            : type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, parameters);
        public static FieldInfo Field(Type type, string name) => type.GetField(name);
        public static T MethodDelegate<T>(MethodInfo method) where T : Delegate => (T)method.CreateDelegate(typeof(T));
    }
}
