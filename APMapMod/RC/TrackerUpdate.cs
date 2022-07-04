using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.HollowKnight.IC;
using ItemChanger;
using ItemChanger.Modules;

namespace APMapMod.RC;

    public class TrackerUpdate : Module
    {
        public override void Initialize()
        {
            APMapMod.Instance.LogDebug("TrackerUpdate Init");
            AbstractItem.AfterGiveGlobal += AfterRandoItemGive;
            //RandoItemTag.AfterRandoItemGive += AfterRandoItemGive;
            //RandoPlacementTag.OnRandoPlacementVisitStateChanged += OnRandoPlacementVisitStateChanged;
            Events.OnTransitionOverride += OnTransitionOverride;
            transitionLookup ??= TD.ctx?.transitionPlacements?.ToDictionary(p => p.Source.Name, p => p.Target.Name) ?? new Dictionary<string, string>();
            
            OnItemObtained += TD.OnItemObtained;
            OnItemObtained += TD_WSB.OnItemObtained;
            OnPlacementPreviewed += TD.OnPlacementPreviewed;
            OnPlacementPreviewed += TD_WSB.OnPlacementPreviewed;
            OnPlacementCleared += TD.OnPlacementCleared;
            OnPlacementCleared += TD_WSB.OnPlacementCleared;
            OnTransitionVisited += TD.OnTransitionVisited;
            OnTransitionVisited += TD_WSB.OnTransitionVisited;
        }

        public override void Unload()
        {
            APMapMod.Instance.LogDebug("TrackerUpdate Unload");
            AbstractItem.AfterGiveGlobal -= AfterRandoItemGive;
            //RandoItemTag.AfterRandoItemGive -= AfterRandoItemGive;
            //RandoPlacementTag.OnRandoPlacementVisitStateChanged -= OnRandoPlacementVisitStateChanged;
            Events.OnTransitionOverride -= OnTransitionOverride;

            OnItemObtained -= TD.OnItemObtained;
            OnItemObtained -= TD_WSB.OnItemObtained;
            OnPlacementPreviewed -= TD.OnPlacementPreviewed;
            OnPlacementPreviewed -= TD_WSB.OnPlacementPreviewed;
            OnPlacementCleared -= TD.OnPlacementCleared;
            OnPlacementCleared -= TD_WSB.OnPlacementCleared;
            OnTransitionVisited -= TD.OnTransitionVisited;
            OnTransitionVisited -= TD_WSB.OnTransitionVisited;
        }

        public static event Action<string> OnPlacementPreviewed;
        public static event Action<string> OnPlacementCleared;
        public static event Action<int, string, string> OnItemObtained;
        public static event Action<string, string> OnTransitionVisited;
        public static event Action OnFinishedUpdate;

        private TrackerData TD => APMapMod.LS.tracker;
        private TrackerData TD_WSB => APMapMod.LS.trackerWithoutSequenceBreaks;
        private Dictionary<string, string> transitionLookup;

        private void OnRandoPlacementVisitStateChanged(VisitStateChangedEventArgs args)
        {
            if ((args.NewFlags & VisitState.Previewed) == VisitState.Previewed)
            {
                OnPlacementPreviewed?.Invoke(args.Placement.Name);
                OnFinishedUpdate?.Invoke();
            }
        }

        private void AfterRandoItemGive(ReadOnlyGiveEventArgs args)
        {
            // dont do anything for other players items.
            if (args.Orig.GetTag<ArchipelagoItemTag>()?.Player != APMapMod.Instance.session.ConnectionInfo.Slot) return;
            
            string itemName = args.Orig.name; // the name of the item that was given (not necessarily the item placed)
            string placementName = args.Placement.Name;

            if (args.Orig.GetTag(out ArchipelagoDummyItem _))
                return;

            var id = args.Orig.GetTag<APMapItemTag>().id;

            APMapMod.Instance.LogDebug($"item obtained {id}, {itemName}, {placementName}");
            OnItemObtained?.Invoke(id, itemName, placementName);
            
            if (args.Placement.Items.Any(item => !item.WasEverObtained()))
            {
                OnPlacementCleared?.Invoke(placementName);
            }

            OnFinishedUpdate?.Invoke();
        }

        /// <summary>
        /// Static method intended to allow updating visited source transitions by external callers.
        /// </summary>
        public static void SendTransitionFound(Transition source)
        {
            if (ItemChangerMod.Modules.Get<TrackerUpdate>() is TrackerUpdate instance) instance.OnTransitionFound(source.ToString());
        }

        private void OnTransitionOverride(Transition source, Transition origTarget, ITransition newTarget)
        {
            OnTransitionFound(source.ToString());
        }

        private void OnTransitionFound(string sourceName)
        {
            if (transitionLookup.TryGetValue(sourceName, out string targetName) && !TD.HasVisited(sourceName))
            {
                OnTransitionVisited?.Invoke(sourceName, targetName);
                //if (RandomizerMod.RS.GenerationSettings.TransitionSettings.Coupled && transitionLookup.ContainsKey(targetName))
                if (transitionLookup.ContainsKey(targetName))
                {
                    OnTransitionVisited?.Invoke(targetName, sourceName);
                }

                OnFinishedUpdate?.Invoke();
            }
        }

    }