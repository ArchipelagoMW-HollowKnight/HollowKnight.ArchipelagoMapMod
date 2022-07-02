using System;
using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using ItemChanger.Modules;
using RandomizerCore.Logic;

namespace APMapMod.RC.LogicInts;

/// <summary>
/// LogicInt which returns 1 less than the number of notches to equip the charm with or without overcharming.
/// </summary>
public class NotchCostInt : LogicInt
{
    // the ids should correspond to the 1-40 charm nums (i.e. 1-indexed)
    public readonly int[] charmIDs;
    
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

    public NotchCostInt(params int[] charmIDs)
    {
        this.charmIDs = charmIDs;
        Array.Sort(charmIDs);
        Name = $"$NotchCost[{string.Join(",", charmIDs)}]";
    }
    
    public static int GetVanillaCost(int id) => vanillaCosts[id - 1];

    public override string Name { get; }

    public override int GetValue(object sender, ProgressionManager pm)
    {

        List<int> notchCosts = new();
        for (int i = 0; i < vanillaCosts.Length; i++)
        {
            notchCosts.Add(PlayerData.instance.GetInt($"charmCost_{i}"));
        }
        
        if (notchCosts.Count >= charmIDs[charmIDs.Length - 1])
        {
            return charmIDs.Sum(i => notchCosts[i - 1]) - charmIDs.Max(i => notchCosts[i - 1]);
        }
        else
        {
            return charmIDs.Sum(GetVanillaCost) - charmIDs.Max(GetVanillaCost);
        }
    }

    public override IEnumerable<Term> GetTerms() => Enumerable.Empty<Term>();
}