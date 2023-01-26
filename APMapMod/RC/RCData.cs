using System.Collections.Generic;
using System.Linq;
using APMapMod.Data;
using APMapMod.RC.RandomizerData;
using ItemChanger;
using RandomizerCore.Logic;
using RoomDef = APMapMod.Data.RoomDef;
using StartDef = APMapMod.RC.RandomizerData.StartDef;

namespace APMapMod.RC;

public static class RCData
{
    // Transitions
    public static Dictionary<string, TransitionDef> transitions;
    
    // Rooms
    public static Dictionary<string, RoomDef> rooms;
    
    // Locations
    public static Dictionary<string, LocationDef> locations;
    
    // Starts
    public static Dictionary<string, StartDef> starts;
    
    private static readonly (LogicManagerBuilder.JsonType type, string fileName)[] Files = new[]
    {
        (LogicManagerBuilder.JsonType.Terms, "terms"),
        (LogicManagerBuilder.JsonType.Macros, "macros"),
        (LogicManagerBuilder.JsonType.Waypoints, "waypoints"),
        (LogicManagerBuilder.JsonType.Transitions, "transitions"),
        (LogicManagerBuilder.JsonType.Locations, "locations"),
        (LogicManagerBuilder.JsonType.Items, "items"),
    };
    
    #region Room Methods

    public static RoomDef? GetRoomDef(string name)
    {
        if (name is null)
        {
            return null;
        }
        if (!rooms.TryGetValue(name, out RoomDef def))
        {
            return null;
        }
        return def;
    }

    public static bool IsRoom(string str)
    {
        return str is not null && rooms.ContainsKey(str);
    }

    #endregion
    
    #region Location Methods

    public static LocationDef GetLocationDef(string name)
    {
        if (locations.TryGetValue(name, out var def)) return def;
        return null;
    }


    public static LocationDef[] GetLocationArray()
    {
        return locations.Values.ToArray();
    }

    public static bool IsLocation(string location)
    {
        return locations.ContainsKey(location);
    }

    #endregion
    
    #region Transition Methods
    public static TransitionDef GetTransitionDef(string name)
    {
        if (transitions.TryGetValue(name, out TransitionDef def)) return def;
        return null;
    }

    public static IEnumerable<string> GetMapAreaTransitionNames()
    {
        return transitions.Where(kvp => kvp.Value.IsMapAreaTransition).Select(kvp => kvp.Key);
    }

    public static IEnumerable<string> GetAreaTransitionNames()
    {
        return transitions.Where(kvp => kvp.Value.IsTitledAreaTransition).Select(kvp => kvp.Key);
    }

    public static IEnumerable<string> GetRoomTransitionNames()
    {
        return transitions.Keys;
    }

    public static bool IsMapAreaTransition(string str)
    {
        return transitions.TryGetValue(str, out TransitionDef def) && def.IsMapAreaTransition;
    }

    public static bool IsAreaTransition(string str)
    {
        return transitions.TryGetValue(str, out TransitionDef def) && def.IsTitledAreaTransition;
    }

    public static bool IsTransition(string str)
    {
        return transitions.ContainsKey(str);
    }

    public static bool IsTransitionWithEntry(string str)
    {
        return transitions.TryGetValue(str, out var def) && def.Sides != TransitionSides.OneWayOut;
    }

    public static bool IsExitOnlyTransition(string str)
    {
        return transitions.TryGetValue(str, out var def) && def.Sides == TransitionSides.OneWayOut;
    }

    public static bool IsEnterOnlyTransition(string str)
    {
        return transitions.TryGetValue(str, out var def) && def.Sides == TransitionSides.OneWayIn;
    }
    #endregion
    
    /// <summary>
    /// Creates a new LogicManager, for our use on the map.
    /// </summary>
    public static LogicManager GetNewLogicManager()
    {
        LogicManagerBuilder lmb = new()
        {
            VariableResolver = new RandoVariableResolver()
        };

        foreach ((LogicManagerBuilder.JsonType type, string fileName) in Files)
        {
            lmb.DeserializeJson(type, APMapMod.Instance.GetType().Assembly.GetManifestResourceStream($"APMapMod.Resources.Logic.{fileName}.json"));
        }
        
        transitions = JsonUtil.Deserialize<Dictionary<string, TransitionDef>>("APMapMod.Resources.Data.transitions.json");
        rooms = JsonUtil.Deserialize<Dictionary<string, RoomDef>>("APMapMod.Resources.Data.rooms.json");
        //starts = JsonUtil.Deserialize<Dictionary<string, StartDef>>("APMapMod.Resources.Data.starts.json");
        //locations = JsonUtil.Deserialize<Dictionary<string, LocationDef>>("RandomizerMod.Resources.Data.locations.json");

        return new LogicManager(lmb);
    }
}