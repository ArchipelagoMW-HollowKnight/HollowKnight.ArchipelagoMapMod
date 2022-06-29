using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using APMapMod.Data;
using APMapMod.Map;
using APMapMod.Settings;
using APMapMod.Shop;
using APMapMod.Trackers;
using APMapMod.UI;
using Archipelago.MultiClient.Net;
using Modding;
using UnityEngine;

namespace APMapMod
{
    public class APMapMod : Mod, ILocalSettings<LocalSettings>, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        public static APMapMod Instance;

        public bool ToggleButtonInsideMenu { get; }
        
        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public override int LoadPriority() => 10;

        public static LocalSettings LS = new();
        public void OnLoadLocal(LocalSettings ls) => LS = ls;
        public LocalSettings OnSaveLocal() => LS;

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings gs) => GS = gs;
        public GlobalSettings OnSaveGlobal() => GS;

        public ArchipelagoSession session;

        public override void Initialize()
        {
            Log("Initializing...");

            Instance = this;

            Dependencies.GetDependencies();

            foreach (KeyValuePair<string, Assembly> pair in Dependencies.strictDependencies)
            {
                if (pair.Value == null)
                {
                    Log($"{pair.Key} is not installed. APMapMod disabled");
                    return;
                }
            }

            foreach (KeyValuePair<string, Assembly> pair in Dependencies.optionalDependencies)
            {
                if (pair.Value == null)
                {
                    Log($"{pair.Key} is not installed. Some features are disabled.");
                }
            }

            GUIController.Setup();
            
            try
            {
                MainData.Load();
            }
            catch (Exception e)
            {
                LogError($"Error loading data!\n{e}");
                throw;
            }

            Archipelago.HollowKnight.Archipelago.OnArchipelagoGameStarted += Hook;
            Archipelago.HollowKnight.Archipelago.OnArchipelagoGameEnded += Unhook;

            if (GS.IconColorR == -1)
            {
                // default value lets randomize it!
                GS.IconColor= Utils.GetRandomLightColor();
            }
            
            Log("Initialization complete.");
        }

        private void Hook()
        {
            Log("Activating mod");

            // Load default/custom assets
            SpriteManager.LoadPinSprites();
            Colors.LoadCustomColors();
            Data.Data.Load();

            if (Dependencies.HasBenchwarp())
            {
                BenchwarpInterop.Load();
            }

            // Track when items are picked up/Geo Rocks are broken
            ItemTracker.Hook();
            GeoRockTracker.Hook();
            
            // Track when Hints are given in AP
            HintTracker.Hook();

            // Remove Map Markers from the Shop (when mod is enabled)
            ShopChanger.Hook();

            // Modify overall Map behaviour
            WorldMap.Hook();

            // Modify overall Quick Map behaviour
            QuickMap.Hook();

            // Allow the full Map to be toggled
            FullMap.Hook();

            // Disable Vanilla Pins when mod is enabled
            PinsVanilla.Hook();

            // Immediately update Map on scene change
            Quill.Hook();

            // Add all the UI elements (world map, quick map, pause menu)
            GUI.Hook();
            
            // enable player icon tracking.
            CoOpMap.Hook();

            session = Archipelago.HollowKnight.Archipelago.Instance.session;
            
            Log("Done Activating Mod");
        }

        private void Unhook()
        {
            ItemTracker.Unhook();
            GeoRockTracker.Unhook();
            ShopChanger.Unhook();
            WorldMap.Unhook();
            QuickMap.Unhook();
            FullMap.Unhook();
            PinsVanilla.Unhook();
            Quill.Unhook();
            CoOpMap.UnHook();
            GUI.Unhook();
        }
        
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            return BetterMenu.GetMenuScreen(modListMenu, toggleDelegates);
        }
    }
}