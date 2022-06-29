using System.Linq;
using System.Threading;
using APMapMod.Data;
using APMapMod.Map;
using Archipelago.HollowKnight.IC;
using ItemChanger;
using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;

namespace APMapMod.UI
{
    internal class InfoPanels
    {
        private static LayoutRoot layout;

        private static StackLayout stackLayout;

        private static Panel lookupPanel;
        private static TextObject lookupPanelText;

        private static string selectedLocation = "None selected";

        private static Panel uncheckedPanel;
        private static TextObject uncheckedPanelText;

        public static string selectedScene = "None";

        public static bool Condition()
        {
            return GUI.worldMapOpen
                && APMapMod.LS.modEnabled
                && !GUI.lockToggleEnable;
        }

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Lookup");
                layout.VisibilityCondition = Condition;

                stackLayout = new(layout, "Info Panels")
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Spacing = 10f,
                    Padding = new(10f, 170f, 160f, 10f)
                };

                lookupPanel = new(layout, GUIController.Instance.Images["panelRight"].ToSlicedSprite(100f, 50f, 200f, 50f), "Lookup Panel")
                {
                    Borders = new(30f, 30f, 30f, 30f),
                    MinWidth = 400f,
                    MinHeight = 100f,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    //Padding = new(10f, 170f, 160f, 10f)
                };

                ((Image)layout.GetElement("Lookup Panel Background")).Tint = Colors.GetColor(ColorSetting.UI_Borders);

                lookupPanelText = new(layout, "Lookup Panel Text")
                {
                    ContentColor = Colors.GetColor(ColorSetting.UI_Neutral),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextAlignment = HorizontalAlignment.Left,
                    Font = MagicUI.Core.UI.Perpetua,
                    FontSize = 20,
                    MaxWidth = 450f
                };

                lookupPanel.Child = lookupPanelText;

                stackLayout.Children.Add(lookupPanel);

                selectedLocation = "None selected";

                uncheckedPanel = new(layout, GUIController.Instance.Images["panelRight"].ToSlicedSprite(100f, 50f, 250f, 50f), "Unchecked Panel")
                {
                    Borders = new(30f, 30f, 30f, 30f),
                    MinWidth = 200f,
                    MinHeight = 100f,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    //Padding = new(10f, 170f, 160f, 10f)
                };

                ((Image)layout.GetElement("Unchecked Panel Background")).Tint = Colors.GetColor(ColorSetting.UI_Borders);

                uncheckedPanelText = new(layout, "Unchecked Panel Text")
                {
                    ContentColor = Colors.GetColor(ColorSetting.UI_Neutral),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Font = MagicUI.Core.UI.TrajanNormal,
                    FontSize = 14
                };

                uncheckedPanel.Child = uncheckedPanelText;

                stackLayout.Children.Add(uncheckedPanel);

                selectedScene = "None";
            }
        }

        public static void Destroy()
        {
            layout?.Destroy();
            layout = null;
        }

        public static void UpdateAll()
        {
            UpdateLookupPanel();
            UpdateUncheckedPanel();
        }

        public static void UpdateLookupPanel()
        {
            if (APMapMod.GS.lookupOn)
            {
                string text = $"{Utils.ToCleanName(selectedLocation)}";
                PinDef pd = MainData.GetUsedPinDef(selectedLocation);
                
                if (pd != null)
                {
                    text += $"\n\nRoom: {pd.sceneName}";

                    text += $"\n\nStatus:";
                    text += pd.pinLocationState switch
                    {
                        // PinLocationState.UncheckedUnreachable => $" Randomized, unchecked, unreachable",
                        // PinLocationState.UncheckedReachable => $" Randomized, unchecked, reachable",
                        // PinLocationState.NonRandomizedUnchecked => $" Not randomized, either unchecked or persistent",
                        // PinLocationState.OutOfLogicReachable => $" Randomized, unchecked, reachable through sequence break",
                        // PinLocationState.Previewed => $" Randomized, previewed",
                        // PinLocationState.Cleared => $" Cleared",
                        // PinLocationState.ClearedPersistent => $" Randomized, cleared, persistent",
                        
                        PinLocationState.UncheckedUnreachable => $" unchecked",
                        PinLocationState.UncheckedReachable => $" unchecked",
                        PinLocationState.NonRandomizedUnchecked => $" Not randomized, either unchecked or persistent",
                        PinLocationState.OutOfLogicReachable => $" unchecked, reachable through sequence break",
                        PinLocationState.Previewed => $" previewed",
                        PinLocationState.Cleared => $" Cleared",
                        PinLocationState.ClearedPersistent => $" cleared, persistent",
                        _ => ""
                    };

                    if (MainData.IsInLogicLookup(selectedLocation))
                    {
                        text += $"\n\nLogic: {MainData.GetRawLogic(selectedLocation)}";
                    }

                    if (pd.placement.Visited.HasFlag(VisitState.Previewed) || pd.placement.Items.Any(i => i.GetTag<ArchipelagoItemTag>().Hinted))
                    {
                        text += $"\n\nPreviewed item(s):\n";

                        string[] previewText = MainData.GetPreviewText(pd.name);

                        if (previewText == null) return;

                        for (var i = 0; i < previewText.Length; i++)
                        {
                            if(pd.placement.Items[i].GetTag<ArchipelagoItemTag>().Hinted || pd.placement.Visited.HasFlag(VisitState.Previewed))
                                text += $" {Utils.ToCleanPreviewText(previewText[i])}\n";
                        }

                        text = text.Substring(0, text.Length - 1);
                    }
                }

                lookupPanelText.Text = text;
                lookupPanel.Visibility = Visibility.Visible;
            }
            else
            {
                lookupPanel.Visibility = Visibility.Collapsed;
            }
        }

        // Called every 0.1 seconds
        public static void UpdateSelectedPinCoroutine()
        {
            if (!GUI.worldMapOpen
                || WorldMap.goCustomPins == null
                || WorldMap.CustomPins == null
                || GameManager.instance.IsGamePaused()
                || !Condition()
                || !APMapMod.GS.lookupOn)
            {
                return;
            }

            if (WorldMap.CustomPins.GetPinClosestToMiddle(selectedLocation, out selectedLocation))
            {
                UpdateSelectedPin();
            }
        }

        public static void UpdateSelectedPin()
        {
            if (!GUI.worldMapOpen
                || WorldMap.goCustomPins == null
                || WorldMap.CustomPins == null) return;

            WorldMap.CustomPins.ResizePins(selectedLocation);
            UpdateAll();
        }

        private static Thread colorUpdateThread;

        // Called every 0.1 seconds
        public static void UpdateSelectedScene()
        {
            if (layout == null
                || GUI.lockToggleEnable
                || GameManager.instance.IsGamePaused()
                || !TransitionData.TransitionModeActive())
            {
                return;
            }

            if (GUI.worldMapOpen)
            {
                if (colorUpdateThread != null && colorUpdateThread.IsAlive) return;

                colorUpdateThread = new(() =>
                {
                    if (MapRooms.GetRoomClosestToMiddle(selectedScene, out selectedScene))
                    {
                        MapRooms.SetSelectedRoomColor(selectedScene, true);
                        TransitionPersistent.UpdateAll();
                        TransitionWorldMap.UpdateAll();
                        UpdateAll();
                    }
                });

                colorUpdateThread.Start();
            }
            else if (GUI.quickMapOpen)
            {
                MapRooms.SetSelectedRoomColor("", true);
            }
        }

        public static void UpdateUncheckedPanel()
        {
            if (TransitionData.TransitionModeActive() && APMapMod.GS.uncheckedPanelActive)
            {
                uncheckedPanelText.Text = selectedScene + "\n\n";
                uncheckedPanelText.Text += TransitionData.GetUncheckedVisited(selectedScene);
                uncheckedPanel.Visibility = Visibility.Visible;
            }
            else
            {
                uncheckedPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
