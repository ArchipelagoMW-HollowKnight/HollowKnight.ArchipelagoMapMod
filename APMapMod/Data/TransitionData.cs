﻿using System.Collections.Generic;
using System.Linq;
using APMapMod.RC;
using RandomizerCore;
using RandomizerCore.Logic;

namespace APMapMod.Data
{
    public static class TransitionData
    {
        private static APRandoContext Ctx => APMapMod.LS.Context;
         private static LogicManager Lm => Ctx?.LM;
    
        private static HashSet<string> _randomizedTransitions = new();
        private static Dictionary<string, TransitionPlacement> _transitionLookup = new();
        private static Dictionary<string, HashSet<string>> _transitionsByScene = new();
    
        public static bool IsTransitionRando()
        {
            return false;
            // return RM.RS.GenerationSettings.TransitionSettings.Mode != TM.None
            //     || (APMapMod.LS.Context.transitionPlacements != null && APMapMod.LS.Context.transitionPlacements.Any());
        }
    
        public static bool TransitionModeActive()
        {
            return APMapMod.LS.modEnabled
                   && (APMapMod.LS.mapMode == Settings.MapMode.TransitionRando
                       || APMapMod.LS.mapMode == Settings.MapMode.TransitionRandoAlt);
        }

        public static bool IsRandomizedTransition(string source)
        {
            return _randomizedTransitions.Contains(source);
        }
    
        public static bool IsInTransitionLookup(string source)
        {
            return _transitionLookup.ContainsKey(source);
        }
    
        public static bool IsSpecialRoom(this string room)
        {
            // Rooms that we care about that aren't randomized
            return room == "Room_Tram_RG"
             || room == "Room_Tram"
             || room == "GG_Atrium"
             || room == "GG_Workshop"
             || room == "GG_Atrium_Roof";
        }
    
    
        public static string GetScene(string source)
        {
             if (_transitionLookup.TryGetValue(source, out TransitionPlacement placement))
             {
                 //return placement.Source.TransitionDef.SceneName;
             }
             
             //APMapMod.Instance.Log("GetTransitionScene null " + source);
    
            return null;
        }
    
        public static string GetTransitionDoor(string source)
        {
             if (_transitionLookup.TryGetValue(source, out TransitionPlacement placement))
             {
                 //return placement.Source.TransitionDef.DoorName;
             }
    
            APMapMod.Instance.Log("GetTransitionDoor null " + source);
    
            return null;
        }
    
        public static string GetAdjacentTransition(string source)
        {
            if (source == "Fungus2_14[bot1]")
            {
                return GetAdjacentTransition("Fungus2_14[bot3]");
            }
    
            if (source == "Fungus2_15[top2]")
            {
                return GetAdjacentTransition("Fungus2_15[top3]");
            }
    
            // if (_transitionLookup.TryGetValue(source, out TransitionPlacement placement)
            //     && placement.Target != null)
            // {
            //     return placement.Target.Name;
            // }
    
            //APMapMod.Instance.Log("GetAdjacentTransition null " + source);
    
            return null;
        }
    
        public static string GetAdjacentScene(string source)
        {
             // if (_transitionLookup.TryGetValue(source, out TransitionPlacement placement)
             //     && placement.Target != null && placement.Target.TransitionDef != null)
             // {
             //     return placement.Target.TransitionDef.SceneName;
             // }
    
             //APMapMod.Instance.Log("GetAdjacentScene null " + source);
    
            return null;
        }
    
        public static HashSet<string> GetTransitionsByScene(string scene)
        {
            if (scene != null && _transitionsByScene.ContainsKey(scene))
            {
                return _transitionsByScene[scene];
            }
    
            //APMapMod.Instance.LogWarn("No transitions found for scene " + scene);
    
            return new();
        }
    
        public static string GetUncheckedVisited(string scene)
        {
            string text = "";
    
            IEnumerable<string> uncheckedTransitions = APMapMod.LS.tracker.uncheckedReachableTransitions
                .Where(t => GetScene(t) == scene);
            
            if (uncheckedTransitions.Any())
            {
                text += $"Unchecked";
            
                foreach (string transition in uncheckedTransitions)
                {
                    text += "\n";
            
                    if (!APMapMod.LS.trackerWithoutSequenceBreaks.uncheckedReachableTransitions.Contains(transition))
                    {
                        text += "*";
                    }
            
                    text += GetTransitionDoor(transition);
                }
            }
            
            Dictionary<string, string> visitedTransitions = APMapMod.LS.tracker.visitedTransitions
                .Where(t => GetScene(t.Key) == scene).ToDictionary(t => t.Key, t => t.Value);
            
            text += BuildTransitionStringList(visitedTransitions, "Visited", false, text != "");
            
            Dictionary<string, string> visitedTransitionsTo = APMapMod.LS.tracker.visitedTransitions
            .Where(t => GetScene(t.Value) == scene).ToDictionary(t => t.Key, t => t.Value);
            
            // Display only one-way transitions in coupled rando
            // if (RM.RS.GenerationSettings.TransitionSettings.Coupled)
            // {
            //     visitedTransitionsTo = visitedTransitionsTo.Where(t => !visitedTransitions.ContainsKey(t.Value)).ToDictionary(t => t.Key, t => t.Value);
            // }
            
            text += BuildTransitionStringList(visitedTransitionsTo, "Visited to", true, text != "");
            
            Dictionary<string, string> vanillaTransitions = APMapMod.LS.Context.Vanilla
                .Where(t => RCData.IsTransition(t.Location.Name)
                    && GetScene(t.Location.Name) == scene
                    && APMapMod.LS.tracker.pm.Get(t.Location.Name) > 0)
                .ToDictionary(t => t.Location.Name, t => t.Item.Name);
            
            
            text += BuildTransitionStringList(vanillaTransitions, "Vanilla", false, text != "");
            
            Dictionary<string, string> vanillaTransitionsTo = APMapMod.LS.Context.Vanilla
                .Where(t => RCData.IsTransition(t.Location.Name)
                    && GetScene(t.Item.Name) == scene
                    && APMapMod.LS.tracker.pm.Get(t.Item.Name) > 0
                    && !vanillaTransitions.ContainsKey(t.Item.Name))
                .ToDictionary(t => t.Location.Name, t => t.Item.Name);
            
            text += BuildTransitionStringList(vanillaTransitionsTo, "Vanilla to", true, text != "");
    
            return text;
        }
    
        public static string BuildTransitionStringList(Dictionary<string, string> transitions, string subtitle, bool to, bool addNewLines)
        {
            string text = "";
        
            if (transitions.Any())
            {
                if (addNewLines)
                {
                    text += "\n\n";
                }
        
                text += $"{subtitle}:";
        
                foreach (KeyValuePair<string, string> pair in transitions)
                {
                    text += "\n";
        
                    if (APMapMod.LS.trackerWithoutSequenceBreaks.outOfLogicVisitedTransitions.Contains(pair.Key))
                    {
                        text += "*";
                    }
        
                    if (to)
                    {
                        text += pair.Key + " -> " + GetTransitionDoor(pair.Value);
                    }
                    else
                    {
                        text += GetTransitionDoor(pair.Key) + " -> " + pair.Value;
                    }
                }
            }
        
            return text;
        }
    
        public static void SetTransitionLookup()
        {
            _randomizedTransitions = new();
            _transitionLookup = new();
            _transitionsByScene = new();
        
            // if (Ctx.transitionPlacements != null)
            // {
            //     _randomizedTransitions = new(Ctx.transitionPlacements.Select(tp => tp.Source.Name));
            //     _transitionLookup = Ctx.transitionPlacements.ToDictionary(tp => tp.Source.Name, tp => tp);
            // }
            //
            // foreach (GeneralizedPlacement gp in Ctx.Vanilla.Where(gp => RCData.IsTransition(gp.Location.Name)))
            // {
            //     RandoModTransition target = new(Lm.GetTransition(gp.Item.Name))
            //     {
            //         TransitionDef = RCData.GetTransitionDef(gp.Item.Name)
            //     };
            //
            //     RandoModTransition source = new(Lm.GetTransition(gp.Location.Name))
            //     {
            //         TransitionDef = RCData.GetTransitionDef(gp.Location.Name)
            //     };
            //
            //     _transitionLookup.Add(gp.Location.Name, new(target, source));
            // }
            //
            // if (Ctx.transitionPlacements != null)
            // {
            //     // Add impossible transitions (because we still need info like scene name etc.)
            //     foreach (TransitionPlacement tp in Ctx.transitionPlacements)
            //     {
            //         if (!_transitionLookup.ContainsKey(tp.Target.Name))
            //         {
            //             _transitionLookup.Add(tp.Target.Name, new(null, tp.Target));
            //         }
            //     }
            // }
            //
            // foreach (GeneralizedPlacement gp in Ctx.Vanilla.Where(gp => RCData.IsTransition(gp.Location.Name)))
            // {
            //     if (!_transitionLookup.ContainsKey(gp.Item.Name))
            //     {
            //         RandoModTransition source = new(Lm.GetTransition(gp.Item.Name))
            //         {
            //             TransitionDef = RCData.GetTransitionDef(gp.Item.Name)
            //         };
            //
            //         _transitionLookup.Add(gp.Item.Name, new(null, source));
            //     }
            // }
            //
            // // Get transitions sorted by scene
            // _transitionsByScene = new();
            //
            // foreach (TransitionPlacement tp in _transitionLookup.Values.Where(tp => tp.Target != null))
            // {
            //     string scene = tp.Source.TransitionDef.SceneName;
            //
            //     if (!_transitionsByScene.ContainsKey(scene))
            //     {
            //         _transitionsByScene.Add(scene, new() { tp.Source.Name });
            //     }
            //     else
            //     {
            //         _transitionsByScene[scene].Add(tp.Source.Name);
            //     }
            // }
        }
    }
}
