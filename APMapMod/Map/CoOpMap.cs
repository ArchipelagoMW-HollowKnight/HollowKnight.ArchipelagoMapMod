using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APMapMod.Concurrency;
using APMapMod.Settings;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Modding;
using Newtonsoft.Json.Linq;
using Satchel;
using UnityEngine;
using WebSocketSharp;
using static System.Threading.ThreadPool;
using Object = UnityEngine.Object;

namespace APMapMod.Map {
    /// <summary>
    /// A class that manages player locations on the in-game map. Taken and modified from Extremelyd1 HKMP mod
    /// found here https://github.com/Extremelyd1/HKMP
    /// </summary>
    internal class CoOpMap : MonoBehaviour
    {
        /// <summary>
        /// The archipelago session instance.
        /// </summary>
        private ArchipelagoSession _netClient;
        /// <summary>
        /// The current game settings.
        /// </summary>
        private LocalSettings _ls;
        /// <summary>
        /// The current global settings settings.
        /// </summary>
        private GlobalSettings _gs;
        
        /// <summary>
        /// Dictionary containing map icon objects per player ID
        /// </summary>
        private ConcurrentDictionary<int, GameObject> _mapIcons;

        private Dictionary<string, int> _playerList;

        private Dictionary<int, (Vector2 pos, Color color)> _locationUpdates;

        /// <summary>
        /// The last sent map position.
        /// </summary>
        private Vector3 _lastPosition;

        /// <summary>
        /// Whether we should display the map icons. True if the map is opened, false otherwise.
        /// </summary>
        private bool _displayingIcons;
        
        /// <summary>
        /// My Current position to send.
        /// </summary>
        private Vector2 _myPos;

        /// <summary>
        /// Whether my current possition has been updated since I last sent it.
        /// </summary>
        private bool _sendNewPos;

        public static bool white_palace = false;
        public static bool gods_glory = false;

        public static void Hook()
        {
            On.GameManager.SetGameMap += GameManager_SetGameMap;
        }
        
        public static void UnHook()
        {
            On.GameManager.SetGameMap -= GameManager_SetGameMap;
        }
        
        private static void GameManager_SetGameMap(On.GameManager.orig_SetGameMap orig, GameManager self, GameObject goGameMap)
        {
            orig(self, goGameMap);

            goGameMap.AddComponent<CoOpMap>();
        }
        

        public void OnEnable()
        {
            _netClient = APMapMod.Instance.session;
            _ls = APMapMod.LS;
            _gs = APMapMod.GS;

            _mapIcons = new ConcurrentDictionary<int, GameObject>();
            _playerList = new Dictionary<string, int>();
            _locationUpdates = new Dictionary<int, (Vector2 pos, Color color)>();

            // register disconnect notices from AP session
            _netClient.Socket.SocketClosed += netClient_OnDisconnect;
            _netClient.Socket.PacketReceived += netClient_onPacket;

            // Register a hero controller update callback, so we can update the map icon position
            On.HeroController.Update += HeroControllerOnUpdate;

            // Register when the player closes their map, so we can hide the icons
            On.GameMap.CloseQuickMap += OnCloseQuickMap;

            // Register when the player opens their map, which is when the compass position is calculated 
            On.GameMap.PositionCompass += OnPositionCompass;

            var newTags = _netClient.ConnectionInfo.Tags.ToList();
            if (!newTags.Contains("APMapMod"))
            {
                newTags.Add("APMapMod");
                _netClient.UpdateConnectionOptions(newTags.ToArray(), _netClient.ConnectionInfo.ItemsHandlingFlags);
            }
            
            EnableUpdates();
        }
        
        public void OnDisable() {
            // register disconnect notices from AP session
            _netClient.Socket.SocketClosed -= netClient_OnDisconnect;
            _netClient.Socket.PacketReceived -= netClient_onPacket;

            // Register a hero controller update callback, so we can update the map icon position
            On.HeroController.Update -= HeroControllerOnUpdate;

            // Register when the player closes their map, so we can hide the icons
            On.GameMap.CloseQuickMap -= OnCloseQuickMap;

            // Register when the player opens their map, which is when the compass position is calculated 
            On.GameMap.PositionCompass += OnPositionCompass;
            
            DisableUpdates();
        }

        internal void EnableUpdates()
        {
            _playerList.Add(_netClient.ConnectionInfo.Uuid,0);
            OnPlayerMapUpdate(0,
                _ls.IconVisibility is IconVisibility.Both or IconVisibility.Own ? _myPos : Vector2.zero,
                _gs.IconColor);

            StartCoroutine(SendPacketRoutine());
            StartCoroutine(UpdatePlayersRoutine());
        }

        internal void DisableUpdates()
        {
            _myPos = Vector2.zero;
            _playerList.Clear();
            StopAllCoroutines();
            SendUpdatePacket();
            RemoveAllIcons();
        }


        private void SendUpdatePacket()
        {
            QueueUserWorkItem((_) =>
            {
                if (!_netClient.Socket.Connected) return;
                var color =  _gs.IconColor;
                var bounce = new BouncePacket
                {
                    Tags = new List<string>
                    {
                        "APMapMod"
                    },
                    Data = new Dictionary<string, JToken>
                    {
                        {"type", "apmapmodcoop"},
                        {"uuid", _netClient.ConnectionInfo.Uuid},
                        {"pos", $"{Convert.ToString(_myPos.x, CultureInfo.InvariantCulture.NumberFormat)}/{Convert.ToString(_myPos.y, CultureInfo.InvariantCulture.NumberFormat)}"},
                        {"color", $"{Convert.ToString(color.r, CultureInfo.InvariantCulture.NumberFormat)}/{Convert.ToString(color.g, CultureInfo.InvariantCulture.NumberFormat)}/{Convert.ToString(color.b, CultureInfo.InvariantCulture.NumberFormat)}/{Convert.ToString(color.a, CultureInfo.InvariantCulture.NumberFormat)}"},
                    }
                };
                _netClient.Socket.SendPacketAsync(bounce);
            });
        }
        
        private IEnumerator SendPacketRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(5);
                if (!_sendNewPos) continue;
                SendUpdatePacket();
                _sendNewPos = false;
            }
        }  
        
        private IEnumerator UpdatePlayersRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(.1f);
                foreach (var id in _locationUpdates.Keys)
                {   
                    OnPlayerMapUpdate(id, _locationUpdates[id].pos, _locationUpdates[id].color);
                }
                _locationUpdates.Clear();
            }
        }

        public void OpenMap()
        {
            _displayingIcons = true;
            UpdateMapIconsActive();
        }

        private void netClient_onPacket(ArchipelagoPacketBase packet)
        {

            // check for bounced packet only
            if (packet.PacketType != ArchipelagoPacketType.Bounced)
                return;
            
            var bounce = (BouncedPacket) packet;
            if (!bounce.Data.TryGetValue("type", out var type))
            {
                if (type.ToString() != "apmapmodcoop") return;
            }
            // its our bounce packet yay!
            
            string uuid = null;
            if (bounce.Data.TryGetValue("uuid", out var uuidJToken))
                uuid = uuidJToken.ToString();
            
            // ignore our own updates. and if we fail to parse because uuid is missing.
            if (uuid == _netClient.ConnectionInfo.Uuid || uuid == null)
                return;

            if (!_playerList.ContainsKey(uuid))
            {
                _playerList.Add(uuid, _playerList.Count);
            }

            var id = _playerList[uuid];
            
            var pos = new Vector2();
            //get vector2 sent.
            if (bounce.Data.TryGetValue("pos", out var posJToken))
            {
                var p = posJToken.ToString().Split('/');
                pos.x = float.Parse(p[0], CultureInfo.InvariantCulture.NumberFormat);
                pos.y = float.Parse(p[1], CultureInfo.InvariantCulture.NumberFormat);
            }
            else return;

            var color = Color.white;
            if (bounce.Data.TryGetValue("color", out var colorJToken))
            {
                var c = colorJToken.ToString().Split('/');
                color = new Color(float.Parse(c[0], CultureInfo.InvariantCulture.NumberFormat), float.Parse(c[1], CultureInfo.InvariantCulture.NumberFormat), float.Parse(c[2], CultureInfo.InvariantCulture.NumberFormat), float.Parse(c[3], CultureInfo.InvariantCulture.NumberFormat));
            }
            else return;
            
            APMapMod.Instance.LogFine($"updating player {id} to pos: {pos.ToString()} with color {color.ToString()}");
            if (_ls.IconVisibility is IconVisibility.Both or IconVisibility.Others)
                _locationUpdates[id] = (pos, color);
            else
                _locationUpdates[id] = (Vector2.zero, color);
        }

        /// <summary>
        /// Callback method for the HeroController#Update method.
        /// </summary>
        /// <param name="orig">The original method.</param>
        /// <param name="self">The HeroController instance.</param>
        private void HeroControllerOnUpdate(On.HeroController.orig_Update orig, HeroController self) {
            // Execute the original method
            orig(self);

            var newPosition = GetMapLocation();
            _myPos = new Vector2(newPosition.x, newPosition.y);
            OnPlayerMapUpdate(0,
                _ls.IconVisibility is IconVisibility.Both or IconVisibility.Own ? _myPos : Vector2.zero,
                _gs.IconColor);

            if (Vector2.Distance(_lastPosition, _myPos) > .25f) {
                // Update the last position, and flag a new send because it changed.
                _lastPosition = newPosition;
                _sendNewPos = true;
            }
        }

        /// <summary>
        /// Get the current map location of the local player.
        /// </summary>
        /// <returns>A Vector3 representing the map location.</returns>
        private Vector3 GetMapLocation() {
            // Get the game manager instance
            var gameManager = GameManager.instance;
            // Get the current map zone of the game manager and check whether we are in
            // an area that doesn't shop up on the map
            var currentMapZone = gameManager.GetCurrentMapZone();
            if (currentMapZone.Equals("DREAM_WORLD")
                || (currentMapZone.Equals("WHITE_PALACE") && !white_palace)
                || (currentMapZone.Equals("GODS_GLORY") && !gods_glory)) {
                return Vector3.zero;
            }

            // Get the game map instance
            var gameMap = GetGameMap();
            if (gameMap == null) {
                return Vector3.zero;
            }

            // This is what the PositionCompass method in GameMap calculates to determine
            // the compass icon location
            // We mimic it, because we need it to always update instead of only when the map is open
            string sceneName;
            if (gameMap.inRoom) {
                currentMapZone = gameMap.doorMapZone;
                sceneName = gameMap.doorScene;
            } else {
                sceneName = gameManager.sceneName;
            }

            GameObject sceneObject = null;
            var areaObject = GetAreaObjectByName(gameMap, currentMapZone);

            if (areaObject == null) {
                return Vector3.zero;
            }

            for (var i = 0; i < areaObject.transform.childCount; i++) {
                var childObject = areaObject.transform.GetChild(i).gameObject;
                if (childObject.name.Equals(sceneName)) {
                    sceneObject = childObject;
                    break;
                }
            }

            if (sceneObject == null) {
                return Vector3.zero;
            }

            var sceneObjectPos = sceneObject.transform.localPosition;
            var areaObjectPos = areaObject.transform.localPosition;

            var currentScenePos = new Vector3(
                sceneObjectPos.x + areaObjectPos.x,
                sceneObjectPos.y + areaObjectPos.y,
                0f
            );
            try
            {
                var size = sceneObject.GetComponent<SpriteRenderer>().sprite.bounds.size;
                var gameMapScale = gameMap.transform.localScale;
                
                Vector3 position;

                if (gameMap.inRoom)
                {
                    position = new Vector3(
                        currentScenePos.x - size.x / 2.0f + (gameMap.doorX + gameMap.doorOriginOffsetX) /
                        gameMap.doorSceneWidth *
                        size.x,
                        currentScenePos.y - size.y / 2.0f + (gameMap.doorY + gameMap.doorOriginOffsetY) /
                        gameMap.doorSceneHeight *
                        gameMapScale.y,
                        -1f
                    );
                }
                else
                {
                    var playerPosition = HeroController.instance.gameObject.transform.position;
                    
                    var originOffsetX = ReflectionHelper.GetField<GameMap, float>(gameMap, "originOffsetX");
                    var originOffsetY = ReflectionHelper.GetField<GameMap, float>(gameMap, "originOffsetY");
                    var sceneWidth = ReflectionHelper.GetField<GameMap, float>(gameMap, "sceneWidth");
                    var sceneHeight = ReflectionHelper.GetField<GameMap, float>(gameMap, "sceneHeight");

                    position = new Vector3(
                        currentScenePos.x - size.x / 2.0f + (playerPosition.x + originOffsetX) / sceneWidth *
                        size.x,
                        currentScenePos.y - size.y / 2.0f + (playerPosition.y + originOffsetY) / sceneHeight *
                        size.y,
                        -1f
                    );

                }
                
                return position;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Callback method for when we receive a map update from another player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="position">The new position on the map.</param>
        /// <param name="color">Color to tint the icon</param>
        public void OnPlayerMapUpdate(int id, Vector2 position, Color color) {
            if (position == Vector2.zero) {
                // We have received an empty update, which means that we need to remove
                // the icon if it exists
                if (_mapIcons.TryGetValue(id, out _)) {
                    RemovePlayerIcon(id);
                }

                return;
            }

            // If there does not exist a player icon for this id yet, we create it
            if (!_mapIcons.TryGetValue(id, out _)) {
                CreatePlayerIcon(id, position);
                _mapIcons[id].GetComponent<tk2dSprite>().color = color;
                return;
            }
            
            // Check whether the object still exists
            var mapObject = _mapIcons[id];
            if (mapObject == null) {
                _mapIcons.Remove(id);
                return;
            }


            // color the icon
            _mapIcons[id].GetComponent<tk2dSprite>().color = color;

            // Check if the transform is still valid and otherwise destroy the object
            // This is possible since whenever we receive a new update packet, we
            // will just create a new map icon
            var transform = mapObject.transform;
            if (transform == null) {
                Object.Destroy(mapObject);
                _mapIcons.Remove(id);
                return;
            }
            
            
            // Subtract ID * 0.01 from the Z position to prevent Z-fighting with the icons
            var unityPosition = new Vector3(
                position.x, 
                position.y,
                id * -0.01f - 5f
            );

            // Update the position of the player icon
            transform.localPosition = unityPosition;
        }

        /// <summary>
        /// Callback method on the GameMap#CloseQuickMap method.
        /// </summary>
        /// <param name="orig">The original method.</param>
        /// <param name="self">The GameMap instance.</param>
        private void OnCloseQuickMap(On.GameMap.orig_CloseQuickMap orig, GameMap self) {
            orig(self);

            // We have closed the map, so we can disable the icons
            _displayingIcons = false;
            UpdateMapIconsActive();
        }

        /// <summary>
        /// Callback method on the GameMap#PositionCompass method.
        /// </summary>
        /// <param name="orig">The original method.</param>
        /// <param name="self">The GameMap instance.</param>
        /// <param name="posShade">The boolean value whether to position the shade.</param>
        private void OnPositionCompass(On.GameMap.orig_PositionCompass orig, GameMap self, bool posShade) {
            orig(self, posShade);

            var posGate = ReflectionHelper.GetField<GameMap, bool>(self, "posGate");

            // If this is a call where we either update the shade position or the dream gate position,
            // we don't want to display the icons again, because we haven't opened the map
            if (posShade || posGate) {
                return;
            }

            // Otherwise, we have opened the map
            _displayingIcons = true;
            UpdateMapIconsActive();
        }

        /// <summary>
        /// Update all existing map icons based on whether they should be active according to game settings.
        /// </summary>
        private void UpdateMapIconsActive() {
            foreach (var mapIcon in _mapIcons.GetCopy().Values) {
                mapIcon.SetActive(_displayingIcons);
            }
        }

        /// <summary>
        /// Create a map icon for a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="position">The position of the map icon.</param>
        private void CreatePlayerIcon(int id, Vector2 position) {
            var gameMap = GetGameMap();
            if (gameMap == null) {
                return;
            }

            var compassIconPrefab = gameMap.compassIcon;
            if (compassIconPrefab == null) {
                APMapMod.Instance.LogError("CompassIcon prefab is null");
                return;
            }

            // Create a new player icon relative to the game map
            var mapIcon = Instantiate(
                compassIconPrefab,
                gameMap.gameObject.transform
            );
            mapIcon.SetActive(_displayingIcons);
            
            //scale all other player icons to half size.
            if(id != 0)
                mapIcon.SetScale(.5f,.5f);

            // Subtract ID * 0.01 from the Z position to prevent Z-fighting with the icons
            var unityPosition = new Vector3(
                position.x, 
                position.y,
                id * -0.01f - 5f
            );
            
            // Set the position of the player icon
            mapIcon.transform.localPosition = unityPosition;

            // Remove the bob effect when walking with the map
            Destroy(mapIcon.LocateMyFSM("Mapwalk Bob"));

            // Put it in the list
            _mapIcons[id] = mapIcon;
        }
        
        /// <summary>
        /// Remove the map icon for a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        public void RemovePlayerIcon(int id) {
            if (!_mapIcons.TryGetValue(id, out var playerIcon)) {
                APMapMod.Instance.LogWarn($"Tried to remove player icon of ID: {id}, but it didn't exist");
                return;
            }

            // Destroy the player icon and then remove it from the list
            Object.Destroy(playerIcon);
            _mapIcons.Remove(id);
        }

        /// <summary>
        /// Remove all map icons.
        /// </summary>
        public void RemoveAllIcons() {
            // Destroy all existing map icons
            foreach (var mapIcon in _mapIcons.GetCopy().Values) {
                Object.Destroy(mapIcon);
            }

            // Clear the mapping
            _mapIcons.Clear();
        }

        /// <summary>
        /// Callback method for when the local user disconnects.
        /// </summary>
        private void netClient_OnDisconnect(CloseEventArgs eventArgs) { 
            DisableUpdates();

            // Reset variables to their initial values
            _lastPosition = Vector3.zero;
        }

        /// <summary>
        /// Get a valid instance of the GameMap class.
        /// </summary>
        /// <returns>An instance of GameMap.</returns>
        private GameMap GetGameMap() {
            var gameManager = GameManager.instance;
            if (gameManager == null) {
                return null;
            }

            var gameMapObject = gameManager.gameMap;
            if (gameMapObject == null) {
                return null;
            }

            var gameMap = gameMapObject.GetComponent<GameMap>();
            if (gameMap == null) {
                return null;
            }

            return gameMap;
        }

        /// <summary>
        /// Get an area object by its name.
        /// </summary>
        /// <param name="gameMap">The GameMap instance.</param>
        /// <param name="name">The name of the area to retrieve.</param>
        /// <returns>A GameObject representing the map area.</returns>
        private static GameObject GetAreaObjectByName(GameMap gameMap, string name) {
            switch (name) {
                case "ABYSS":
                    return gameMap.areaAncientBasin;
                case "CITY":
                case "KINGS_STATION":
                case "SOUL_SOCIETY":
                case "LURIENS_TOWER":
                    return gameMap.areaCity;
                case "CLIFFS":
                    return gameMap.areaCliffs;
                case "CROSSROADS":
                case "SHAMAN_TEMPLE":
                    return gameMap.areaCrossroads;
                case "MINES":
                    return gameMap.areaCrystalPeak;
                case "DEEPNEST":
                case "BEASTS_DEN":
                    return gameMap.areaDeepnest;
                case "FOG_CANYON":
                case "MONOMON_ARCHIVE":
                    return gameMap.areaFogCanyon;
                case "WASTES":
                case "QUEENS_STATION":
                    return gameMap.areaFungalWastes;
                case "GREEN_PATH":
                    return gameMap.areaGreenpath;
                case "OUTSKIRTS":
                case "HIVE":
                case "COLOSSEUM":
                    return gameMap.areaKingdomsEdge;
                case "ROYAL_GARDENS":
                    return gameMap.areaQueensGardens;
                case "RESTING_GROUNDS":
                    return gameMap.areaRestingGrounds;
                case "TOWN":
                case "KINGS_PASS":
                    return gameMap.areaDirtmouth;
                case "WATERWAYS":
                case "GODSEEKER_WASTE":
                    return gameMap.areaWaterways;
                default:
                    return gameMap.gameObject.FindGameObjectInChildren(name);
            }
        }
    }
}