﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MapModS.CanvasUtil;
using MapModS.Data;
using MapModS.Map;
using MapModS.Settings;
using UnityEngine.Events;
using System;
using System.Text.RegularExpressions;

namespace MapModS.UI
{
    // All the following was modified from the GUI implementation of BenchwarpMod by homothetyhk
    internal class PauseMenu
    {
        public static GameObject Canvas;

        private static readonly Dictionary<string, (UnityAction<string>, Vector2)> _mainButtons = new Dictionary<string, (UnityAction<string>, Vector2)>
        {
            ["Spoilers"] = (SpoilersClicked, new Vector2(100f, 0f)),
            ["Style"] = (StyleClicked, new Vector2(200f, 0f)),
            ["Randomized"] = (RandomizedClicked, new Vector2(300f, 0f)),
            ["Others"] = (OthersClicked, new Vector2(0f, 30f)),
            ["Size"] = (SizeClicked, new Vector2(100f, 30f)),
            ["Mode"] = (ModeClicked, new Vector2(200f, 30f)),
        };

        private static CanvasPanel _mapControlPanel;

        public static void BuildMenu(GameObject _canvas)
        {
            Canvas = _canvas;

            _mapControlPanel = new CanvasPanel
                (_canvas, GUIController.Instance.Images["ButtonsMenuBG"], new Vector2(10f, 870f), new Vector2(1346f, 0f), new Rect(0f, 0f, 0f, 0f));
            _mapControlPanel.AddText("MapModLabel", "MapMod S", new Vector2(0f, -25f), Vector2.zero, GUIController.Instance.TrajanNormal, 18);

            Rect buttonRect = new(0, 0, GUIController.Instance.Images["ButtonRect"].width, GUIController.Instance.Images["ButtonRect"].height);

            // Main settings

            // Toggle the mod on or off
            _mapControlPanel.AddButton
                (
                    "Enable",
                    GUIController.Instance.Images["ButtonRect"],
                    new Vector2(0f, 0f),
                    Vector2.zero,
                    EnableClicked,
                    buttonRect,
                    GUIController.Instance.TrajanBold,
                    "Enable",
                    fontSize: 10
                );

            if (!MapModS.LS.ModEnabled)
            {
                UpdateEnable();

                if (GameManager.instance.IsGamePaused())
                {
                    _mapControlPanel.SetActive(true, false);
                }

                return;
            }

            foreach (KeyValuePair<string, (UnityAction<string>, Vector2)> pair in _mainButtons)
            {
                _mapControlPanel.AddButton
                (
                    pair.Key,
                    GUIController.Instance.Images["ButtonRect"],
                    pair.Value.Item2,
                    Vector2.zero,
                    pair.Value.Item1,
                    buttonRect,
                    GUIController.Instance.TrajanBold,
                    pair.Key,
                    fontSize: 10
                );
            }

            // New panel for pool buttons
            CanvasPanel pools = _mapControlPanel.AddPanel
            (
                "PoolsPanel",
                GUIController.Instance.Images["ButtonRectEmpty"],
                new Vector2(400f, 0f),
                Vector2.zero,
                new Rect(0f, 0f, GUIController.Instance.Images["DropdownBG"].width, 270f)
            );
            _mapControlPanel.AddButton
            (
                "PoolsToggle",
                GUIController.Instance.Images["ButtonRect"],
                new Vector2(300f, 30f),
                Vector2.zero,
                s => PoolsPanelClicked(),
                buttonRect,
                GUIController.Instance.TrajanBold,
                "Customize\nPins",
                fontSize: 10
            );

            // Collapse panel
            pools.SetActive(false, true);

            // Pool buttons
            //foreach (KeyValuePair<PoolGroup, (string, Vector2)> pair in _groupButtons)
            foreach (PoolGroup group in Enum.GetValues(typeof(PoolGroup)))
            {
                if (group == PoolGroup.Unknown) continue;

                float x_offset = (float) (group - 1) % 9 * 90;
                float y_offset = (int) (group - 1) / 9 * 30;
                string[] splitGroup = Regex.Split(group.ToString(), @"(?<!^)(?=[A-Z])");
                string cleanGroup;

                if (splitGroup.Length == 1)
                {
                    cleanGroup = splitGroup[0];
                }
                else
                {
                    cleanGroup = splitGroup[0] + "\n" + splitGroup[1];
                }

                pools.AddButton
                (
                    group.ToString(),
                    GUIController.Instance.Images["ButtonRectEmpty"],
                    new Vector2(x_offset, y_offset),
                    Vector2.zero,
                    PoolClicked,
                    buttonRect,
                    GUIController.Instance.TrajanBold,
                    cleanGroup,
                    fontSize: 10
                );
            }

            UpdateGUI();

            _mapControlPanel.SetActive(false, true); // collapse all subpanels

            if (GameManager.instance.IsGamePaused())
            {
                _mapControlPanel.SetActive(true, false);
            }
        }

        // Called every frame
        public static void Update()
        {
            if (_mapControlPanel == null || GameManager.instance == null)
            {
                return;
            }

            if (HeroController.instance == null || !GameManager.instance.IsGameplayScene() || !GameManager.instance.IsGamePaused())
            {
                // Any time we aren't at the Pause Menu / don't want to show the UI otherwise
                if (_mapControlPanel.Active) _mapControlPanel.SetActive(false, true);
                return;
            }

            // On the frame that we enter the Pause Menu
            if (!_mapControlPanel.Active)
            {
                _mapControlPanel.Destroy();
                BuildMenu(Canvas);
            }
        }

        // Update all the buttons (text, color)
        public static void UpdateGUI()
        {
            UpdateEnable();
            UpdateSpoilers();
            UpdateStyle();
            UpdateRandomized();
            UpdateOthers();
            UpdateSize();
            UpdateMode();

            foreach (PoolGroup group in Enum.GetValues(typeof(PoolGroup)))
            {
                if (group == PoolGroup.Unknown) continue;

                UpdatePool(group);
            }
        }

        public static void EnableClicked(string buttonName)
        {
            MapModS.LS.ToggleModEnabled();
            _mapControlPanel.Destroy();
            BuildMenu(Canvas);
        }

        private static void UpdateEnable()
        {
            _mapControlPanel.GetButton("Enable").SetTextColor
                (
                    MapModS.LS.ModEnabled ? Color.green : Color.red
                );
            _mapControlPanel.GetButton("Enable").UpdateText
                (
                    MapModS.LS.ModEnabled ? ("Mod\nEnabled") : ("Mod\nDisabled")
                );
        }

        public static void SpoilersClicked(string buttonName)
        {
            WorldMap.CustomPins.ToggleSpoilers();
            UpdateGUI();
        }

        private static void UpdateSpoilers()
        {
            _mapControlPanel.GetButton("Spoilers").SetTextColor
                (
                    MapModS.LS.SpoilerOn ? Color.green : Color.white
                );
            _mapControlPanel.GetButton("Spoilers").UpdateText
                (
                    MapModS.LS.SpoilerOn ? ("Spoilers:\non") : ("Spoilers:\noff")
                );
        }

        public static void StyleClicked(string buttonName)
        {
            WorldMap.CustomPins.TogglePinStyle();
            UpdateGUI();
        }

        private static void UpdateStyle()
        {
            switch (MapModS.LS.pinStyle)
            {
                case PinStyle.Normal:
                    _mapControlPanel.GetButton("Style").UpdateText("Style:\nnormal");
                    break;

                case PinStyle.Q_Marks_1:
                    _mapControlPanel.GetButton("Style").UpdateText("Style:\nq marks 1");
                    break;

                case PinStyle.Q_Marks_2:
                    _mapControlPanel.GetButton("Style").UpdateText("Style:\nq marks 2");
                    break;

                case PinStyle.Q_Marks_3:
                    _mapControlPanel.GetButton("Style").UpdateText("Style:\nq marks 3");
                    break;
            }
        }

        public static void RandomizedClicked(string buttonName)
        {
            WorldMap.CustomPins.ToggleRandomized();
            UpdateGUI();
        }

        private static void UpdateRandomized()
        {
            if (WorldMap.CustomPins == null) return;

            if (!WorldMap.CustomPins.RandomizedGroups.Any(MapModS.LS.GetOnFromGroup))
            {
                _mapControlPanel.GetButton("Randomized").SetTextColor(Color.white);
                _mapControlPanel.GetButton("Randomized").UpdateText("Randomized:\noff");
                MapModS.LS.RandomizedOn = false;
            }
            else if (WorldMap.CustomPins.RandomizedGroups.All(MapModS.LS.GetOnFromGroup))
            {
                _mapControlPanel.GetButton("Randomized").SetTextColor(Color.green);
                _mapControlPanel.GetButton("Randomized").UpdateText("Randomized:\non");
                MapModS.LS.RandomizedOn = true;
            }
            else
            {
                _mapControlPanel.GetButton("Randomized").SetTextColor(Color.yellow);
                _mapControlPanel.GetButton("Randomized").UpdateText("Randomized:\ncustom");
                MapModS.LS.RandomizedOn = true;
            }
        }

        public static void OthersClicked(string buttonName)
        {
            WorldMap.CustomPins.ToggleOthers();
            UpdateGUI();
        }

        private static void UpdateOthers()
        {
            if (WorldMap.CustomPins == null) return;

            if (!WorldMap.CustomPins.OthersGroups.Any(MapModS.LS.GetOnFromGroup))
            {
                _mapControlPanel.GetButton("Others").SetTextColor(Color.white);
                _mapControlPanel.GetButton("Others").UpdateText("Others:\noff");
                MapModS.LS.OthersOn = false;
            }
            else if (WorldMap.CustomPins.OthersGroups.All(MapModS.LS.GetOnFromGroup))
            {
                _mapControlPanel.GetButton("Others").SetTextColor(Color.green);
                _mapControlPanel.GetButton("Others").UpdateText("Others:\non");
                MapModS.LS.OthersOn = true;
            }
            else
            {
                _mapControlPanel.GetButton("Others").SetTextColor(Color.yellow);
                _mapControlPanel.GetButton("Others").UpdateText("Others:\ncustom");
                MapModS.LS.OthersOn = true;
            }
        }

        public static void SizeClicked(string buttonName)
        {
            MapModS.GS.TogglePinSize();
            UpdateGUI();
        }

        private static void UpdateSize()
        {
            switch (MapModS.GS.PinSizeSetting)
            {
                case Settings.GlobalSettings.PinSize.small:
                    _mapControlPanel.GetButton("Size").UpdateText("Pin Size\nsmall");
                    break;
                case Settings.GlobalSettings.PinSize.medium:
                    _mapControlPanel.GetButton("Size").UpdateText("Pin Size\nmedium");
                    break;
                case Settings.GlobalSettings.PinSize.large:
                    _mapControlPanel.GetButton("Size").UpdateText("Pin Size\nlarge");
                    break;
            }
        }

        public static void ModeClicked(string buttonName)
        {
            MapModS.LS.ToggleFullMap();
            UpdateGUI();
        }

        private static void UpdateMode()
        {
            switch (MapModS.LS.mapState)
            {
                case Mode.FullMap:
                    _mapControlPanel.GetButton("Mode").SetTextColor(Color.green);
                    _mapControlPanel.GetButton("Mode").UpdateText("Mode:\n Full Map");
                    break;
                case Mode.AllPins:
                    _mapControlPanel.GetButton("Mode").SetTextColor(Color.white);
                    _mapControlPanel.GetButton("Mode").UpdateText("Mode:\n All Pins");
                    break;
                default:
                    _mapControlPanel.GetButton("Mode").SetTextColor(Color.white);
                    _mapControlPanel.GetButton("Mode").UpdateText("Mode:\n Pins Over Map");
                    break;
            }
        }

        public static void PoolsPanelClicked()
        {
            _mapControlPanel.TogglePanel("PoolsPanel");
        }

        public static void PoolClicked(string buttonName)
        {
            MapModS.LS.SetOnFromGroup(buttonName, !MapModS.LS.GetOnFromGroup(buttonName));
            UpdateGUI();
        }

        private static void UpdatePool(PoolGroup pool)
        {
            //if (pool == PoolGroup.Rock)
            //{
            //    _mapControlPanel.GetPanel("PoolsPanel").GetButton(pool.ToString()).UpdateText
            //        (
            //            _groupButtons[pool].Item1 + "\n"
            //            + MapModS.LS.GeoRockCounter + " / " + "207"
            //        );
            //}

            bool setting = MapModS.LS.GetOnFromGroup(pool);

            _mapControlPanel.GetPanel("PoolsPanel").GetButton(pool.ToString()).SetTextColor
                (
                    setting ? Color.green : Color.white
                );
        }
    }
}