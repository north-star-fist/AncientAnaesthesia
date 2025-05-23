using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace Sergei.Safonov.UMA.CharacterSystem {

    /// <summary>
    /// The same utility that UMA provides out of the box, but this one does not remove default renderers.
    /// It just adds new slots with specified rendering assets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DynamicCharacterAvatar))]
    public class DcaRendererManager : MonoBehaviour {

        [System.Serializable]
        public class RendererElement {
            public List<UMARendererAsset> rendererAssets = new();
            public List<SlotDataAsset> slotAssets = new();
            public List<string> wardrobeSlots = new();
        }
        public List<RendererElement> RendererElements = new();

        bool _lastRenderersEnabled;

        public bool RenderersEnabled = true;

        private DynamicCharacterAvatar _avatar;
        private readonly UMAData.UMARecipe _umaRecipe = new();
        readonly List<SlotDataAsset> _wardrobeSlotAssets = new();
        private UMAContextBase _context;
        private readonly List<SlotData> _slotsToAdd = new();


        private bool _areCustomRenderSlotsAdded;

        // Use this for initialization
        void Start() {
            _avatar = GetComponent<DynamicCharacterAvatar>();
            _avatar.CharacterBegun.AddListener(CharacterBegun);
            _context = UMAContextBase.Instance;
            _lastRenderersEnabled = RenderersEnabled; // only cause it to rebuild if it actually changes
        }

        private void Update() {
            if (RenderersEnabled != _lastRenderersEnabled) {
                if (!_avatar.activeRace.isValid || _avatar.UpdatePending() || _avatar.hide) {
                    return;
                }
#if UMA_ADDRESSABLES
                if (_avatar.AddressableBuildPending) {
                    return;
                }
#endif

                _lastRenderersEnabled = RenderersEnabled;
                _avatar.BuildCharacter();
            }
        }

        void CharacterBegun(UMAData umaData) {
            //If mesh is not dirty then we haven't changed slots.
            if (_areCustomRenderSlotsAdded || !RenderersEnabled || !umaData.isMeshDirty) {
                return;
            }
            addSlotsForRenderers(umaData);
            _areCustomRenderSlotsAdded = true;
        }

        private void addSlotsForRenderers(UMAData umaData) {
            SlotData[] currentSlots = umaData.umaRecipe.slotDataList;
            _slotsToAdd.Clear();

            for (int i = 0; i < RendererElements.Count; i++) {
                RendererElement element = RendererElements[i];
                if (element.rendererAssets == null || element.rendererAssets.Count <= 0) {
                    continue;
                }

                // First, lets collect a list of the slotDataAssets that are present in the wardrobe recipes
                // of the wardrobe slots we've specified
                _wardrobeSlotAssets.Clear();
                for (int j = 0; j < element.wardrobeSlots.Count; j++) {
                    addWardrobeSlotAssets(_wardrobeSlotAssets, element.wardrobeSlots[j]);
                }

                //Next, check each slot for if they are in the list of specified slots or exist in one of the wardrobe recipes of the wardrobe slot we specified.
                for (int j = 0; j < currentSlots.Length; j++) {
                    SlotData slot = currentSlots[j];
                    if (HasSlot(element.slotAssets, slot.slotName) || HasSlot(_wardrobeSlotAssets, slot.slotName)) {

                        // This portion of UMA original code is a part that overrides rendering of a slot
                        /*
                        //We check for at least one rendererAsset at the top level for loop.
                        //Set our existing slot to the first renderer in our renderer list.
                        slot.rendererAsset = element.rendererAssets[0];

                        //If we have more renderers then make a copy of the SlotData and set that copy's rendererAsset to this item's renderer.
                        //Add the newly created slots to a running list to combine back with the entire slot list at the end.
                        for (int k = 1; k < element.rendererAssets.Count; k++) {
                        */

                        for (int k = 0; k < element.rendererAssets.Count; k++) {
                            SlotData addSlot = slot.Copy();
                            addSlot.rendererAsset = element.rendererAssets[k];
                            _slotsToAdd.Add(addSlot);
                        }
                    }
                }
            }

            //If we have added Slots, then add the first slots to the list and set the recipe's slots to the new combined list.
            if (_slotsToAdd.Count > 0) {
                _slotsToAdd.AddRange(currentSlots);
                umaData.umaRecipe.SetSlots(_slotsToAdd.ToArray());
                _slotsToAdd.Clear();
            }

            _wardrobeSlotAssets.Clear();

            // Adds wardrobe slot assets to the list
            void addWardrobeSlotAssets(List<SlotDataAsset> wardrobeSlotAssets, string wardrobeSlot) {
                UMATextRecipe recipe = _avatar.GetWardrobeItem(wardrobeSlot);
                if (recipe != null) {
                    recipe.Load(_umaRecipe, _context);

                    if (_umaRecipe.slotDataList != null) {
                        for (int k = 0; k < _umaRecipe.slotDataList.Length; k++) {
                            SlotData slotData = _umaRecipe.slotDataList[k];
                            if (slotData == null) {
                                continue;
                            }

                            if (slotData.isBlendShapeSource) {
                                continue;
                            }

                            if (slotData != null && slotData.asset != null) {
                                wardrobeSlotAssets.Add(slotData.asset);
                            }
                        }
                    }
                }
            }
        }

        private static bool HasSlot(List<SlotDataAsset> slots, string slotName) {
            if (slots != null) {
                for (int i = 0; i < slots.Count; i++) {
                    SlotDataAsset sl = slots[i];
                    if (sl != null && sl.slotName == slotName) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
