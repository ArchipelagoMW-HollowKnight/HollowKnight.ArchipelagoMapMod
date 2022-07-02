using System.Collections.Generic;
using RandomizerCore;

namespace APMapMod.RC;

public class APRandoContext : RandoContext
{
    public APRandoContext() : base(RCData.GetNewLogicManager())
    {}

    public List<GeneralizedPlacement> Vanilla;
    public List<ItemPlacement> itemPlacements;
    public List<TransitionPlacement> transitionPlacements;
    public List<int> notchCosts;
    
    public override IEnumerable<GeneralizedPlacement> EnumerateExistingPlacements()
    {
        //APMapMod.Instance.LogDebug("Asking for GeneralizedPlacements");
        if (Vanilla != null) foreach (GeneralizedPlacement p in Vanilla) yield return p;
        if (itemPlacements != null) foreach (ItemPlacement p in itemPlacements) yield return p;
        if (transitionPlacements != null) foreach (TransitionPlacement p in transitionPlacements) yield return p;
    }
}