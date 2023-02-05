using System;
using Modding;
using Satchel;
using Satchel.BetterMenus;
using UnityEngine;
using UnityEngine.UI;
using MenuButton = Satchel.BetterMenus.MenuButton;
using Utils = APMapMod.Data.Utils;

namespace APMapMod.UI;

internal static class BetterMenu
{
    private static Menu menuRef;
    private static Image sr;

    public static MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        menuRef ??= PrepareMenu();
        return menuRef.GetMenuScreen(modListMenu);
    }

    private static Menu PrepareMenu()
    {
        return new Menu("Archipelago Map Mod", new Element[]
        {
            new TextPanel("Enter the Red Green and Blue values for your icon color", 800f),
            new MenuButton("Random", "", _ =>
            {
                APMapMod.GS.IconColor = Utils.GetRandomLightColor();
                sr.color = APMapMod.GS.IconColor;
                menuRef.Update();
            }),
            new CustomSlider(
                "Red",
                r =>
                {
                    APMapMod.GS.IconColorR = Mathf.RoundToInt(r);
                    sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorR,
                minValue: 0, maxValue: 255, wholeNumbers: true
            ),
            new CustomSlider(
                "Green",
                g =>
                {
                    APMapMod.GS.IconColorG = Mathf.RoundToInt(g);
                    sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorG,
                minValue: 0, maxValue: 255, wholeNumbers: true
            ),
            new CustomSlider(
                "Blue",
                b =>
                {
                    APMapMod.GS.IconColorB = Mathf.RoundToInt(b);
                    sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorB,
                minValue: 0, maxValue: 255, wholeNumbers: true
            ),
            new StaticPanel(
                "preview icon",
                CreateIcon,
                100f
            ),
            new CustomSlider(
                "Gameplay Hints shown",
                b =>
                {
                    //APMapMod.Instance.Log($"Setting gameplay hints to {b} from satchel");
                    APMapMod.GS.gameplayHints = Mathf.RoundToInt(b);
                    HintDisplay.UpdateDisplay();
                },
                () => APMapMod.GS.gameplayHints,
                minValue: 0, maxValue: 20, wholeNumbers: true
            ),
            new CustomSlider(
                "Pause Menu Hints Shown",
                b =>
                {
                    //APMapMod.Instance.Log($"Setting pause menu hints to {b} from satchel");
                    APMapMod.GS.pauseMenuHints = Mathf.RoundToInt(b);
                    HintDisplay.UpdateDisplay();
                },
                () => APMapMod.GS.pauseMenuHints,
                minValue: 0, maxValue: 20, wholeNumbers: true
            ),
            new CustomSlider(
                "Hint Text Size",
                b =>
                {
                    //APMapMod.Instance.Log($"Setting hint size to {b} from satchel");
                    APMapMod.GS.hintFontSize = Mathf.RoundToInt(b);
                    HintDisplay.UpdateDisplay();
                },
                () => APMapMod.GS.hintFontSize,
                minValue: 10, maxValue: 50, wholeNumbers: true
            ),
        });
    }

    private static void CreateIcon(GameObject go)
    {
        var knightIcon = new GameObject("APKnight Icon")
        {
            transform =
            {
                parent = go.transform
            }
        };

        var tex = GUIController.Instance.Images["CompassIcon"];
        var compassIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 55);
        knightIcon.AddComponent<CanvasRenderer>();
        sr = knightIcon.AddComponent<Image>();
        sr.sprite = compassIcon;
        sr.color = APMapMod.GS.IconColor;
        knightIcon.transform.localPosition = new Vector3(0, -tex.height / 2f, 0);
        knightIcon.layer = 27; // uGUI layer
        knightIcon.SetScale(1, 1);
    }
}