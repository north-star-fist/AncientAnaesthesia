using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace AncientAnaesthesia {

    /// <summary>
    /// Simplified <see cref="UMARandomAvatar"/> with no ability to spawn anything on Start().
    /// </summary>
    public class UmaRandomPatient : MonoBehaviour {
        public List<UMARandomizer> Randomizers;
        public GameObject prefab;
        public bool ShowPlaceholder;
        public UMARandomAvatarEvent RandomAvatarGenerated;

        private DynamicCharacterAvatar RandomAvatar;


        public void GenerateRandomCharacter(Vector3 Pos, Quaternion Rot, string Name, Transform parent = null) {
            if (prefab) {
                GameObject go = GameObject.Instantiate(prefab, Pos, Rot);
                go.transform.parent = parent;
                RandomAvatar = go.GetComponent<DynamicCharacterAvatar>();
                go.name = Name;
                // Event for possible networking here
                if (RandomAvatarGenerated != null) {
                    RandomAvatarGenerated.Invoke(gameObject, go);
                }
            }
            Randomize(RandomAvatar);
            RandomAvatar.BuildCharacter(!RandomAvatar.BundleCheck);
        }

        public RandomWardrobeSlot GetRandomWardrobe(List<RandomWardrobeSlot> wardrobeSlots) {
            int total = 0;

            for (int i = 0; i < wardrobeSlots.Count; i++) {
                RandomWardrobeSlot rws = wardrobeSlots[i];
                total += rws.Chance;
            }

            for (int i = 0; i < wardrobeSlots.Count; i++) {
                RandomWardrobeSlot rws = wardrobeSlots[i];
                if (UnityEngine.Random.Range(0, total) < rws.Chance) {
                    return rws;
                }
            }
            return wardrobeSlots[wardrobeSlots.Count - 1];
        }

        private OverlayColorData GetRandomColor(RandomColors rc) {
            int inx = UnityEngine.Random.Range(0, rc.ColorTable.colors.Length);
            return rc.ColorTable.colors[inx];
        }

        private void AddRandomSlot(DynamicCharacterAvatar Avatar, RandomWardrobeSlot uwr) {
            Avatar.SetSlot(uwr.WardrobeSlot);
            if (uwr.Colors != null) {
                for (int i = 0; i < uwr.Colors.Count; i++) {
                    RandomColors rc = uwr.Colors[i];
                    if (rc.ColorTable != null) {
                        OverlayColorData ocd = GetRandomColor(rc);
                        Avatar.SetColor(rc.ColorName, ocd, false);
                    }
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (ShowPlaceholder) {
                Gizmos.DrawCube(transform.position, Vector3.one);
            }
        }
#endif


        public void Randomize(DynamicCharacterAvatar Avatar) {
            // Must clear that out!
            Avatar.WardrobeRecipes.Clear();

            UMARandomizer Randomizer = null;
            if (Randomizers != null) {
                if (Randomizers.Count == 0) {
                    return;
                }

                if (Randomizers.Count == 1) {
                    Randomizer = Randomizers[0];
                } else {
                    Randomizer = Randomizers[UnityEngine.Random.Range(0, Randomizers.Count)];
                }
            }
            if (Avatar != null && Randomizer != null) {
                RandomAvatar ra = Randomizer.GetRandomAvatar();
                Avatar.ChangeRaceData(ra.RaceName);
                //Avatar.BuildCharacterEnabled = true;
                var RandomDNA = ra.GetRandomDNA();
                Avatar.predefinedDNA = RandomDNA;
                var RandomSlots = ra.GetRandomSlots();

                if (ra.SharedColors != null && ra.SharedColors.Count > 0) {
                    for (int i = 0; i < ra.SharedColors.Count; i++) {
                        RandomColors rc = ra.SharedColors[i];
                        if (rc.ColorTable != null) {
                            Avatar.SetColor(rc.ColorName, GetRandomColor(rc), false);
                        }
                    }
                }
                foreach (string s in RandomSlots.Keys) {
                    List<RandomWardrobeSlot> RandomWardrobe = RandomSlots[s];
                    RandomWardrobeSlot uwr = GetRandomWardrobe(RandomWardrobe);
                    if (uwr.WardrobeSlot != null) {
                        AddRandomSlot(Avatar, uwr);
                    }
                }
            }
        }
    }
}