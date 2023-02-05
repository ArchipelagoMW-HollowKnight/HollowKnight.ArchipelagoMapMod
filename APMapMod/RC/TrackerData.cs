using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archipelago.HollowKnight.SlotData;
using Newtonsoft.Json;
using RandomizerCore;
using RandomizerCore.Logic;

namespace APMapMod.RC;

// class for storing data related to seed progress
// updating handled through IC.TrackerUpdate
// Note: tracking may fail if placement names do not match the corresponding RandoLocation names in the RawSpoiler.
// tracking does not depend on item names
public class TrackerData
{
    /// <summary>
    /// The CTX indices of the items that have been obtained.
    /// </summary>
    public HashSet<int> obtainedItems = new();

    /// <summary>
    /// A set which tracks the placements which have been previewed, by the Name property of the corresponding RandoLocation.
    /// </summary>
    public HashSet<string> previewedLocations = new();

    /// <summary>
    /// A dictionary which tracks the transitions that have been visited. Keys are sources and values are their targets.
    /// </summary>
    public Dictionary<string, string> visitedTransitions = new();

    /// <summary>
    /// A set which tracks the placements which have all items obtained, by the Name property of the corresponding RandoLocation.
    /// </summary>
    public HashSet<string> clearedLocations = new();

    /// <summary>
    /// A set which tracks the placements which are reachable in logic and have items remaining and have not been previewed, by the Name property of the corresponding RandoLocation.
    /// </summary>
    public HashSet<string> uncheckedReachableLocations = new();

    /// <summary>
    /// A set which tracks the transitions which are reachable in logic and have not been visited.
    /// </summary>
    public HashSet<string> uncheckedReachableTransitions = new();

    /// <summary>
    /// Should out of logic items and transitions be immediately added to current progression when acquired, or deferred until their locations are reachable?
    /// </summary>
    public bool AllowSequenceBreaks;

    /// <summary>
    /// The subset of obtainedItems that are currently out of logic, and were obtained by sequence breaking. Entries are removed as they become in logic.
    /// </summary>
    public HashSet<int> outOfLogicObtainedItems = new();

    /// <summary>
    /// The subset of visited transitions that are currently out of logic, and were visited by sequence breaking.
    /// </summary>
    public HashSet<string> outOfLogicVisitedTransitions = new();

    /// <summary>
    /// The ProgressionManager for the current state, with the information available to the player.
    /// </summary>
    [JsonIgnore] public ProgressionManager pm;

    [JsonIgnore] public LogicManager lm;
    [JsonIgnore] public APRandoContext ctx;
    private MainUpdater mu;

    public void Setup(APRandoContext context)
    {
        ctx = context;
        lm = ctx.LM;
        Reset();
    }

    public void Reset()
    {
        pm = new(lm, ctx);

        // note: location costs are ignored in the tracking, to prevent providing unintended information, by using p.location.logic rather than p.location
        // it is assumed that no information is divulged from the regular location logic and transition logic

        mu = pm.mu;
        mu.AddWaypoints(lm.Waypoints);
        mu.AddTransitions(lm.TransitionLookup.Values);

        mu.AddEntries(ctx.Vanilla.Select(v => new DelegateUpdateEntry(v.Location, pm =>
        {
            pm.Add(v.Item, v.Location);
            if (v.Location is ILocationWaypoint ilw)
            {
                pm.Add(ilw.GetReachableEffect());
            }
        })));

        if (ctx.itemPlacements != null)
        {
            mu.AddEntries(ctx.itemPlacements.Select((p, id) =>
                new DelegateUpdateEntry(p.Location.logic, OnCanGetLocation(id))));
        }

        if (ctx.transitionPlacements != null)
        {
            mu.AddEntries(ctx.transitionPlacements.Select((p, id) =>
                new DelegateUpdateEntry(p.Source, OnCanGetTransition(id))));
        }

        mu.StartUpdating(); // automatically handle tracking reachable unobtained locations/transitions and adding vanilla progression to pm
        pm.Set("ITEMRANDO", 1);

        SlotOptions options = Archipelago.HollowKnight.Archipelago.Instance.SlotOptions;
        if (options.AcidSkips)
            pm.Set("ACIDSKIPS", 1);
        if (options.ComplexSkips)
            pm.Set("COMPLEXSKIPS", 1);
        if (options.DamageBoosts)
            pm.Set("DAMAGEBOOSTS", 1);
        if (options.DangerousSkips)
            pm.Set("DANGEROUSSKIPS", 1);
        if (options.DarkRooms)
            pm.Set("DARKROOMS", 1);
        if (options.DifficultSkips)
            pm.Set("DIFFICULTSKIPS", 1);
        if (options.EnemyPogos)
            pm.Set("ENEMYPOGOS", 1);
        if (options.FireballSkips)
            pm.Set("FIREBALLSKIPS", 1);
        if (options.InfectionSkips)
            pm.Set("INFECTIONSKIPS", 1);
        if (options.ObscureSkips)
            pm.Set("OBSCURESKIPS", 1);
        if (options.PreciseMovement)
            pm.Set("PRECISEMOVEMENT", 1);
        if (options.ProficientCombat)
            pm.Set("PROFICIENTCOMBAT", 1);
        if (options.RandomizeElevatorPass)
            pm.Set("RANDOMELEVATORS", 1);
        if (options.RandomizeFocus)
            pm.Set("RANDOMFOCUS", 1);
        if (options.ShadeSkips)
            pm.Set("SHADESKIPS", 1);
        if (options.SpikeTunnels)
            pm.Set("SPIKETUNNELS", 1);
        if (options.BackgroundObjectPogos)
            pm.Set("BACKGROUNDPOGOS", 1);
        if (options.RemoveSpellUpgrades)
            pm.Set("CURSED", 1);
        if (options.RandomizeNail)
            pm.Set("RANDOMNAIL", 1);

        APMapMod.Instance.LogDebug("adding tutorial to tracker updates");
        TrackerUpdate.trackerUpdates.Add(() =>
        {
            APMapMod.Instance.LogDebug("executing tutorial adding update");
            pm.Add(lm.GetTransition("Tutorial_01[right1]"));
            APMapMod.Instance.LogDebug("finished executing tutorial adding update");
        });
        
    }

    private Action<ProgressionManager> OnCanGetLocation(int id)
    {
        return pm =>
        {
            (RandoItem item, RandoLocation location) = ctx.itemPlacements[id];
            if (location is ILocationWaypoint ilw)
            {
                pm.Add(ilw.GetReachableEffect());
            }

            if (outOfLogicObtainedItems.Remove(id))
            {
                pm.Add(item, location);
            }

            if (!clearedLocations.Contains(location.Name) && !previewedLocations.Contains(location.Name))
            {
                uncheckedReachableLocations.Add(location.Name);
            }
        };
    }

    private Action<ProgressionManager> OnCanGetTransition(int id)
    {
        return pm =>
        {
            (RandoTransition target, RandoTransition source) = ctx.transitionPlacements[id];

            if (!pm.Has(source.lt.term))
            {
                pm.Add(source.GetReachableEffect());
            }

            if (outOfLogicVisitedTransitions.Remove(source.Name))
            {
                pm.Add(target, source);
            }

            if (!visitedTransitions.ContainsKey(source.Name))
            {
                uncheckedReachableTransitions.Add(source.Name);
            }
        };
    }

    public void OnItemObtained(int id, string itemName, string placementName)
    {
        (RandoItem ri, RandoLocation rl) = ctx.itemPlacements[id];
        obtainedItems.Add(id);
        if (AllowSequenceBreaks || rl.logic.CanGet(pm))
        {
            pm.Add(ri, rl);
        }
        else
        {
            outOfLogicObtainedItems.Add(id);
        }
    }

    public void OnPlacementPreviewed(string placementName)
    {
        previewedLocations.Add(placementName);
        uncheckedReachableLocations.Remove(placementName);
    }

    public void OnPlacementCleared(string placementName)
    {
        clearedLocations.Add(placementName);
        previewedLocations.Remove(placementName);
        uncheckedReachableLocations.Remove(placementName);
    }

    public void OnTransitionVisited(string source, string target)
    {
        visitedTransitions[source] = target;
        uncheckedReachableTransitions.Remove(source);

        LogicTransition st = lm.GetTransition(source);
        if (AllowSequenceBreaks || st.CanGet(pm))
        {
            LogicTransition tt = lm.GetTransition(target);
            if (!pm.Has(st.term))
            {
                pm.Add(st.GetReachableEffect());
            }

            pm.Add(tt, st);
        }
        else
        {
            outOfLogicVisitedTransitions.Add(source);
        }
    }

    public bool HasVisited(string transition) => visitedTransitions.ContainsKey(transition);

    public class DelegateUpdateEntry : UpdateEntry
    {
        readonly Action<ProgressionManager> onAdd;
        readonly ILogicDef location;

        public DelegateUpdateEntry(ILogicDef location, Action<ProgressionManager> onAdd)
        {
            this.location = location;
            this.onAdd = onAdd;
        }

        public override bool CanGet(ProgressionManager pm)
        {
            return location.CanGet(pm);
        }

        public override IEnumerable<Term> GetTerms()
        {
            return location.GetTerms();
        }

        public override void OnAdd(ProgressionManager pm)
        {
            onAdd?.Invoke(pm);
        }
    }
}