using System.Collections.Generic;
using System.Linq;
using RandomizerCore.Logic;

namespace APMapMod.RC.LogicInts;

public class StartLocationDelta : LogicInt
{
    public StartLocationDelta(string location)
    {
        Location = location;
        Name = $"$StartLocation[{location}]";
    }

    public override string Name { get; }
    public string Location { get; }

    public override IEnumerable<Term> GetTerms()
    {
        return Enumerable.Empty<Term>();
    }

    public override int GetValue(object sender, ProgressionManager pm)
    {
        return Location == "Tutorial_01" ? 1 : 0;
    }
}