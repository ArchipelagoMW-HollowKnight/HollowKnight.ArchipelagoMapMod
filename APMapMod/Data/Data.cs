using System.Collections.Generic;

namespace APMapMod.Data;

public struct RoomDef
{
    public string SceneName;
    public string MapArea;
    public string TitledArea;
}

public static class Data
{
    // Trimmed down file from Homothetyhk's rando 4 mod https://github.com/homothetyhk/RandomizerMod/blob/master/RandomizerMod/RandomizerData/Data.cs
    
    // Rooms
    private static Dictionary<string, RoomDef> _rooms;
    
    #region Room Methods

    public static RoomDef GetRoomDef(string name)
    {
        if (name is null)
        {
            return new RoomDef();
        }
        if (!_rooms.TryGetValue(name, out RoomDef def))
        {
            return new RoomDef();
        }
        return def;
    }

    public static bool IsRoom(string str)
    {
        return str is not null && _rooms.ContainsKey(str);
    }

    #endregion
    
    public static void Load()
    {
        _rooms = JsonUtil.Deserialize<Dictionary<string, RoomDef>>("APMapMod.Resources.Data.rooms.json");
    }

}