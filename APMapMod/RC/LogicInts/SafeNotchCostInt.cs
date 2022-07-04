using System;
using System.Collections.Generic;
using System.Linq;
using RandomizerCore.Logic;

namespace APMapMod.RC.LogicInts;

/// <summary>
/// LogicInt which returns 1 less than the number of notches to equip the charm with or without overcharming.
/// </summary>
public class SafeNotchCostInt : LogicInt
{
    // the ids should correspond to the 1-40 charm nums (i.e. 1-indexed)
    public readonly int[] charmIDs;
    
    public SafeNotchCostInt(params int[] charmIDs)
    {
        this.charmIDs = charmIDs;
        Array.Sort(charmIDs);
        Name = $"$SafeNotchCost[{string.Join(",", charmIDs)}]";
    }
    
    public override string Name { get; }

    public override int GetValue(object sender, ProgressionManager pm)
    {

        List<int> notchCosts = new();
        for (int i = 0; i < NotchCostInt.vanillaCosts.Length; i++)
        {
            notchCosts.Add(PlayerData.instance.GetInt($"charmCost_{i}"));
        }
        if (notchCosts.Count >= charmIDs[charmIDs.Length - 1])
        {
            return charmIDs.Sum(i => notchCosts[i - 1]) - 1;
        }
        else
        {
            return charmIDs.Sum(NotchCostInt.GetVanillaCost) - 1;
        }
    }

    public override IEnumerable<Term> GetTerms() => Enumerable.Empty<Term>();
}