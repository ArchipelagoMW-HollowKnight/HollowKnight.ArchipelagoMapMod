using System.Collections.Generic;
using System.Linq;
using Archipelago.HollowKnight;
using Archipelago.HollowKnight.IC;
using GlobalEnums;
using ItemChanger;
using ItemChanger.Internal;
using ItemChanger.Placements;
using ItemChanger.Tags;

namespace APMapMod.Data
{
    public static class MainData
    {
        private static Dictionary<string, PinDef> allPins;
        private static Dictionary<string, PinDef> allPinsAM;
        private static List<string> sortedGroups;
        private static HashSet<string> minimalMapRooms;
        private static Dictionary<string, MapRoomDef> nonMappedRooms;
        private static Dictionary<string, MapRoomDef> pinScenes;
        private static readonly Dictionary<string, PinDef> usedPins = new();
        private static Dictionary<string, string> logicLookup = new();

        public static List<string> usedPoolGroups = new();

        public static PinDef[] GetUsedPinArray()
        {
            return usedPins.Values.OrderBy(p => p.offsetX).ThenBy(p => p.offsetY).ToArray();
        }

        public static PinDef GetUsedPinDef(string locationName)
        {
            if (usedPins.TryGetValue(locationName, out PinDef pinDef))
            {
                return pinDef;
            }

            return default;
        }

        public static bool IsMinimalMapRoom(string scene)
        {
            return minimalMapRooms.Contains(scene);
        }

        public static bool IsNonMappedScene(string scene)
        {
            return nonMappedRooms.ContainsKey(scene);
        }

        public static IEnumerable<string> GetNonMappedScenes()
        {
            if (Dependencies.HasAdditionalMaps())
            {
                return nonMappedRooms.Keys.Where(s => nonMappedRooms[s].includeWithAdditionalMaps);
            }

            return nonMappedRooms.Keys;
        }

        public static MapRoomDef GetNonMappedRoomDef(string scene)
        {
            if (nonMappedRooms.TryGetValue(scene, out MapRoomDef mrd))
            {
                return mrd;
            }

            return default;
        }

        public static MapZone GetFixedMapZone()
        {
            if (nonMappedRooms.TryGetValue(Utils.CurrentScene(), out MapRoomDef mrd))
            {
                return mrd.mapZone;
            }

            // This seems to be the one mapped room that doesn't have its MapZone set
            if (Utils.CurrentScene() == "Ruins_Elevator")
            {
                return MapZone.CITY;
            }

            return default;
        }

        public static bool IsInLogicLookup(string locationName)
        {
            return logicLookup.ContainsKey(locationName);
        }

        public static string GetRawLogic(string locationName)
        {
            if (logicLookup.TryGetValue(locationName, out string logic))
            {
                return logic;
            }

            return default;
        }

        public static bool IsPersistent(this AbstractItem item)
        {
            return item.HasTag<PersistentItemTag>();
        }

        public static bool CanPreviewItem(this AbstractPlacement placement)
        {
            return !placement.HasTag<DisableItemPreviewTag>();
        }

        public static string[] GetPreviewText(string abstractPlacementName)
        {
            if (!Ref.Settings.Placements.TryGetValue(abstractPlacementName, out AbstractPlacement placement)) return default;

            if (placement.GetTag(out MultiPreviewRecordTag multiTag))
            {
                return multiTag.previewTexts;
            }

            if (placement.GetTag(out PreviewRecordTag tag))
            {
                return new[] { tag.previewText };
            }
            
            var costText = "";
            if (placement is ISingleCostPlacement costPlacement)
            {
                if(costPlacement.Cost != null)
                    costText += costPlacement.Cost.GetCostText();
            }
            else
            {
                return placement.Items.Select(item =>
                {
                    if (item.GetTag(out CostTag costTag))
                    {
                        costText = costTag.Cost.GetCostText();
                    }
                    return $"{item.GetPreviewName()}{(costText.Length > 0 ? $" - {costText}" : "")}";
                }).ToArray();
            }

            return placement.Items.Select(item => $"{item.GetPreviewName()}{(costText.Length > 0 ? $" - {costText}" : "")}").ToArray();
        }
        
        public static void SetUsedPinDefs()
        {
            usedPins.Clear();
            usedPoolGroups.Clear();
            HashSet<string> unsortedGroups = new();

            // Randomized placements
            foreach (AbstractPlacement placement in Ref.Settings.Placements.Values)
            {
                if (placement.Items.Any(i => !i.HasTag<ArchipelagoItemTag>())) continue;
                IEnumerable<ItemDef> items = placement.Items
                    .Where(x => !x.IsObtained() || x.IsPersistent())
                    .Select(x => new ItemDef(x));

                if (!items.Any()) continue;

                if (!allPins.TryGetValue(placement.Name, out PinDef pd))
                {
                    pd = new();

                    APMapMod.Instance.LogWarn($"Unknown placement {placement.Name}. Making a 'best guess' for the placement");
                }

                pd.name = placement.Name;
                pd.sceneName = Finder.GetLocation(placement.Name).sceneName;
                
                if (pd.sceneName == "Room_Colosseum_Bronze" || pd.sceneName == "Room_Colosseum_Silver")
                {
                    pd.sceneName = "Room_Colosseum_01";
                }

                if (nonMappedRooms.ContainsKey(pd.sceneName))
                {
                    pd.pinScene = nonMappedRooms[pd.sceneName].mappedScene;
                    pd.mapZone = nonMappedRooms[pd.sceneName].mapZone;
                }
                if (pinScenes.ContainsKey(pd.sceneName))
                {
                    pd.pinScene = pinScenes[pd.sceneName].mappedScene;
                    pd.mapZone = pinScenes[pd.sceneName].mapZone;
                }

                if (pd.pinScene == null)
                {
                    pd.mapZone = Utils.ToMapZone(Data.GetRoomDef(pd.sceneName).MapArea);
                }
                pd.randomized = placement.IsRandomized();
                pd.randoItems = items;
                pd.canPreviewItem = placement.CanPreviewItem();
                pd.placement = placement;

                // UpdatePins will set it to the correct state
                pd.pinLocationState = pd.randomized
                    ? PinLocationState.UncheckedUnreachable
                    : PinLocationState.NonRandomizedUnchecked;
                pd.locationPoolGroup = placement.GetPlacementGroup();

                usedPins.Add(placement.Name, pd);

                unsortedGroups.Add(pd.locationPoolGroup);

                foreach(ItemDef i in pd.randoItems)
                {
                    unsortedGroups.Add(i.poolGroup);
                }

                //APMapMod.Instance.Log(locationName);
                //APMapMod.Instance.Log(pinDef.locationPoolGroup);
            }
            
            // // Vanilla placements
            // foreach (GeneralizedPlacement placement in RM.RS.Context.Vanilla)
            // {
            //     if (RD.IsLocation(placement.Location.Name)
            //         && !RM.RS.TrackerData.clearedLocations.Contains(placement.Location.Name)
            //         && placement.Location.Name != "Start"
            //         && placement.Location.Name != "Iselda"
            //         && allPins.ContainsKey(placement.Location.Name)
            //         && !usedPins.ContainsKey(placement.Location.Name))
            //     {
            //         PinDef pd = allPins[placement.Location.Name];
            //
            //         pd.name = placement.Location.Name;
            //         pd.sceneName = RD.GetLocationDef(placement.Location.Name).SceneName;
            //
            //         if (pd.sceneName == "Room_Colosseum_Bronze" || pd.sceneName == "Room_Colosseum_Silver")
            //         {
            //             pd.sceneName = "Room_Colosseum_01";
            //         }
            //
            //         if (nonMappedRooms.ContainsKey(pd.sceneName))
            //         {
            //             pd.pinScene = nonMappedRooms[pd.sceneName].mappedScene;
            //             pd.mapZone = nonMappedRooms[pd.sceneName].mapZone;
            //         }
            //
            //         if (pd.pinScene == null)
            //         {
            //             pd.mapZone = Utils.ToMapZone(RD.GetRoomDef(pd.sceneName).MapArea);
            //         }
            //
            //         if (!HasObtainedVanillaItem(pd))
            //         {
            //             pd.randomized = false;
            //
            //             pd.pinLocationState = PinLocationState.NonRandomizedUnchecked;
            //             pd.locationPoolGroup = SubcategoryFinder.GetLocationPoolGroup(placement.Location.Name).FriendlyName();
            //
            //             usedPins.Add(placement.Location.Name, pd);
            //
            //             unsortedGroups.Add(pd.locationPoolGroup);
            //
            //             //APMapMod.Instance.Log(placement.Location.Name);
            //         }
            //     }
            // }

            // Sort all the PoolGroups that have been used
            foreach (string poolGroup in sortedGroups)
            {
                if (unsortedGroups.Contains(poolGroup))
                {
                    usedPoolGroups.Add(poolGroup);
                    unsortedGroups.Remove(poolGroup);
                }
            }

            usedPoolGroups.AddRange(unsortedGroups);

            if (Dependencies.HasAdditionalMaps())
            {
                ApplyAdditionalMapsChanges();
            }
        }

        public static void ApplyAdditionalMapsChanges()
        {
            foreach (KeyValuePair<string, PinDef> kvp in allPinsAM)
            {
                if (usedPins.TryGetValue(kvp.Key, out PinDef pinDef))
                {
                    pinDef.pinScene = kvp.Value.pinScene;
                    pinDef.mapZone = kvp.Value.mapZone;
                    pinDef.offsetX = kvp.Value.offsetX;
                    pinDef.offsetY = kvp.Value.offsetY;
                }
            }
        }

        public static void SetLogicLookup()
        {
            logicLookup = APMapMod.LS.Context.LM.LogicLookup.Values.ToDictionary(l => l.Name, l => l.ToInfix());
        }

        public static void Load()
        {
            allPins = JsonUtil.Deserialize<Dictionary<string, PinDef>>("APMapMod.Resources.pins.json");
            allPinsAM = JsonUtil.Deserialize<Dictionary<string, PinDef>>("APMapMod.Resources.pinsAM.json");
            sortedGroups = JsonUtil.Deserialize<List<string>>("APMapMod.Resources.sortedGroups.json");
            minimalMapRooms = JsonUtil.Deserialize<HashSet<string>>("APMapMod.Resources.minimalMapRooms.json");
            nonMappedRooms = JsonUtil.Deserialize<Dictionary<string, MapRoomDef>>("APMapMod.Resources.nonMappedRooms.json");
            pinScenes = JsonUtil.Deserialize<Dictionary<string, MapRoomDef>>("APMapMod.Resources.pinScenes.json");
        }
        
        public static bool IsRandomized(this AbstractPlacement placement)
        {

            var slotOptions = Archipelago.HollowKnight.Archipelago.Instance.SlotOptions;

            switch (placement.Name)
            {
                case ItemNames.Elevator_Pass:
                    return slotOptions.RandomizeElevatorPass;
                case ItemNames.Right_Mothwing_Cloak:
                case ItemNames.Left_Mothwing_Cloak:
                    return slotOptions.SplitMothwingCloak;
                case ItemNames.Right_Crystal_Heart:
                case ItemNames.Left_Crystal_Heart:
                    return slotOptions.SplitCrystalHeart;
                case ItemNames.Right_Mantis_Claw:
                case ItemNames.Left_Mantis_Claw:
                    return slotOptions.SplitMantisClaw;
            }

            switch (placement.GetPlacementGroup())
            {
                case "Dreamers":
                    return slotOptions.RandomizeDreamers;
                case "Skills":
                    return slotOptions.RandomizeSkills;
                case "Charms":
                    if (placement.Name == LocationNames.King_Fragment)
                        return slotOptions.WhitePalace is not WhitePalaceOption.Exclude;
                    return slotOptions.RandomizeCharms;
                case "Keys":
                    return slotOptions.RandomizeKeys;
                case "Mask Shards":
                    return slotOptions.RandomizeMaskShards;
                case "Vessel Fragments":
                    return slotOptions.RandomizeVesselFragments; 
                case "Charm Notches":
                    return slotOptions.RandomizeCharmNotches;
                case "Pale Ore":
                    return slotOptions.RandomizePaleOre;
                case "Geo Chests":
                    return placement.Name.Contains("Junk_Pit") ? slotOptions.RandomizeJunkPitChests : slotOptions.RandomizeGeoChests; 
                case "Rancid Eggs":
                    return slotOptions.RandomizeRancidEggs;
                case "Relics":
                    return slotOptions.RandomizeRelics;
                case "Whispering Roots":
                    return slotOptions.RandomizeWhisperingRoots;
                case "Boss Essence":
                    return slotOptions.RandomizeBossEssence; 
                case "Grubs":
                    return slotOptions.RandomizeGrubs;
                case "Mimics":
                    return slotOptions.RandomizeMimics; 
                case "Maps":
                    return slotOptions.RandomizeMaps;
                case "Stags":
                    return slotOptions.RandomizeStags;
                case "Lifeblood Cocoons":
                    return slotOptions.RandomizeLifebloodCocoons;
                case "Grimmkin Flames":
                    return slotOptions.RandomizeGrimmkinFlames;
                case "Journal Entries":
                    return slotOptions.RandomizeJournalEntries;
                case "Geo Rocks":
                    return slotOptions.RandomizeGeoRocks;
                case "Boss Geo":
                    return slotOptions.RandomizeBossGeo;
                case "Soul Totems":
                    if (placement.Name.Contains("White_Palace"))
                    {
                        return slotOptions.WhitePalace is WhitePalaceOption.NoPathOfPain or WhitePalaceOption.Include;
                    }
                    if (placement.Name.Contains("Path_Of_Pain"))
                    {
                        return slotOptions.WhitePalace == WhitePalaceOption.Include;
                    }
                    return slotOptions.RandomizeSoulTotems;
                case "Lore Tablets":
                    if (placement.Name.Contains("Palace"))
                    {
                        return slotOptions.WhitePalace == WhitePalaceOption.NoPathOfPain;
                    }
                    if (placement.Name.Contains("Path_Of_Pain"))
                    {
                        return slotOptions.WhitePalace == WhitePalaceOption.Include;
                    }
                    return slotOptions.RandomizeLoreTablets;
                case "Shops":
                    return true;
                default:
                    return false;
            }
        }
        
         public static string GetPlacementGroup(this AbstractPlacement placement)
        {
            return GetPlacementGroup(placement.Name);
        }   
         
         public static string GetPlacementGroup(this ItemDef item)
        {
            return GetPlacementGroup(item.itemName);
        }

        public static string GetPlacementGroup(string name)
        {
            switch (name.Split('-')[0])
            {
                case ItemNames.Lurien:
                case ItemNames.Monomon:
                case ItemNames.Herrah:
                case ItemNames.World_Sense:
                    return "Dreamers";
                    
                case ItemNames.Mothwing_Cloak:
                case ItemNames.Left_Mothwing_Cloak:
                case ItemNames.Right_Mothwing_Cloak:
                case LocationNames.Split_Mothwing_Cloak:
                case ItemNames.Mantis_Claw:
                case ItemNames.Left_Mantis_Claw:
                case ItemNames.Right_Mantis_Claw:
                case ItemNames.Crystal_Heart:
                case ItemNames.Left_Crystal_Heart:
                case ItemNames.Right_Crystal_Heart:
                case LocationNames.Split_Crystal_Heart:
                case ItemNames.Monarch_Wings:
                case ItemNames.Shade_Cloak:
                case ItemNames.Ismas_Tear:
                case ItemNames.Swim:
                case ItemNames.Focus:
                case ItemNames.Dream_Nail:
                case ItemNames.Awoken_Dream_Nail:
                case ItemNames.Dream_Gate:
                case ItemNames.Vengeful_Spirit:
                case ItemNames.Shade_Soul:
                case ItemNames.Desolate_Dive:
                case ItemNames.Descending_Dark:
                case ItemNames.Howling_Wraiths:
                case ItemNames.Abyss_Shriek:
                case ItemNames.Cyclone_Slash:
                case ItemNames.Dash_Slash:
                case ItemNames.Great_Slash:
                case ItemNames.Leftslash:
                case ItemNames.Upslash:
                case ItemNames.Rightslash:
                    return "Skills";
                    
                case "Baldur_Shell":
                case "Fury_of_the_Fallen":
                case "Lifeblood_Core":
                case "Defender's_Crest":
                case "Flukenest":
                case "Thorns_of_Agony":
                case "Mark_of_Pride":
                case "Sharp_Shadow":
                case "Spore_Shroom":
                case "Soul_Catcher":
                case "Soul_Eater":
                case "Glowing_Womb":
                case "Nailmaster's_Glory":
                case "Joni's_Blessing":
                case "Shape_of_Unn":
                case "Hiveblood":
                case "Dashmaster":
                case "Quick_Slash":
                case "Spell_Twister":
                case "Deep_Focus":
                case "Queen_Fragment":
                case "King_Fragment":
                case "Void_Heart":
                case "Dreamshield":
                case "Weaversong":
                case "Grimmchild":
                case "Carefree_Melody":
                case "Longnail":
                case "Gathering_Swarm":
                case "Steady_Body":
                case "Shaman_Stone":
                case "Quick_Focus":
                case "Lifeblood_Heart":
                case "Stalwart_Shell":
                case "Heavy_Blow":
                case "Sprintmaster":
                case "Grubsong":
                case "Grubberfly's_Elegy":
                case "Dream_Wielder":
                case "Wayward_Compass":
                case "Unbreakable_Heart":
                case "Unbreakable_Greed":
                case "Unbreakable_Strength":
                case "Fragile_Heart":
                case "Fragile_Greed":
                case "Fragile_Strength":
                    return "Charms";
                        
                case ItemNames.Simple_Key:
                case ItemNames.Shopkeepers_Key:
                case ItemNames.Love_Key:
                case ItemNames.Kings_Brand:
                case ItemNames.Godtuner:
                case ItemNames.Collectors_Map:
                case ItemNames.City_Crest:
                case ItemNames.Tram_Pass:
                case ItemNames.Elevator_Pass:
                    return "Keys";
                    
                case ItemNames.Mask_Shard:
                case ItemNames.Full_Mask:
                case ItemNames.Double_Mask_Shard:
                    return "Mask Shards";

                case ItemNames.Vessel_Fragment:
                case ItemNames.Double_Vessel_Fragment:
                case ItemNames.Full_Soul_Vessel:
                    return "Vessel Fragments";
                
                case ItemNames.Charm_Notch:
                    return "Charm Notches";
                    
                case ItemNames.Pale_Ore:
                    return "Pale Ore";
                    
                case "Geo_Chest":
                case ItemNames.Lumafly_Escape:
                    return "Geo Chests";
                    
                case "Rancid_Egg":
                    return "Rancid Eggs";
                    
                case "Wanderer's_Journal": 
                case "Hallownest_Seal":
                case "King's_Idol": 
                case "Arcane_Egg":
                    return "Relics";
                    
                case "Whispering_Root":
                    return "Whispering Roots";
                    
                case "Boss_Essence":
                    return "Boss Essence";
                    
                case "Grub":
                    return "Grubs";
                    
                case "Mimic_Grub":
                    return "Mimics";
                    
                case "Lifeblood_Cocoon":
                case ItemNames.Lifeblood_Cocoon_Small:
                case ItemNames.Lifeblood_Cocoon_Large:
                    return "Lifeblood Cocoons";
                
                case "Grimmkin_Flame":
                    return "Grimmkin Flames";
                    
                case "Hunter's_Journal":
                case "Journal_Entry":
                    return "Journal Entries";
                    
                case "Geo_Rock":
                    return "Geo Rocks";
                    
                case "Boss_Geo":
                    return "Boss Geo";
                    
                case "Soul_Totem":
                    return "Soul Totems";
                    
                case "Lore_Tablet":
                    return "Lore Tablets";
                    
                case LocationNames.Sly:
                case LocationNames.Sly_Key:
                case LocationNames.Salubra:
                case LocationNames.Iselda:
                case LocationNames.Leg_Eater:
                case LocationNames.Seer:
                case LocationNames.Grubfather:
                case LocationNames.Egg_Shop:
                    return "Shops";
            }

            if (name.Contains("Map"))
                return "Maps"; 
            
            if (name.Contains("Stag"))
                return "Stags";
            
            return "Unknown";
        }

#if DEBUG
        public static Dictionary<string, PinDef> newPins;
        public static Dictionary<string, MapRoomDef> newRooms;

        public static void LoadDebugResources()
        {
            //newPins = JsonUtil.DeserializeFromExternalFile<Dictionary<string, PinDef>> ("newPins.json");
            newRooms = JsonUtil.DeserializeFromExternalFile<Dictionary<string, MapRoomDef>>("newRooms.json");
        }
#endif
    }
}