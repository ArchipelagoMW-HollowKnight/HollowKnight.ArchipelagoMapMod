using System.Collections.Generic;
using System.Linq;
using APMapMod.Data;
using InControl;
using MagicUI.Core;
using MagicUI.Elements;

namespace APMapMod.UI
{
    internal class TransitionWorldMap
    {
        private static LayoutRoot layout;

        private static TextObject instruction;
        private static TextObject routeSummary;

        private static bool Condition()
        {
            return TransitionData.TransitionModeActive()
                && !GUI.lockToggleEnable
                && GUI.worldMapOpen;
        }

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Transition World Map");
                layout.VisibilityCondition = Condition;

                instruction = UIExtensions.TextFromEdge(layout, "Instructions", false);

                routeSummary = UIExtensions.TextFromEdge(layout, "Route Summary", true);

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
            UpdateInstructions();
            UpdateRouteSummary();
        }

        public static void UpdateInstructions()
        {
            string text = "";

            if (!APMapMod.GS.uncheckedPanelActive)
            {
                text += $"Selected room: {InfoPanels.selectedScene}.";
            }

            List<BindingSource> bindings = new(InputHandler.Instance.inputActions.menuSubmit.Bindings);

            if (InfoPanels.selectedScene == Utils.CurrentScene())
            {
                text += $" You are here.";
            }

            text += $" Press ";

            text += Utils.GetBindingsText(bindings);

            if (TransitionPersistent.selectedRoute.Any()
                && InfoPanels.selectedScene == TransitionPersistent.lastFinalScene
                && TransitionPersistent.selectedRoute.Count() == TransitionPersistent.transitionsCount)
            {
                text += $" to change starting / final transitions of current route.";
            }
            else
            {
                text += $" to find a new route.";
            }


            // if (TransitionPersistent.selectedRoute.Any() && TransitionPersistent.selectedRoute.First().IsBenchwarpTransition() && Dependencies.HasBenchwarp())
            // {
            //     bindings = new(InputHandler.Instance.inputActions.attack.Bindings);
            //
            //     text += $" Hold ";
            //
            //     text += Utils.GetBindingsText(bindings);
            //
            //     text += $" to benchwarp.";
            // }

            instruction.Text = text;
        }

        public static void UpdateRouteSummary()
        {
            string text = $"Current route: ";

            // if (TransitionPersistent.lastStartTransition != ""
            //     && TransitionPersistent.lastFinalTransition != ""
            //     && TransitionPersistent.transitionsCount > 0
            //     && TransitionPersistent.selectedRoute.Any())
            // {
            //     if (TransitionPersistent.lastFinalTransition.IsSpecialTransition())
            //     {
            //         if (TransitionPersistent.transitionsCount == 1)
            //         {
            //             text += $"{TransitionPersistent.lastStartTransition.ToCleanName()}";
            //         }
            //         else
            //         {
            //             text += $"{TransitionPersistent.lastStartTransition.ToCleanName()} ->...-> {TransitionPersistent.lastFinalTransition.ToCleanName()}";
            //         }
            //     }
            //     else
            //     {
            //         text += $"{TransitionPersistent.lastStartTransition.ToCleanName()} ->...-> {TransitionPersistent.lastFinalTransition.GetAdjacentTerm().ToCleanName()}";
            //     }
            //     
            //     text += $"\n\nTransitions: {TransitionPersistent.transitionsCount}";
            // }
            // else
            // {
            //     text += "None";
            // }

            routeSummary.Text = text;
        }


    }
}
