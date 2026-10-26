using RatkinAnomaly;
using RatkinAnomalyMonolithFix.Core;
using RatkinAnomalyMonolithFix.HarmonyPatches;
using RimWorld;
using RimWorld.Utility;
using Verse;
using System.Xml.Linq;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
ComponentData Setup()
{
    Find.Anomaly = new AnomalyState { monolith = new Building_VoidMonolith() };
    Find.WindowStack = new();
    PawnsFinder.Pawns = new();
    GameVictoryUtility.Credits = 0;
    VoidAwakeningUtility.CloseCalls = 0;
    VoidAwakeningUtility.VanillaDisruptCalls = 0;
    TaleRecorder.Count = 0;
    return new ComponentData(new Game());
}
void EndRatkin(RAComponent component)
{
    Patches.Postfix_EndPlay.EndPlay_Prefix(component, out Pawn state);
    component.EndPlay();
    Patches.Postfix_EndPlay.EndPlay_Postfix(state);
}

// The intercepted original ending preserves the monolith and records its hero.
ComponentData saveComponent = Setup();
Pawn hero = new() { Id = "original-hero" };
Pawn colonist = new() { Id = "colonist", IsInhumanized = true };
PawnsFinder.Pawns.Add(colonist);
Check(!Patches.Prefix_DisruptTheLink.DisruptTheLink_Prefix(hero), "Original ending was not intercepted");
Check(VoidAwakeningUtility.CloseCalls == 1, "Metal Hell was not closed exactly once");
Check(Find.Anomaly.monolith.quest.EndCalls == 1, "Original quest was not completed");
Check(Find.Anomaly.Level == MonolithLevelDefOf.Embraced, "Ratkin ending was not kept accessible");
Check(!Find.Anomaly.monolith.Destroyed, "Monolith collapsed before Ratkin ending");
Check(Data.ifDisruptTheLink && !Data.ifRatkinEnd && Data.HeroPawn == hero, "Initial ending state was lost");
Check(GameVictoryUtility.Credits == 1 && Find.WindowStack.Windows.Count == 1, "Initial ending UI was not shown once");

// Save and load the actual production component with the legacy field names.
Scribe.mode = LoadSaveMode.Saving;
Scribe.Data = new XElement("component");
Scribe.Pawns[hero.Id] = hero;
saveComponent.ExposeData();
string xml = Scribe.Data.ToString();
Check(xml.Contains("ifRatkinEnd") && xml.Contains("ifDisruptTheLink") && xml.Contains("HeroPawn"), "Legacy save keys changed");
ComponentData loaded = new(new Game());
Check(!Data.ifRatkinEnd && !Data.ifDisruptTheLink && Data.HeroPawn == null, "New game inherited static flags");
Scribe.mode = LoadSaveMode.LoadingVars;
Scribe.Data = XElement.Parse(xml);
loaded.ExposeData();
Scribe.mode = LoadSaveMode.Inactive;
Check(Data.ifDisruptTheLink && !Data.ifRatkinEnd && Data.HeroPawn == hero, "Saved ending state was not restored");

RAComponent ratkin = new() { actor = new Pawn { Id = "later-actor" } };
EndRatkin(ratkin);
Check(ratkin.actor == null, "Test dependency did not clear actor like real EndPlay");
Check(Data.ifRatkinEnd && Find.Anomaly.Level == MonolithLevelDefOf.Disrupted, "Ratkin ending was not finalized");
Check(Find.Anomaly.monolith.CollapseCalls == 1, "Monolith did not collapse exactly once");
Check(Data.HeroPawn == hero, "Original hero was replaced despite a valid saved reference");
Check(VoidAwakeningUtility.CloseCalls == 1 && VoidAwakeningUtility.VanillaDisruptCalls == 0, "Completion re-entered the original ending");
Check(GameVictoryUtility.Credits == 1, "Victory credits replayed");
Check(!colonist.IsInhumanized && colonist.needs.mood.thoughts.memories.Names.Contains("VoidClosed"), "Colony ending effects were lost");
Check(TaleRecorder.Count == 1, "Hero tale was not recorded exactly once");
EndRatkin(ratkin);
Check(Find.Anomaly.monolith.CollapseCalls == 1 && TaleRecorder.Count == 1 && Find.WindowStack.Windows.Count == 2,
    "Duplicate Ratkin callback replayed completion effects");
Check(Patches.Prefix_DisruptTheLink.DisruptTheLink_Prefix(hero), "Vanilla ending was blocked after Ratkin completion");

// An embraced-void route has no saved hero; capture the Ratkin actor before
// EndPlay clears it and preserve the original mod's final collapse behavior.
Setup();
Pawn actor = new() { Id = "ratkin-actor" };
EndRatkin(new RAComponent { actor = actor });
Check(Data.HeroPawn == actor && Data.ifRatkinEnd && Data.ifDisruptTheLink, "Normal Ratkin ending lost its actor");
Check(Find.Anomaly.monolith.CollapseCalls == 1 && TaleRecorder.Count == 1, "Normal Ratkin ending did not finish");
Check(GameVictoryUtility.Credits == 0 && VoidAwakeningUtility.CloseCalls == 0, "Normal Ratkin ending replayed an original ending");

Setup();
Data.HeroPawn = new Pawn { Destroyed = true };
EndRatkin(new RAComponent { actor = actor });
Check(Data.HeroPawn == actor && Find.Anomaly.monolith.Destroyed, "Destroyed hero did not fall back to the event actor");

Setup();
EndRatkin(new RAComponent());
Check(Find.Anomaly.monolith.Destroyed && TaleRecorder.Count == 0, "Missing all hero references prevented safe completion");

Setup();
Find.Anomaly.monolith.quest = null;
Check(!Patches.Prefix_DisruptTheLink.DisruptTheLink_Prefix(hero), "Missing legacy quest prevented the first ending");
Check(Data.ifDisruptTheLink && !Find.Anomaly.monolith.Destroyed, "Missing legacy quest corrupted ending state");

Setup();
Find.Anomaly.monolith = null;
Check(Patches.Prefix_DisruptTheLink.DisruptTheLink_Prefix(hero), "Missing monolith was intercepted");
EndRatkin(new RAComponent { actor = actor });
Check(Find.WindowStack.Windows.Count == 0 && TaleRecorder.Count == 0, "Missing monolith produced completion effects");

Setup();
Check(Patches.Prefix_DisruptTheLink.DisruptTheLink_Prefix(null) && !Data.ifDisruptTheLink, "Null first-ending pawn modified state");
Find.Anomaly = null;
EndRatkin(new RAComponent());
Check(Data.ifRatkinEnd, "Missing Anomaly context caused an exception");

Setup();
Data.ifRatkinEnd = Data.ifDisruptTheLink = true;
Data.HeroPawn = hero;
_ = new ComponentData(new Game());
Check(!Data.ifRatkinEnd && !Data.ifDisruptTheLink && Data.HeroPawn == null, "New-game state was contaminated");
Check(Patches.Postfix_EndPlay.TargetMethod()?.DeclaringType == typeof(RAComponent), "Dynamic Ratkin patch target was not resolved");
Console.WriteLine($"PASS: {checks} production ending, save/load, null-reference and repeat-callback checks.");
