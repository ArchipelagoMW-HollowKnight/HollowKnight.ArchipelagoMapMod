using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using APMapMod.Data;
using APMapMod.Settings;
using Archipelago.HollowKnight.IC;
using Archipelago.MultiClient.Net.Enums;
using ItemChanger;
using UnityEngine;

namespace APMapMod.Map
{
    public class PinAnimatedSprite : MonoBehaviour
    {
        public PinDef PD { get; private set; } = null;
        
        SpriteRenderer SR => gameObject.GetComponent<SpriteRenderer>();

        SpriteRenderer BorderSR => transform.GetChild(0).GetComponent<SpriteRenderer>();

        private int spriteIndex = 0;

        private readonly Color _inactiveColor = Color.gray;
        
        private Color _origColor;

        public void SetPinData(PinDef pd)
        {
            PD = pd;
            _origColor = SR.color;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Member is actually used")]
        private void OnEnable()
        {
            if (gameObject.activeSelf
                && PD != null
                && PD.randoItems != null
                && PD.randoItems.Count() > 1)
            {
                StartCoroutine("CycleSprite");
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Member is actually used")]
        private void OnDisable()
        {
            if (!gameObject.activeSelf)
            {
                StopAllCoroutines();
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Member is actually used")]
        private IEnumerator CycleSprite()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1);
                spriteIndex = (spriteIndex + 1) % PD.randoItems.Count();
                SetSprite();
            }
        }

        public void ResetSpriteIndex()
        {
            spriteIndex = 0;
        }

        public void SetSprite()
        {
            if (!gameObject.activeSelf) return;

            // Non-randomized
            if (PD.pinLocationState == PinLocationState.NonRandomizedUnchecked)
            {
                SR.sprite = SpriteManager.GetSpriteFromPool(PD.locationPoolGroup, false);
                
                return;
            }

            if (PD.randoItems == null || spriteIndex + 1 > PD.randoItems.Count()) return;

            // Set pool to display
            string pool = PD.locationPoolGroup;
            bool normalOverride = false;

            if (PD.pinLocationState is PinLocationState.Previewed or PinLocationState.ClearedPersistent
                || PD.randoItems.ElementAt(spriteIndex).item.GetTag<ArchipelagoItemTag>().Hinted)
            {
                var itemDef = PD.randoItems.ElementAt(spriteIndex);
                
                if (Finder.ItemNames.Contains(itemDef.itemName))
                {
                    pool = itemDef.poolGroup;
                }
                else if (itemDef.item.GetTag<ArchipelagoItemTag>().Flags.HasFlag(ItemFlags.Advancement))
                {
                    pool = "Archipelago Progression";
                }
                else if (itemDef.item.GetTag<ArchipelagoItemTag>().Flags.HasFlag(ItemFlags.NeverExclude))
                {
                    pool = "Archipelago Useful";
                }
                else
                {
                    pool = "Archipelago";
                }
                
                normalOverride = true;
            }

            SR.sprite = SpriteManager.GetSpriteFromPool(pool, normalOverride);

            SetBorderColor(false);
        }

        public void SetSizeAndColor()
        {
            // Size
            transform.localScale = PD.pinLocationState switch
            {
                PinLocationState.UncheckedReachable
                or PinLocationState.OutOfLogicReachable
                or PinLocationState.Previewed
                =>  new Vector3(1.45f * GetPinScale(), 1.45f * GetPinScale(), 1f),
                _ => new Vector3(1.015f * GetPinScale(), 1.015f * GetPinScale(), 1f)
            };

            // Color
            SR.color = PD.pinLocationState switch
            {
                PinLocationState.UncheckedReachable
                or PinLocationState.OutOfLogicReachable
                or PinLocationState.Previewed
                or PinLocationState.ClearedPersistent
                => _origColor,

                _ => _inactiveColor,
            };

            SetBorderColor(false);
        }

        public void SetSizeAndColorSelected()
        {
            transform.localScale = new Vector3(1.8f * GetPinScale(), 1.8f * GetPinScale(), 1f);
            SR.color = _origColor;
            SetBorderColor(true);
        }

        private float GetPinScale()
        {
            return APMapMod.GS.pinSize switch
            {
                PinSize.Small => 0.31f,
                PinSize.Medium => 0.37f,
                PinSize.Large => 0.42f,
                _ => throw new NotImplementedException()
            };
        }
        
        private void SetBorderColor(bool highlightOverride)
        {
            if (PD.randoItems != null && PD.randoItems.Any())
            {
                if (PD.randoItems.ElementAt(spriteIndex).item.GetTag(out ArchipelagoItemTag tag))
                {
                    if (tag.Hinted)
                    {
                        if (PD.randoItems.ElementAt(spriteIndex).persistent)
                        {
                            BorderSR.sprite = SpriteManager.GetSprite("pinBorderHexagon");
                            BorderSR.color = Colors.GetColor(ColorSetting.Pin_Persistent);
                        }
                        else
                        {
                            BorderSR.sprite = SpriteManager.GetSprite("pinBorderDiamond");
                            BorderSR.color = Colors.GetColor(ColorSetting.Pin_Previewed);
                        }

                        return;
                    }
                }
            }
            
            BorderSR.sprite = SpriteManager.GetSprite("pinBorder");
            
            switch (PD.pinLocationState)
            {
                case PinLocationState.UncheckedUnreachable:
                case PinLocationState.NonRandomizedUnchecked:
                    if (highlightOverride)
                    {
                        BorderSR.color = Colors.GetColor(ColorSetting.Pin_Normal);
                    }
                    else
                    {
                        BorderSR.color = GrayOut(Colors.GetColor(ColorSetting.Pin_Normal));
                    }
                    break;
                case PinLocationState.UncheckedReachable:
                    BorderSR.color = Colors.GetColor(ColorSetting.Pin_Normal);
                    break;
                case PinLocationState.OutOfLogicReachable:
                    BorderSR.color = Colors.GetColor(ColorSetting.Pin_Out_of_logic);
                    break;
                case PinLocationState.Previewed:
                    BorderSR.sprite = SpriteManager.GetSprite("pinBorderDiamond");
                    BorderSR.color = Colors.GetColor(ColorSetting.Pin_Previewed);
                    break;
                case PinLocationState.ClearedPersistent:
                    //APMapMod.Instance.LogDebug($"hex border for {PD.name}");
                    BorderSR.sprite = SpriteManager.GetSprite("pinBorderHexagon");
                    BorderSR.color = Colors.GetColor(ColorSetting.Pin_Persistent);
                    break;
                default:
                    break;
            }
        }

        private Vector4 GrayOut(Vector4 color)
        {
            Vector4 newColor = new();

            newColor.x = color.x / 2f;
            newColor.y = color.y / 2f;
            newColor.z = color.z / 2f;
            newColor.w = color.w;

            return newColor;
        }
    }
}
