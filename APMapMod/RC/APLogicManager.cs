using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using ItemChanger.Locations;
using RandomizerCore;

namespace APMapMod.RC;

public static class APLogicManager
{

    public static void SetupLogic()
    {
        APMapMod.LS.Context ??= new APRandoContext();
        APMapMod.LS.tracker ??= new TrackerData {AllowSequenceBreaks = true};
        APMapMod.LS.trackerWithoutSequenceBreaks ??= new TrackerData {AllowSequenceBreaks = false};
        if (APMapMod.LS.Context.Vanilla == null)
        {
            APMapMod.LS.Context.Vanilla ??= new List<GeneralizedPlacement>();
            foreach (var transition in RCData.transitions)
            {
                if (transition.Value.VanillaTarget == null) continue;
                //APMapMod.Instance.LogDebug($"creating transition {transition.Key} and linking it to {transition.Value.VanillaTarget}");
                var item = APMapMod.LS.Context.LM.TransitionLookup[transition.Key];
                var location = APMapMod.LS.Context.LM.GetTransition(transition.Value.VanillaTarget);
                APMapMod.LS.Context.Vanilla.Add(new GeneralizedPlacement(item, location));
            }
        }

        if (APMapMod.LS.Context.itemPlacements == null)
        {
            var allItems = Finder.ItemNames.ToList();
            APMapMod.LS.Context.itemPlacements ??= new List<ItemPlacement>();
            foreach (var placement in ItemChanger.Internal.Ref.Settings.Placements)
            {
                foreach (var abstractItem in placement.Value.Items)
                {
                    allItems.Remove(abstractItem.name);
                    //APMapMod.Instance.LogDebug($"Creating Item Placement {placement.Key} ({abstractItem.name})");
                    RandoItem item = new()
                    {
                        item = Finder.GetItem(abstractItem.name) != null ? APMapMod.LS.Context.LM.GetItem(abstractItem.name) : null
                    };
                    RandoLocation location = new()
                    {
                        logic = APMapMod.LS.Context.LM.GetLogicDef(placement.Key)
                    };

                    APMapMod.LS.Context.itemPlacements.Add(new ItemPlacement(item, location));
                }
            }
            //APMapMod.Instance.LogDebug($"Local Items sent, {allItems.Count} remain adding to start region.");
            foreach (var allItem in allItems)
            {
                RandoItem item = new()
                {
                    item = APMapMod.LS.Context.LM.GetItem(allItem)
                };
                RandoLocation location = new()
                {
                    logic = APMapMod.LS.Context.LM.GetLogicDef("Start")
                };

                APMapMod.LS.Context.itemPlacements.Add(new ItemPlacement(item, location));
            }
        }
        
        APMapMod.LS.tracker?.Setup(APMapMod.LS.Context);
        APMapMod.LS.trackerWithoutSequenceBreaks?.Setup(APMapMod.LS.Context);
        
        ItemChangerMod.Modules.Add<TrackerUpdate>();
    }
}