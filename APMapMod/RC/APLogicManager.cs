using System.Collections.Generic;
using System.Linq;
using System.Threading;
using APMapMod.UI;
using Archipelago.HollowKnight.IC;
using ItemChanger;
using ItemChanger.Internal;
using RandomizerCore;

namespace APMapMod.RC;

public static class APLogicManager
{

    public static void SetupLogic()
    {
        APMapMod.LS.Context ??= new APRandoContext();
        APMapMod.LS.tracker ??= new TrackerData {AllowSequenceBreaks = true};
        APMapMod.LS.trackerWithoutSequenceBreaks ??= new TrackerData {AllowSequenceBreaks = false};

        APMapMod.LS.Context.Vanilla ??= new List<GeneralizedPlacement>();
        foreach (var transition in RCData.transitions)
        {
            if (transition.Value.VanillaTarget == null) continue;
#if DEBUG
            APMapMod.Instance.LogDebug($"creating transition {transition.Key} and linking it to {transition.Value.VanillaTarget}");
#endif
            var item = APMapMod.LS.Context.LM.TransitionLookup[transition.Key];
            var location = APMapMod.LS.Context.LM.GetTransition(transition.Value.VanillaTarget);
            APMapMod.LS.Context.Vanilla.Add(new GeneralizedPlacement(item, location));
        }
        
    
        // we need to keep track of all items in the game so we can make sure they are all in our context
        // we remove items form here as we add them from known local placements. and then add all remaining
        // items to the "start" later
        var externalItems = Finder.ItemNames.ToList();
        APMapMod.LS.Context.itemPlacements ??= new List<ItemPlacement>();
        var sortedPlacements = Ref.Settings.Placements.ToList().OrderBy(p => p.Value.GetTag<APMapPlacementTag>()?.id);
        var index = 0;
        foreach (var entry in sortedPlacements)
        {
            
            //no placement tag found this is our first run lets save the index so future runs can sort by it
            if (!entry.Value.HasTag<APMapPlacementTag>())
                entry.Value.AddTag<APMapPlacementTag>().id = index++;

            foreach (var abstractItem in entry.Value.Items)
            {
                RandoItem item = new();
                var id = APMapMod.LS.Context.itemPlacements.Count;
                var tag = abstractItem.GetOrAddTag<APMapItemTag>();
                tag.id = id;

                //check if this is an AP item.
                if (abstractItem.GetTag(out ArchipelagoItemTag aptag))
                {
                    // only add an actual logical item if its for us.
                    if (aptag.Player == APMapMod.Instance.session.ConnectionInfo.Slot)
                    {
                        item.item = APMapMod.LS.Context.LM.GetItem(abstractItem.name);
                        // item is for us we can remove it from the external list
                        externalItems.Remove(abstractItem.name);
                    }
                }

                RandoLocation location = new()
                {
                    logic = APMapMod.LS.Context.LM.GetLogicDef(entry.Key)
                };
                APMapMod.Instance.LogDebug($"Creating Item Placement [{id}] [{aptag?.Player}] {item.item?.Name} at {entry.Key}");
                APMapMod.LS.Context.itemPlacements.Add(new ItemPlacement(item, location));
            }
        }

        APMapMod.Instance.LogDebug($"Local Items set, {externalItems.Count} remain adding to start region.");
        foreach (var externalItem in externalItems)
        {
            // there are some oddball items like the charm repair and Iselda's map pins that are not in here
            // so just skip those.
            if (! APMapMod.LS.Context.LM.ItemLookup.ContainsKey(externalItem)) continue;
            RandoItem item = new()
            {
                item = APMapMod.LS.Context.LM.GetItem(externalItem)
            };
            RandoLocation location = new()
            {
                logic = APMapMod.LS.Context.LM.GetLogicDef("Start")
            };

            APMapMod.LS.Context.itemPlacements.Add(new ItemPlacement(item, location));
        }

        new Thread(() =>
        {
            APMapMod.LS.trackerWithoutSequenceBreaks?.Setup(APMapMod.LS.Context);
            APMapMod.LS.tracker?.Setup(APMapMod.LS.Context);
            HintDisplay.SortHints();
        }).Start();

            
        ItemChangerMod.Modules.Remove(typeof(TrackerUpdate));
        ItemChangerMod.Modules.Add<TrackerUpdate>();
        
    }
}