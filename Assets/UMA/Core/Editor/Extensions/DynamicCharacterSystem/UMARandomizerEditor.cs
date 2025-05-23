﻿using System.Collections.Generic;
using System.Linq;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;


namespace UMA.Editors {
    [CustomEditor(typeof(UMARandomizer))]
    public class UMARandomizerEditor : Editor {

        UMARandomizer currentTarget = null;

        private readonly Dictionary<string, Dictionary<int, int>> _recipesToAddSelected = new();

        public void OnEnable() {
            currentTarget = target as UMARandomizer;
            List<string> Races = new List<string>();
            currentTarget.raceDatas = new List<RaceData>();

            currentTarget.raceDatas = UMAAssetIndexer.Instance.GetAllAssets<RaceData>();
            for (int i = 0; i < currentTarget.raceDatas.Count; i++) {
                RaceData race = currentTarget.raceDatas[i];
                if (race != null) {
                    Races.Add(race.name);
                }
            }
            currentTarget.races = Races.ToArray();

            // My amendment. Sergei Safonov
            foreach (var rData in currentTarget.raceDatas) {
                Dictionary<int, int> recipeMap = new Dictionary<int, int>();
                _recipesToAddSelected.Add(rData.raceName, recipeMap);
            }
        }


        protected bool DropAreaGUI(Rect dropArea) {

            var evt = Event.current;

            if (evt.type == EventType.DragUpdated) {
                if (dropArea.Contains(evt.mousePosition)) {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            }

            if (evt.type == EventType.DragPerform) {
                currentTarget.droppedItems.Clear();
                if (dropArea.Contains(evt.mousePosition)) {
                    DragAndDrop.AcceptDrag();

                    UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences as UnityEngine.Object[];
                    for (int i = 0; i < draggedObjects.Length; i++) {
                        if (draggedObjects[i]) {
                            if (draggedObjects[i] is UMAWardrobeRecipe) {
                                UMAWardrobeRecipe utr = draggedObjects[i] as UMAWardrobeRecipe;
                                currentTarget.droppedItems.Add(utr);
                                continue;
                            }

                            var path = AssetDatabase.GetAssetPath(draggedObjects[i]);
                            if (System.IO.Directory.Exists(path)) {
                                RecursiveScanFoldersForAssets(path);
                            }
                        }
                    }
                }
            }
            return currentTarget.droppedItems.Count > 0;
        }

        protected void RecursiveScanFoldersForAssets(string path) {
            var assetFiles = System.IO.Directory.GetFiles(path, "*.asset");
            for (int i = 0; i < assetFiles.Length; i++) {
                string assetFile = assetFiles[i];
                var tempRecipe = AssetDatabase.LoadAssetAtPath(assetFile, typeof(UMAWardrobeRecipe)) as UMAWardrobeRecipe;
                if (tempRecipe) {
                    currentTarget.droppedItems.Add(tempRecipe);
                }
            }
            string[] array = System.IO.Directory.GetDirectories(path);
            for (int i = 0; i < array.Length; i++) {
                string subFolder = array[i];
                RecursiveScanFoldersForAssets(subFolder.Replace('\\', '/'));
            }
        }

        public bool RandomColorsGUI(RandomAvatar ra, RandomWardrobeSlot rws, RandomColors rc) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shared Color", GUILayout.Width(80));
            rc.CurrentColor = EditorGUILayout.Popup(rc.CurrentColor, rws.PossibleColors, GUILayout.Width(80));
            rc.ColorName = rws.PossibleColors[rc.CurrentColor];
            EditorGUILayout.LabelField("Color Table", GUILayout.Width(80));
            rc.ColorTable = (SharedColorTable)EditorGUILayout.ObjectField(rc.ColorTable, typeof(SharedColorTable), false, GUILayout.ExpandWidth(true));
            bool retval = GUILayout.Button("\u0078", EditorStyles.miniButton, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            return retval;
        }

        public void RandomColorsGUI(RandomAvatar ra, RandomColors rc) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shared Color", GUILayout.Width(80));
            EditorGUILayout.LabelField(rc.ColorName, EditorStyles.textField, GUILayout.Width(80));
            EditorGUILayout.LabelField("Color Table", GUILayout.Width(80));
            rc.ColorTable = (SharedColorTable)EditorGUILayout.ObjectField(rc.ColorTable, typeof(SharedColorTable), false, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        public void RandomWardrobeSlotGUI(RandomAvatar ra, RandomWardrobeSlot rws) {
            // do random colors
            // show each possible item.
            string name = "<null>";
            if (rws.WardrobeSlot != null) {
                name = rws.WardrobeSlot.name;
            }

            GUIHelper.FoldoutBar(ref rws.GuiFoldout, name + " (" + rws.Chance + ")", out rws.Delete);
            if (rws.GuiFoldout) {
                GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.75f, 0.75f));
                rws.Chance = EditorGUILayout.IntSlider("Weighted Chance", rws.Chance, 1, 100);
                if (rws.PossibleColors.Length > 0) {
                    if (GUILayout.Button("Add Shared Color")) {
                        rws.AddColorTable = true;
                    }
                    RandomColors delme = null;
                    for (int i = 0; i < rws.Colors.Count; i++) {
                        RandomColors rc = rws.Colors[i];
                        if (RandomColorsGUI(ra, rws, rc)) {
                            delme = rc;
                        }
                    }
                    if (delme != null) {
                        rws.Colors.Remove(delme);
                        EditorUtility.SetDirty(this.target);
                        string path = AssetDatabase.GetAssetPath(target.GetInstanceID());
                        AssetDatabase.ImportAsset(path);
                    }
                } else {
                    GUILayout.Label("Wardrobe Recipe has no Shared Colors");
                }
                GUIHelper.EndVerticalPadded(10);
            }
        }

        public void RandomAvatarGUI(RandomAvatar ra) {
            if (ra.raceData == null) {
                ra.raceData = UMAAssetIndexer.Instance.GetAsset<RaceData>(ra.RaceName);
            }
            bool del = false;
            GUIHelper.FoldoutBar(ref ra.GuiFoldout, ra.RaceName, out del);
            if (ra.GuiFoldout) {
                GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.875f, 1f));
                if (del) {
                    ra.Delete = true;
                }

                ra.Chance = EditorGUILayout.IntSlider("Weighted Chance", ra.Chance, 1, 100);

                ra.ColorsFoldout = GUIHelper.FoldoutBar(ra.ColorsFoldout, "Colors");
                if (ra.ColorsFoldout) {

                    GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.75f, 0.75f));
                    if (ra.SharedColors != null && ra.SharedColors.Count > 0) {
                        for (int i = 0; i < ra.SharedColors.Count; i++) {
                            RandomColors rc = ra.SharedColors[i];
                            RandomColorsGUI(ra, rc);
                        }
                    } else {
                        EditorGUILayout.LabelField("No shared colors found on base race");
                    }
                    GUIHelper.EndVerticalPadded(10);
                }

                ra.DnaFoldout = GUIHelper.FoldoutBar(ra.DnaFoldout, "DNA");
                if (ra.DnaFoldout) {
                    GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.75f, 0.75f));
                    // (popup with DNA names) and "Add" button.
                    EditorGUILayout.BeginHorizontal();
                    ra.SelectedDNA = EditorGUILayout.Popup("DNA", ra.SelectedDNA, ra.PossibleDNA);
                    bool pressed = GUILayout.Button("Add DNA", EditorStyles.miniButton);// GUIStyles.Popup?
                    EditorGUILayout.EndHorizontal();
                    if (pressed) {
                        ra.DNAAdd = ra.PossibleDNA[ra.SelectedDNA];
                    }
                    if (ra.RandomDna.Count == 0) {
                        EditorGUILayout.LabelField("No Random DNA has been added");
                    } else {

                        for (int i = 0; i < ra.RandomDna.Count; i++) {
                            RandomDNA rd = ra.RandomDna[i];
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(rd.DnaName, EditorStyles.miniLabel, GUILayout.Width(100));
                            float lastMin = rd.MinValue;
                            float lastMax = rd.MaxValue;
                            EditorGUILayout.MinMaxSlider(ref rd.MinValue, ref rd.MaxValue, 0.0f, 1.0f);
                            if (rd.MinValue != lastMin || rd.MaxValue != lastMax) {
                                ra.DnaChanged = true;
                            }

                            rd.Delete = GUILayout.Button("\u0078", EditorStyles.miniButton, GUILayout.ExpandWidth(false));
                            string vals = rd.MinValue.ToString("N3") + " - " + rd.MaxValue.ToString("N3");
                            EditorGUILayout.LabelField(vals, EditorStyles.miniTextField, GUILayout.Width(80));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    GUIHelper.EndVerticalPadded(10);

                }
                ra.WardrobeFoldout = GUIHelper.FoldoutBar(ra.WardrobeFoldout, "Wardrobe");
                if (ra.WardrobeFoldout) {
                    // add a null slot for a 
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Select Wardrobe Slot", GUILayout.ExpandWidth(false));
                    ra.currentWardrobeSlot = EditorGUILayout.Popup(ra.currentWardrobeSlot, ra.raceData.wardrobeSlots.ToArray(), GUILayout.ExpandWidth(true));

                    // My amends beginning. Sergei Safonov
                    /*
                    if (GUILayout.Button("Add Null", GUILayout.ExpandWidth(false))) {
                        ra.RandomWardrobeSlots.Add(new RandomWardrobeSlot(null, ra.raceData.wardrobeSlots[ra.currentWardrobeSlot]));
                        ra.RandomWardrobeSlots.Sort((x, y) => x.SortName.CompareTo(y.SortName));
                    }*/
                    if (ra.currentWardrobeSlot > 0) {
                        var raceName = ra.raceData.raceName;
                        if (_recipesToAddSelected.TryGetValue(raceName, out var recMap)) {
                            var recipes = UMAAssetIndexer.Instance.GetRecipesForRaceSlot(
                                raceName,
                                ra.raceData.wardrobeSlots[ra.currentWardrobeSlot]
                            );
                            recipes.Insert(0, null);
                            if (!recMap.TryGetValue(ra.currentWardrobeSlot, out var chosen)) {
                                chosen = 0;
                            }
                            int recipeToAdd = EditorGUILayout.Popup(
                                chosen,
                                recipes.Select(r => r != null ? r.name : "Empty").ToArray(),
                                GUILayout.ExpandWidth(true)
                            );
                            recMap[ra.currentWardrobeSlot] = recipeToAdd;
                            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false))) {
                                if (recipeToAdd > 0) {
                                    if (recipes[recipeToAdd] is UMAWardrobeRecipe wr) {
                                        ra.RandomWardrobeSlots.Add(new RandomWardrobeSlot(wr, ra.raceData.wardrobeSlots[ra.currentWardrobeSlot]));
                                        ra.RandomWardrobeSlots.Sort((x, y) => x.SortName.CompareTo(y.SortName));
                                    }
                                } else {
                                    ra.RandomWardrobeSlots.Add(new RandomWardrobeSlot(null, ra.raceData.wardrobeSlots[ra.currentWardrobeSlot]));
                                    ra.RandomWardrobeSlots.Sort((x, y) => x.SortName.CompareTo(y.SortName));
                                }
                            }
                        }
                    }
                    // Amends end

                    GUILayout.EndHorizontal();
                    GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.75f, 0.75f));

                    string lastSlot = "";

                    for (int i = 0; i < ra.RandomWardrobeSlots.Count; i++) {
                        RandomWardrobeSlot rws = ra.RandomWardrobeSlots[i];
                        if (rws.SlotName != lastSlot) {
                            GUILayout.Label("[" + rws.SlotName + "]");
                            lastSlot = rws.SlotName;
                        }
                        RandomWardrobeSlotGUI(ra, rws);
                    }
                    GUIHelper.EndVerticalPadded(10);
                }
                GUIHelper.EndVerticalPadded(10);
            }
        }


        public override void OnInspectorGUI() {
            if (Event.current.type == EventType.Layout) {
                UpdateObject();
            }

            currentTarget.currentRace = EditorGUILayout.Popup("First Select Race", currentTarget.currentRace, currentTarget.races);
            GUILayout.Space(20);
            Rect updateDropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(updateDropArea, "Then Drag Wardrobe Recipe(s) for " + currentTarget.races[currentTarget.currentRace] + " here");
            GUILayout.Space(10);
            DropAreaGUI(updateDropArea);
            GUILayout.Space(10);
            for (int i = 0; i < currentTarget.RandomAvatars.Count; i++) {
                RandomAvatar ra = currentTarget.RandomAvatars[i];
                RandomAvatarGUI(ra);
            }
            if (GUI.changed) {
                EditorUtility.SetDirty(currentTarget);
                string path = AssetDatabase.GetAssetPath(currentTarget.GetInstanceID());
                AssetDatabase.ImportAsset(path);
            }
        }


        private void UpdateObject() {
            // Add any dropped items.
            int ChangeCount = currentTarget.droppedItems.Count;

            if (currentTarget.droppedItems.Count > 0) {
                for (int i = 0; i < currentTarget.RandomAvatars.Count; i++) {
                    RandomAvatar rv = currentTarget.RandomAvatars[i];
                    rv.GuiFoldout = false;
                    for (int i1 = 0; i1 < rv.RandomWardrobeSlots.Count; i1++) {
                        RandomWardrobeSlot rws = rv.RandomWardrobeSlots[i1];
                        rws.GuiFoldout = false;
                    }
                }

                RandomAvatar ra = FindAvatar(currentTarget.raceDatas[currentTarget.currentRace]);

                // Add all the wardrobe items.
                for (int i = 0; i < currentTarget.droppedItems.Count; i++) {
                    UMAWardrobeRecipe uwr = currentTarget.droppedItems[i];
                    if (RecipeCompatible(uwr, currentTarget.raceDatas[currentTarget.currentRace])) {
                        RandomWardrobeSlot rws = new RandomWardrobeSlot(uwr, uwr.wardrobeSlot);
                        ra.GuiFoldout = true;
                        ra.RandomWardrobeSlots.Add(rws);
                    }
                }
                // sort the wardrobe slots
                ra.RandomWardrobeSlots.Sort((x, y) => x.SortName.CompareTo(y.SortName));
                currentTarget.droppedItems.Clear();
            }

            ChangeCount += currentTarget.RandomAvatars.RemoveAll(x => x.Delete);
            foreach (RandomAvatar ra in currentTarget.RandomAvatars) {
                if (!string.IsNullOrEmpty(ra.DNAAdd)) {
                    ra.DnaChanged = true;
                    ra.RandomDna.Add(new RandomDNA(ra.DNAAdd));
                    ra.DNAAdd = "";
                    ChangeCount++;
                }

                int DNAChangeCount = ra.RandomDna.RemoveAll(x => x.Delete);
                if (DNAChangeCount > 0) {
                    ra.DnaChanged = true;
                    ChangeCount++;
                }
                ChangeCount += ra.SharedColors.RemoveAll(x => x.Delete);
                ChangeCount += ra.RandomWardrobeSlots.RemoveAll(x => x.Delete);
                for (int i = 0; i < ra.RandomWardrobeSlots.Count; i++) {
                    RandomWardrobeSlot rws = ra.RandomWardrobeSlots[i];
                    ChangeCount += rws.Colors.RemoveAll(x => x.Delete);
                    if (rws.AddColorTable) {
                        rws.Colors.Add(new RandomColors(rws));
                        rws.AddColorTable = false;
                        ChangeCount++;
                    }
                }
            }

            if (ChangeCount > 0) {
                EditorUtility.SetDirty(currentTarget);
                string path = AssetDatabase.GetAssetPath(currentTarget.GetInstanceID());
                AssetDatabase.ImportAsset(path);
            }
        }

        private bool RecipeCompatible(UMAWardrobeRecipe uwr, RaceData raceData) {
            // first, see if the recipe is directly compatible with the race.
            for (int i = 0; i < uwr.compatibleRaces.Count; i++) {
                string s = uwr.compatibleRaces[i];
                if (s == raceData.raceName) {
                    return true;
                }
                if (raceData.IsCrossCompatibleWith(s)) {
                    return true;
                }
            }
            return false;
        }


        private RandomAvatar FindAvatar(RaceData raceData) {
            // Is the current race defined?
            for (int i = 0; i < currentTarget.RandomAvatars.Count; i++) {
                RandomAvatar ra = currentTarget.RandomAvatars[i];
                if (raceData.raceName == ra.RaceName) {
                    return ra;
                }
            }
            RandomAvatar rav = new RandomAvatar(raceData);
            currentTarget.RandomAvatars.Add(rav);
            return rav;
        }
    }
}
