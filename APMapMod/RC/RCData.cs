using System.Collections.Generic;
using System.Linq;
using APMapMod.Data;
using APMapMod.RC.RandomizerData;
using RandomizerCore.Logic;
using RoomDef = APMapMod.Data.RoomDef;

namespace APMapMod.RC;

public class RCData
{
    // Transitions
    public static Dictionary<string, TransitionDef> transitions;
    
    // Rooms
    public static Dictionary<string, RoomDef> rooms;
    
    // Locations
    public static Dictionary<string, LocationDef> locations;
    
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
        //locations = JsonUtil.Deserialize<Dictionary<string, LocationDef>>("RandomizerMod.Resources.Data.locations.json");

        return new LogicManager(lmb);
    }
}