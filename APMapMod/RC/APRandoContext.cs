using System.Collections.Generic;
using RandomizerCore;

namespace APMapMod.RC;

public class APRandoContext : RandoContext
{
    public APRandoContext() : base(RCData.GetNewLogicManager())
    {
        notchCosts = new List<int>();
        for (int i = 0; i < vanillaCosts.Length; i++)
        {
            notchCosts.Add(PlayerData.instance.GetInt($"charmCost_{i}"));
        }
        
    }

    public List<GeneralizedPlacement> Vanilla;
    public List<ItemPlacement> itemPlacements;
    public List<TransitionPlacement> transitionPlacements;
    public List<int> notchCosts;
    public static int[] vanillaCosts = new int[]
    {
        1,
        1,
        1,
        2,
        2,
        2,
        3,
        2,
        3,
        1,
        3,
        1,
        3,
        1,
        2,
        2,
        1,
        2,
        3,
        2,
        4,
        2,
        2,
        2,
        3,
        1,
        4,
        2,
        4,
        1,
        2,
        3,
        2,
        4,
        3,
        5,
        1,
        3,
        2,
        2
    };

    
    public override IEnumerable<GeneralizedPlacement> EnumerateExistingPlacements()
    {
        //APMapMod.Instance.LogDebug("Asking for GeneralizedPlacements");
        if (Vanilla != null) foreach (GeneralizedPlacement p in Vanilla) yield return p;
        if (itemPlacements != null) foreach (ItemPlacement p in itemPlacements) yield return p;
        if (transitionPlacements != null) foreach (TransitionPlacement p in transitionPlacements) yield return p;
    }
}