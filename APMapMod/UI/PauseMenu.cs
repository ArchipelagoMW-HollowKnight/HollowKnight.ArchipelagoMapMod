using System;
using System.Collections.Generic;
using APMapMod.Data;
using APMapMod.Map;
using APMapMod.Settings;
using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;
using AP = Archipelago.HollowKnight.Archipelago;

namespace APMapMod.UI
{
    internal class PauseMenu
    {
        private static LayoutRoot layout;

        private static bool panelActive = false;

        private static readonly Dictionary<string, (Action<Button>, Action<Button>)> _mainButtons = new()
        {
            { "Enabled", (ToggleEnabled, UpdateEnabled) },
            { "Icon Visibility", (ToggleIconVisibility, UpdateIconVisibility) },
            { "Randomized", (ToggleRandomized, UpdateRandomized) },
            { "Others", (ToggleOthers, UpdateOthers) },
            { "Style", (ToggleStyle, UpdateStyle) },
            { "Size", (ToggleSize, UpdateSize) },
            { "Mode", (ToggleMode, UpdateMode) },
            { "Customize Pins", (ToggleCustomizePins, UpdateCustomizePins) }
        };

        private static readonly Dictionary<string, (KeyCode, Action<Button>)> _hotkeys = new()
        {
            { "Icon Visibility", (KeyCode.Alpha1, ToggleIconVisibility) },
            { "Randomized", (KeyCode.Alpha2, ToggleRandomized) },
            { "Others", (KeyCode.Alpha3, ToggleOthers) },
            { "Style", (KeyCode.Alpha4, ToggleStyle) },
            { "Size", (KeyCode.Alpha5, ToggleSize) }
        };

        private static readonly Dictionary<string, (Action<Button>, Action<Button>)> _auxButtons = new()
        {
            { "Persistent", (TogglePersistent, UpdatePersistent) },
            { "Group By", (ToggleGroupBy, UpdateGroupBy) }
        };

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Pause Menu");
                layout.VisibilityCondition = GameManager.instance.IsGamePaused;

                TextObject title = new(layout, "APMapMod")
                {
                    TextAlignment = HorizontalAlignment.Left,
                    ContentColor = Colors.GetColor(ColorSetting.UI_Neutral),
                    FontSize = 20,
                    Font = MagicUI.Core.UI.TrajanBold,
                    Padding = new(10f, 840f, 10f, 10f),
                    Text = "APMapMod",
                };

                DynamicUniformGrid mainButtons = new(layout, "Main Buttons")
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Orientation = Orientation.Vertical,
                    Padding = new(10f, 865f, 10f, 10f),
                    HorizontalSpacing = 5f,
                    VerticalSpacing = 5f
                };

                mainButtons.ChildrenBeforeRollover = 4;

                foreach (KeyValuePair<string, (Action<Button>, Action<Button>)> kvp in _mainButtons)
                {
                    Button button = new(layout, kvp.Key)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        BorderColor = Colors.GetColor(ColorSetting.UI_Borders),
                        MinHeight = 28f,
                        MinWidth = 95f,
                        Font = MagicUI.Core.UI.TrajanBold,
                        FontSize = 11,
                        Margin = 0f
                    };

                    button.Click += kvp.Value.Item1;
                    mainButtons.Children.Add(button);
                }
            }

            DynamicUniformGrid poolButtons = new(layout, "Pool Buttons")
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Orientation = Orientation.Vertical,
                Padding = new(415f, 865f, 10f, 10f),
                HorizontalSpacing = 0f,
                VerticalSpacing = 5f
            };

            poolButtons.ChildrenBeforeRollover = 10;

            List<string> groupButtonNames = MainData.usedPoolGroups;

            if (!Dependencies.HasBenchRando() || !BenchwarpInterop.IsBenchRandoEnabled())
            {
                groupButtonNames.Add("Benches (Vanilla)");
            }

            foreach (string group in groupButtonNames)
            {
                Button button = new(layout, group)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Borderless = true,
                    MinHeight = 28f,
                    MinWidth = 85f,
                    Content = group.Replace(" ", "\n"),
                    Font = MagicUI.Core.UI.TrajanNormal,
                    FontSize = 11,
                    Margin = 0f
                };

                button.Click += TogglePool;
                poolButtons.Children.Add(button);
            }

            StackLayout auxButtons = new(layout, "Aux Buttons")
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
                Padding = new(210f, 931f, 10f, 10f),
                Spacing = 5f
            };

            foreach (KeyValuePair<string, (Action<Button>, Action<Button>)> kvp in _auxButtons)
            {
                Button button = new(layout, kvp.Key)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Borderless = true,
                    MinHeight = 28f,
                    MinWidth = 95f,
                    Font = MagicUI.Core.UI.TrajanBold,
                    FontSize = 11,
                    Margin = 0f
                };

                button.Click += kvp.Value.Item1;
                auxButtons.Children.Add(button);
            }

            layout.ListenForHotkey(KeyCode.M, () =>
            {
                ToggleEnabled((Button)layout.GetElement("Enabled"));
            }, ModifierKeys.Ctrl);

            foreach (KeyValuePair<string, (KeyCode, Action<Button>)> kvp in _hotkeys)
            {
                layout.ListenForHotkey(kvp.Value.Item1, () =>
                {
                    kvp.Value.Item2.Invoke((Button)layout.GetElement(kvp.Key));
                }, ModifierKeys.Ctrl, () => APMapMod.LS.modEnabled);
            }

            UpdateAll();
        }

        public static void Destroy()
        {
            layout?.Destroy();
            layout = null;
        }

        private static void UpdateAll()
        {
            foreach (KeyValuePair<string, (Action<Button>, Action<Button>)> kvp in _mainButtons)
            {
                Button button = (Button)layout.GetElement(kvp.Key);

                kvp.Value.Item2.Invoke(button);

                if (kvp.Key == "Enabled") continue;

                if (APMapMod.LS.modEnabled)
                {
                    button.Visibility = Visibility.Visible;
                }
                else
                {
                    button.Visibility = Visibility.Hidden;
                }
            }

            foreach (string group in new List<string>(MainData.usedPoolGroups) { "Benches(Vanilla)" })
            {
                if (layout.GetElement(group) == null) continue;

                UpdatePool((Button)layout.GetElement(group));
            }

            if (APMapMod.LS.modEnabled && panelActive)
            {
                layout.GetElement("Pool Buttons").Visibility = Visibility.Visible;
                layout.GetElement("Aux Buttons").Visibility = Visibility.Visible;
            }
            else
            {
                layout.GetElement("Pool Buttons").Visibility = Visibility.Hidden;
                layout.GetElement("Aux Buttons").Visibility = Visibility.Hidden;
            }

            foreach (KeyValuePair<string, (Action<Button>, Action<Button>)> kvp in _auxButtons)
            {
                kvp.Value.Item2.Invoke((Button)layout.GetElement(kvp.Key));
            }
        }

        public static void ToggleEnabled(Button sender)
        {
            if (GUI.lockToggleEnable) return;

            APMapMod.LS.ToggleModEnabled();

            UIManager.instance.checkpointSprite.Show();
            UIManager.instance.checkpointSprite.Hide();

            if (GUI.worldMapOpen || GUI.quickMapOpen)
            {
                GUI.lockToggleEnable = true;
                MapText.SetToRefresh();
            }
            else
            {
                MapText.UpdateAll();
            }

            if (!APMapMod.LS.modEnabled)
            {
                WorldMap.goCustomPins.SetActive(false);
                WorldMap.goExtraRooms.SetActive(false);
                FullMap.PurgeMap();
                MapRooms.ResetMapColors(GameManager.instance.gameMap);
                panelActive = false;
            }

            UpdateAll();
        }

        private static void UpdateEnabled(Button sender)
        {
            if (APMapMod.LS.modEnabled)
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                sender.Content = $"Mod\nEnabled";
            }
            else
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Disabled);
                sender.Content = $"Mod\nDisabled";
            }
        }

        public static void ToggleIconVisibility(Button sender)
        {
            APMapMod.LS.ToggleIconVisibility();

            UpdateAll();
            MapText.UpdateAll();
        }

        private static void UpdateIconVisibility(Button sender)
        {
            
            switch (APMapMod.LS.IconVisibility)
            {
                case IconVisibility.Both:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                    sender.Content = $"Player Icons:\nBoth";
                    break;
                
                case IconVisibility.Own:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    sender.Content = $"Player Icons:\nOwn";
                    break;
                
                case IconVisibility.Others:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    sender.Content = $"Player Icons:\nOthers";
                    break;
                
                case IconVisibility.None:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Disabled);
                    sender.Content = $"Player Icons:\nNone";
                    break;
            }
        }

        public static void ToggleRandomized(Button sender)
        {
            APMapMod.LS.ToggleRandomizedOn();
            WorldMap.CustomPins.ResetPoolSettings();
            WorldMap.CustomPins.SetPinsActive();
            WorldMap.CustomPins.SetSprites();

            UpdateAll();
            MapText.UpdateAll();
        }

        private static void UpdateRandomized(Button sender)
        {
            if (WorldMap.CustomPins == null) return;

            string text = $"Randomized:\n";

            if (APMapMod.LS.randomizedOn)
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                text += "on";
            }
            else
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                text += "off";
            }

            if (WorldMap.CustomPins.IsRandomizedCustom())
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
                text += $" (custom)";
            }

            sender.Content = text;
        }

        public static void ToggleOthers(Button sender)
        {
            APMapMod.LS.ToggleOthersOn();
            WorldMap.CustomPins.ResetPoolSettings();
            WorldMap.CustomPins.SetPinsActive();
            WorldMap.CustomPins.SetSprites();

            UpdateAll();
            MapText.UpdateAll();
        }

        private static void UpdateOthers(Button sender)
        {
            if (WorldMap.CustomPins == null) return;

            string text = $"Others:\n";

            if (APMapMod.LS.othersOn)
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                text += "on";
            }
            else
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                text += "off";
            }

            if (WorldMap.CustomPins.IsOthersCustom())
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
                text += $" (custom)";
            }

            sender.Content = text;
        }

        public static void ToggleStyle(Button sender)
        {
            APMapMod.GS.TogglePinStyle();
            WorldMap.CustomPins.SetSprites();

            UpdateAll();
            MapText.UpdateAll();
        }

        private static void UpdateStyle(Button sender)
        {
            string text = $"Pin Style:\n";

            switch (APMapMod.GS.pinStyle)
            {
                case PinStyle.Normal:
                    text += "normal";
                    break;

                case PinStyle.Q_Marks_1:
                    text += $"q marks 1";
                    break;

                case PinStyle.Q_Marks_2:
                    text += $"q marks 2";
                    break;

                case PinStyle.Q_Marks_3:
                    text += $"q marks 3";
                    break;
            }

            sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
            sender.Content = text;
        }

        public static void ToggleSize(Button sender)
        {
            APMapMod.GS.TogglePinSize();

            if (WorldMap.CustomPins != null)
            {
                WorldMap.CustomPins.ResizePins("None selected");
            }

            if (APMapMod.GS.lookupOn)
            {
                InfoPanels.UpdateSelectedPin();
            }

            UpdateAll();
            MapText.UpdateAll();
        }

        private static void UpdateSize(Button sender)
        {
            string text = $"Pin Size:\n";

            switch (APMapMod.GS.pinSize)
            {
                case PinSize.Small:
                    text += "small";
                    break;

                case PinSize.Medium:
                    text += "medium";
                    break;

                case PinSize.Large:
                    text += "large";
                    break;
            }

            sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
            sender.Content = text;
        }

        public static void ToggleMode(Button sender)
        {
            if (GameManager.instance.gameMap != null
                && (APMapMod.LS.mapMode == MapMode.TransitionRando
                    || APMapMod.LS.mapMode == MapMode.TransitionRandoAlt))
            {
                MapRooms.ResetMapColors(GameManager.instance.gameMap);
            }

            APMapMod.LS.ToggleMapMode();

            UpdateAll();
            MapText.UpdateAll();
            ControlPanel.UpdateAll();
            MapKey.UpdateAll();
            InfoPanels.selectedScene = "None";
            TransitionPersistent.ResetRoute();
            //RouteCompass.UpdateCompass();
        }

        private static void UpdateMode(Button sender)
        {
            string text = $"Mode:\n";

            switch (APMapMod.LS.mapMode)
            {
                case MapMode.FullMap:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                    text += "Full Map";
                    break;

                case MapMode.AllPins:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                    text += "All Pins";
                    break;

                case MapMode.PinsOverMap:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                    text += "Pins Over Map";
                    break;

                case MapMode.TransitionRando:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    text += "Transition";
                    break;

                case MapMode.TransitionRandoAlt:
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    text += "Transition 2";
                    break;
            }

            sender.Content = text;
        }

        public static void CollapsePanel()
        {
            panelActive = false;

            UpdateAll();
        }

        public static void ToggleCustomizePins(Button sender)
        {
            panelActive = !panelActive;

            UpdateAll();
        }

        private static void UpdateCustomizePins(Button sender)
        {
            if (APMapMod.LS.modEnabled && panelActive)
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
            }
            else
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
            }

            sender.Content = $"Customize\nPins";
        }

        public static void TogglePool(Button sender)
        {
            if (sender.Name == "Benches(Vanilla)")
            {
                if (!PlayerData.instance.GetBool("hasPinBench")) return;

                APMapMod.LS.ToggleBench();
            }
            else
            {
                APMapMod.LS.TogglePoolGroupSetting(sender.Name);

                WorldMap.CustomPins.GetRandomizedOthersGroups();

                UpdateAll();
                MapText.UpdateAll();
            }
        }

        private static void UpdatePool(Button sender)
        {
            if (WorldMap.CustomPins == null) return;

            if (sender.Name == "Geo Rocks" && !AP.Instance.SlotOptions.RandomizeGeoRocks)
            {
                sender.Content = $"Geo Rocks:\n" + APMapMod.LS.geoRockCounter + " / " + "207";
            }

            if (sender.Name == "Benches(Vanilla)")
            {
                if (PlayerData.instance.GetBool("hasPinBench"))
                {
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                }
                else
                {
                    sender.ContentColor = Colors.GetColor(ColorSetting.UI_Disabled);
                }
            }
            else
            {
                switch (APMapMod.LS.GetPoolGroupSetting(sender.Name))
                {
                    case PoolGroupState.Off:
                        sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                        break;
                    case PoolGroupState.On:
                        sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                        break;
                    case PoolGroupState.Mixed:
                        sender.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
                        break;
                }
            }
        }

        public static void TogglePersistent(Button sender)
        {
            APMapMod.GS.TogglePersistentOn();

            UpdateAll();
        }

        private static void UpdatePersistent(Button sender)
        {
            if (APMapMod.GS.persistentOn)
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                sender.Content = $"Persistent\nitems: On";
            }
            else
            {
                sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                sender.Content = $"Persistent\nitems: Off";
            }
        }

        public static void ToggleGroupBy(Button sender)
        {
            APMapMod.LS.ToggleGroupBy();

            WorldMap.CustomPins.GetRandomizedOthersGroups();
            WorldMap.CustomPins.ResetPoolSettings();

            UpdateAll();
        }

        private static void UpdateGroupBy(Button sender)
        {
            switch (APMapMod.LS.groupBy)
            {
                case GroupBy.Location:
                    sender.Content = $"Group by:\nLocation";
                    break;

                case GroupBy.Item:
                    sender.Content = $"Group by:\nItem";
                    break;
            }

            sender.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
        }
    }
}