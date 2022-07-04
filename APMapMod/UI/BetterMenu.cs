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
    private static Menu _menuRef;
    private static Image _sr;
    private static bool _update;

    public static MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        _menuRef ??= PrepareMenu();
        _menuRef.OnBuilt += BuildUpdate;
        return _menuRef.GetMenuScreen(modListMenu);
    }

    private static Menu PrepareMenu()
    {
        return new Menu("Archipelago Map Mod", new Element[]
        {
            new TextPanel("Enter the Red Green and Blue values for your icon color", 800f),
            new MenuButton("Random", "", _ => {
                APMapMod.GS.IconColor = Utils.GetRandomLightColor();
                _sr.color = APMapMod.GS.IconColor;
                _menuRef.Update();
            }),
            new CustomSlider(
                "Red",
                r =>
                {
                    APMapMod.GS.IconColorR = _update ? Mathf.RoundToInt(r) : APMapMod.GS.IconColorR;
                    _sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorR
            )
            {
                minValue = 0,
                maxValue = 255,
                wholeNumbers = true
            },
            new CustomSlider(
                "Green",
                g =>
                {
                    APMapMod.GS.IconColorG = _update ? Mathf.RoundToInt(g) : APMapMod.GS.IconColorG;
                    _sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorG
            )
            {
                minValue = 0,
                maxValue = 255,
                wholeNumbers = true
            },
            new CustomSlider(
                "Blue",
                b =>
                {
                    APMapMod.GS.IconColorB = _update ? Mathf.RoundToInt(b) : APMapMod.GS.IconColorB;
                    _sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorB
            )
            {
                minValue = 0,
                maxValue = 255,
                wholeNumbers = true
            },
            new StaticPanel(
                "preview icon",
                CreateIcon,
                100f
            ),
    });
    }

    private static void BuildUpdate(object sender, ContainerBuiltEventArgs containerBuiltEventArgs)
    {
        _menuRef.Update();
        _update = true;
    }

    private static void CreateIcon(GameObject go)
    {
        var knightIcon = new GameObject("Knight Icon")
        {
            transform =
            {
                parent = go.transform
            }
        };

        var tex = GUIController.Instance.Images["CompassIcon"];
        var compassIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 55);
        knightIcon.AddComponent<CanvasRenderer>();
        _sr = knightIcon.AddComponent<Image>();
        _sr.sprite = compassIcon;
        _sr.color = APMapMod.GS.IconColor;
        knightIcon.transform.localPosition = new Vector3(0, -tex.height/2f, 0);
        knightIcon.layer = 27; // uGUI layer
        knightIcon.SetScale(1,1);
    }
}