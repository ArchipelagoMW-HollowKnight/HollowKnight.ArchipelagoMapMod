using Archipelago.HollowKnight.IC;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using ItemChanger;
using ItemChanger.Internal;

namespace APMapMod.Trackers;

public class HintTracker
{
    private static ArchipelagoSession session;
    
    public static void Hook()
    {
        session = APMapMod.Instance.session;
        session.Socket.PacketReceived += OnHint;
    }

    private static void OnHint(ArchipelagoPacketBase packet)
    {
        // ignore all non hint json packets
        if (packet is not HintPrintJsonPacket)
            return;
        
        var hint =  (HintPrintJsonPacket)packet;
        APMapMod.Instance.LogDebug($"hint packet found. item: {hint.Item.Item} location: {hint.Item.Location} player: {hint.Item.Player}");

        // ignore items in other players worlds.
        if (hint.Item.Player != session.ConnectionInfo.Slot)
            return;

        var apLocation = session.Locations.GetLocationNameFromId(hint.Item.Location);
        var location = apLocation;
        var item = 0;
        
        
        if (apLocation.StartsWith(LocationNames.Leg_Eater)) location = LocationNames.Leg_Eater;
        else if (apLocation.StartsWith(LocationNames.Seer)) location = LocationNames.Seer;
        else if (apLocation.StartsWith(LocationNames.Iselda)) location = LocationNames.Iselda;
        else if (apLocation.StartsWith(LocationNames.Grubfather)) location = LocationNames.Grubfather;
        else if (apLocation.StartsWith(LocationNames.Sly_Key)) location = LocationNames.Sly_Key;
        else if (apLocation.StartsWith(LocationNames.Sly)) location = LocationNames.Sly;
        else if (apLocation.StartsWith(LocationNames.Salubra)) location = LocationNames.Salubra;
        else if (apLocation.StartsWith(LocationNames.Egg_Shop)) location = LocationNames.Egg_Shop;

        if (!apLocation.Equals(location))
        {
            var split = apLocation.Split('_');
            item = int.Parse(split[split.Length-1]) - 1;
        }
        
        var placement = Ref.Settings.Placements[location];
        if (placement == null) return;
        
        placement.Items[item].GetTag<ArchipelagoItemTag>().Hinted = true;
        
        if (placement.Items.Count > 1) return;
        
        placement.GetTag<ArchipelagoPlacementTag>().Hinted = true;
        placement.AddVisitFlag(VisitState.Previewed);
    }
}