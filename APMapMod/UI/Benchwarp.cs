using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using APMapMod.Data;
using APMapMod.Map;
using InControl;
using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;

namespace APMapMod.UI
{
    internal class Benchwarp
    {
        private static LayoutRoot layout;

        // Only for normal modes (not transition mode)
        private static TextObject benchwarpText;
        public static string selectedBenchScene = "";
        private static int benchPointer = 0;

        private static bool Condition()
        {
            return APMapMod.LS.modEnabled
                && !TransitionData.TransitionModeActive()
                && !GUI.lockToggleEnable
                && GUI.worldMapOpen
                && APMapMod.GS.benchwarpWorldMap;
        }

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Benchwarp Layout");
                layout.VisibilityCondition = Condition;

                benchwarpText = UIExtensions.TextFromEdge(layout, "Benchwarp Text", false);

                UpdateAll();
            }
        }

        public static void Destroy()
        {
            layout?.Destroy();
            layout = null;

            ResetBenchSelection();
        }

        public static void ResetBenchSelection()
        {
            selectedBenchScene = "";
            benchPointer = 0;
            attackHoldTimer.Reset();
        }

        public static void UpdateAll()
        {
            if (Dependencies.HasBenchwarp() && !TransitionData.TransitionModeActive())
            {
                if (!APMapMod.GS.benchwarpWorldMap)
                {
                    ResetBenchSelection();
                }

                BenchwarpInterop.UpdateVisitedBenches();
                UpdateBenchwarpText();
                //MapRooms.SetSelectedRoomColor(selectedBenchScene, false);
            }
        }

        public static void UpdateBenchwarpText()
        {
            string text = "";

            if (Dependencies.HasBenchwarp() && selectedBenchScene != "")
            {
                List<BindingSource> bindings = new(InputHandler.Instance.inputActions.attack.Bindings);

                text += $"Hold ";

                text += Utils.GetBindingsText(bindings);

                text += $" to warp to {GetSelectedBench().benchName.Replace("Warp ", "").Replace("Bench ", "")}.";

                if (BenchwarpInterop.benches.ContainsKey(selectedBenchScene) && BenchwarpInterop.benches[selectedBenchScene].Count > 1)
                {
                    text += $"\nTap ";

                    text += Utils.GetBindingsText(bindings);

                    text += $" to toggle to another bench here.";
                }
            }

            benchwarpText.Text = text;
        }

        private static Thread benchUpdateThread;

        // Called every 0.1 seconds
        public static void UpdateSelectedBenchCoroutine()
        {
            if (layout == null
                || !APMapMod.LS.modEnabled
                || TransitionData.TransitionModeActive()
                || GUI.lockToggleEnable
                || !Dependencies.HasBenchwarp()
                || GameManager.instance.IsGamePaused())
            {
                return;
            }

            if (GUI.worldMapOpen && APMapMod.GS.benchwarpWorldMap)
            {
                if (benchUpdateThread != null && benchUpdateThread.IsAlive) return;

                benchUpdateThread = new(() =>
                {
                    if (GetBenchClosestToMiddle(selectedBenchScene, out selectedBenchScene))
                    {
                        benchPointer = 0;
                        MapRooms.SetSelectedRoomColor(selectedBenchScene, false);
                        UpdateBenchwarpText();
                    }
                });

                benchUpdateThread.Start();
            }
            else if (GUI.worldMapOpen || GUI.quickMapOpen)
            {
                MapRooms.SetSelectedRoomColor("", false);
            }
        }

        public static bool GetBenchClosestToMiddle(string previousScene, out string selectedScene)
        {
            selectedScene = "";
            double minDistance = double.PositiveInfinity;
            GameObject go_GameMap = GameManager.instance.gameMap;
            if (go_GameMap == null) return false;
            foreach (Transform areaObj in go_GameMap.transform)
            {
                foreach (Transform roomObj in areaObj.transform)
                {
                    if (!roomObj.gameObject.activeSelf || !BenchwarpInterop.benches.ContainsKey(roomObj.name)) continue;
                    ExtraMapData emd = roomObj.GetComponent<ExtraMapData>();
                    if (emd == null) continue;

                    double distance = Utils.DistanceToMiddle(roomObj);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        selectedScene = emd.sceneName;
                    }
                }
            }

            return selectedScene != previousScene;
        }

        public static Stopwatch attackHoldTimer = new();

        // Called every frame
        public static void Update()
        {
            if (!APMapMod.LS.modEnabled
                || !GUI.worldMapOpen
                || GUI.lockToggleEnable
                || !Dependencies.HasBenchwarp()
                || GameManager.instance.IsGamePaused()
                || InputHandler.Instance == null)
            {
                return;
            }

            // Hold attack to benchwarp
            if (InputHandler.Instance.inputActions.attack.WasPressed)
            {
                attackHoldTimer.Restart();
            }

            if (InputHandler.Instance.inputActions.attack.WasReleased)
            {
                if (!TransitionData.TransitionModeActive()
                    && APMapMod.GS.benchwarpWorldMap
                    && attackHoldTimer.ElapsedMilliseconds < 500)
                {
                    ToggleBench();
                    UpdateBenchwarpText();
                }

                attackHoldTimer.Reset();
            }

            if (attackHoldTimer.ElapsedMilliseconds >= 500)
            {
                if (TransitionData.TransitionModeActive())
                {
                    // if (TransitionPersistent.selectedRoute.Any() && TransitionPersistent.selectedRoute.First().IsBenchwarpTransition())
                    // {
                    //     attackHoldTimer.Reset();
                    //     GameManager.instance.StartCoroutine(BenchwarpInterop.DoBenchwarp(TransitionPersistent.selectedRoute.First()));
                    //     return;
                    // }
                    //
                    // attackHoldTimer.Reset();
                }
                else if (APMapMod.GS.benchwarpWorldMap)
                {
                    if (selectedBenchScene != "")
                    {
                        attackHoldTimer.Reset();
                        GameManager.instance.StartCoroutine(BenchwarpInterop.DoBenchwarp(selectedBenchScene, benchPointer));
                        return;
                    }

                    attackHoldTimer.Reset();
                }
            }
        }

        private static void ToggleBench()
        {
            if (!BenchwarpInterop.benches.ContainsKey(selectedBenchScene)
                || benchPointer > BenchwarpInterop.benches[selectedBenchScene].Count - 1)
            {
                APMapMod.Instance.LogWarn("Invalid bench toggle");
                return;
            }

            benchPointer = (benchPointer + 1) % BenchwarpInterop.benches[selectedBenchScene].Count;
        }

        private static WorldMapBenchDef GetSelectedBench()
        {
            if (!BenchwarpInterop.benches.ContainsKey(selectedBenchScene)
                || benchPointer > BenchwarpInterop.benches[selectedBenchScene].Count - 1)
            {
                APMapMod.Instance.LogWarn("Invalid bench selection");
                return BenchwarpInterop.benches.First().Value.First();
            }

            return BenchwarpInterop.benches[selectedBenchScene][benchPointer];
        }
    }
}
