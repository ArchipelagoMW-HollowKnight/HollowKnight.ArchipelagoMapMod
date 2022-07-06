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
            new MenuButton("Random", "", _ => {
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
                0,
                255,
                true
                ),
            new CustomSlider(
                "Green",
                g =>
                {
                    APMapMod.GS.IconColorG = Mathf.RoundToInt(g);
                    sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorG,
                0,
                255,
                true
            ),
            new CustomSlider(
                "Blue",
                b =>
                {
                    APMapMod.GS.IconColorB = Mathf.RoundToInt(b);
                    sr.color = APMapMod.GS.IconColor;
                },
                () => APMapMod.GS.IconColorB,
                0,
                255,
                true
            ),
            new StaticPanel(
                "preview icon",
                CreateIcon,
                100f
            ),
        });
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
        sr = knightIcon.AddComponent<Image>();
        sr.sprite = compassIcon;
        sr.color = APMapMod.GS.IconColor;
        knightIcon.transform.localPosition = new Vector3(0, -tex.height/2f, 0);
        knightIcon.layer = 27; // uGUI layer
        knightIcon.SetScale(1,1);
    }
}