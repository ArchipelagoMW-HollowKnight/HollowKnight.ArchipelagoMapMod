﻿using System;
using System.Collections.Generic;
using APMapMod.Data;
using APMapMod.Map;
using APMapMod.Settings;
using MagicUI.Core;
using MagicUI.Elements;

namespace APMapMod.UI
{
    internal class MapText
    {
        private static LayoutRoot layout;

        private static TextObject refresh;

        private static readonly Dictionary<string, Tuple<Padding, Action<TextObject>>> _textObjects = new()
        {
            { "Icon Visibility", new(new(10f, 10f, 1200f, 20f), UpdateIconVisibility) },
            { "Randomized", new(new(10f, 10f, 500f, 20f), UpdateRandomized) },
            { "Others", new(new(10f, 10f, 10f, 20f), UpdateOthers) },
            { "Style", new(new(500f, 10f, 10f, 20f), UpdateStyle) },
            { "Size", new(new(1200f, 10f , 10f, 20f), UpdateSize) },
        };

        public static bool Condition()
        {
            return GUI.worldMapOpen || GUI.quickMapOpen;
        }

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Map Text");
                layout.VisibilityCondition = Condition;

                foreach (string textName in _textObjects.Keys)
                {
                    TextObject textObj = new(layout, textName)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Font = MagicUI.Core.UI.TrajanNormal,
                        FontSize = 16,
                        Padding = _textObjects[textName].Item1
                    };
                }

                refresh = new(layout, "Refresh")
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Font = MagicUI.Core.UI.TrajanNormal,
                    FontSize = 16,
                    Padding = new(10f, 10f, 10f, 20f)
                };

                UpdateAll();
            }
        }

        public static void Destroy()
        {
            layout?.Destroy();
            layout = null;
        }

        public static void UpdateAll()
        {
            foreach (string textName in _textObjects.Keys)
            {
                TextObject textObj = (TextObject)layout.GetElement(textName);

                _textObjects[textName].Item2.Invoke(textObj);

                if (APMapMod.LS.modEnabled)
                {
                    textObj.Visibility = Visibility.Visible;
                }
                else
                {
                    textObj.Visibility = Visibility.Hidden;
                }
            }

            refresh.Visibility = Visibility.Hidden;
        }

        public static void SetToRefresh()
        {
            foreach (string text in _textObjects.Keys)
            {
                TextObject textObj = (TextObject)layout.GetElement(text);

                textObj.Visibility = Visibility.Hidden;
            }

            refresh.Visibility = Visibility.Visible;

            if (APMapMod.LS.modEnabled)
            {
                refresh.Text = "APMapMod enabled. Close map to refresh";
            }
            else
            {
                refresh.Text = "APMapMod disabled. Close map to refresh";
            }
        }

        private static void UpdateIconVisibility(TextObject textObj)
        {
            string text = $"Player Icons (ctrl-1): ";

            
            switch (APMapMod.LS.IconVisibility)
            {
                case IconVisibility.Both:
                    textObj.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                    text += "Both";
                    break;
                
                case IconVisibility.Own:
                    textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    text += "Own";
                    break;
                
                case IconVisibility.Others:
                    textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Special);
                    text += $"Others";
                    break;
                
                case IconVisibility.None:
                    textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Disabled);
                    text += $"None";
                    break;
            }

            textObj.Text = text;
        }

        private static void UpdateRandomized(TextObject textObj)
        {
            string text = $"Randomized (ctrl-2): ";

            if (APMapMod.LS.randomizedOn)
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                text += "on";
            }
            else
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                text += "off";
            }

            if (WorldMap.CustomPins.IsRandomizedCustom())
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
            }

            textObj.Text = text;
        }

        private static void UpdateOthers(TextObject textObj)
        {
            string text = $"Others (ctrl-3): ";

            if (APMapMod.LS.othersOn)
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_On);
                text += "on";
            }
            else
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
                text += "off";
            }

            if (WorldMap.CustomPins.IsOthersCustom())
            {
                textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Custom);
            }

            textObj.Text = text;
        }

        private static void UpdateStyle(TextObject textObj)
        {
            string text = $"Style (ctrl-4): ";

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

            textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
            textObj.Text = text;
        }

        private static void UpdateSize(TextObject textObj)
        {
            string text = $"Size (ctrl-5): ";

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

            textObj.ContentColor = Colors.GetColor(ColorSetting.UI_Neutral);
            textObj.Text = text;
        }
    }
}