using AncientAnaesthesia;
using R3;
using Sergei.Safonov.Unity.Util;
using System;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UMA.PoseTools;
using UnityEngine;


namespace Sergei.Safonov.UMA {

    /// <summary>
    /// Component that transports hit-caused changes to UMA avatar. The changes include
    /// material changes, DNA changes, expression changes and wardrobe changes.
    /// </summary>
    public class UmaDamageableAvatar : MonoBehaviour, IPatient {

        [SerializeField, Layer]
        private int _patientLayer = 0;
        [SerializeField, Tooltip("Damage configuration")]
        DamageableArea[] _damageableAreas;
        [SerializeField, Tooltip("Wardrope slots that are used for head accessories. ('Helmet' for example)")]
        PunchOffStuff[] _punchOff;


        public Rigidbody Rigidbody => GetComponentInChildren<Rigidbody>();

        public IObservable<bool> ReadyForPunch => _readyForPunch.AsSystemObservable();
        private readonly ReactiveProperty<bool> _readyForPunch = new ReactiveProperty<bool>(false);

        private DynamicCharacterAvatar _avatar;
        private ExpressionPlayer _umaExpressionPlayer;

        // The next two maps exist because different areas can refer the same material
        // AreaID -> (rendererName, matIndex, paramHash)
        private readonly Dictionary<string, (string, int, int)> _areaMatMap = new();
        // (rendererName, matIndex, paramHash) -> MatParams
        private readonly Dictionary<(string, int, int), MaterialParam> _materialData = new();

        private readonly Dictionary<int, List<float>> _dnaInitValues = new();
        private readonly Dictionary<int, List<float>> _dnaDeltas = new();
        private float[] _exprInitValues;
        private float[] _exprValues;
        private readonly List<string> _hatSlots = new List<string>();

        private void Awake() {
            _avatar = GetComponent<DynamicCharacterAvatar>();
            _avatar.CharacterCreated.AddListener(HandleCharacterCreation);
            _avatar.CharacterUpdated.AddListener(HandleCharacterUpdate);
        }

        public void Damage(string areaId, Vector3 position, Vector3 normal, float force01) {
            UpdateMaterials(areaId, force01);

            var dnaChanged = UpdateDna(areaId, force01);

            UpdateExpressions(areaId, force01);

            UpdateHats(position, normal, force01);
            if (dnaChanged) {
                _avatar.umaData.Dirty(true, false, false);
            }
        }

        public void KnockOut(Vector3 forceImpulse) {
            var physAvatar = _avatar.GetComponent<UmaPhysicsExtAvatar>();
            if (physAvatar == null) {
                return;
            }
            physAvatar.SetProperties(false, false, true, true, 1, false, false, true);
            GameObject headObj = _avatar.umaData.GetBoneGameObject("Head");
            if (headObj != null) {
                physAvatar.AddForce(forceImpulse, headObj.transform.position, ForceMode.Impulse);
            } else {
                physAvatar.AddForce(forceImpulse, transform.position, ForceMode.Impulse);
            }
        }

        public void Heal() {
            resetMaterials();
            resetDna();
            resetExpressions();

            void resetDna() {
                foreach (var dnaPair in _dnaInitValues) {
                    var dna = _avatar.umaData.GetDna(dnaPair.Key);
                    var dnaList = dnaPair.Value;
                    List<float> deltaList = _dnaDeltas[dnaPair.Key];
                    for (int i = 0; i < dnaList.Count; i++) {
                        dna.SetValue(i, dnaList[i]);

                        deltaList[i] = 0f;
                    }
                }
            }

            void resetExpressions() {
                if (_exprInitValues != null) {
                    for (int i = 0; i < _umaExpressionPlayer.Values.Length; i++) {
                        _umaExpressionPlayer.Values = _exprInitValues;
                        if (_exprValues == null || _exprValues.Length < _exprInitValues.Length) {
                            _exprValues = new float[_exprInitValues.Length];
                        }
                        Array.Copy(_exprInitValues, _exprValues, _umaExpressionPlayer.Values.Length);
                    }
                }
            }

            void resetMaterials() {
                foreach (var matParam in _materialData.Values) {
                    var newVal = 0f;    // Assuming any mat 'damage' param is initially 0
                    matParam.CurrentValue = newVal;
                    matParam.Material.SetFloat(matParam.ParamIndex, newVal);
                }
            }

            _avatar.umaData.Dirty(true, false, false);
        }

        private void UpdateMaterials(string areaId, float force01) {
            if (_areaMatMap.TryGetValue(areaId, out var matKey)) {
                if (_materialData.TryGetValue(matKey, out var matParam)) {
                    var newVal = Mathf.Clamp01(matParam.CurrentValue + force01 * matParam.ForceFactor);
                    matParam.CurrentValue = newVal;
                    matParam.Material.SetFloat(matParam.ParamIndex, newVal);
                }
            }
        }

        private bool UpdateDna(string areaId, float force01) {
            bool changed = false;
            foreach (var area in _damageableAreas) {
                if (area.AreaId == areaId) {
                    if (area.DnaEffectConfigs != null) {
                        foreach (var dnaPair in area.DnaEffectConfigs) {
                            int dnaHash = dnaPair.DnaAsset.dnaTypeHash;
                            var dna = _avatar.umaData.GetDna(dnaHash);
                            int dnaInd = findDnaInd(dna, dnaPair.DnaParameter);
                            if (dnaInd > -1) {
                                float newDelta = _dnaDeltas[dnaHash][dnaInd] + force01 * dnaPair.ForceFactor;
                                newDelta = Mathf.Clamp(newDelta, dnaPair.MinMaxDelta.x, dnaPair.MinMaxDelta.y);
                                float newValue = _dnaInitValues[dnaHash][dnaInd] + newDelta;
                                dna.SetValue(dnaInd, newValue);
                                _dnaDeltas[dnaHash][dnaInd] = newDelta;
                                changed = true;
                            } else {
                                continue;
                            }
                        }
                    }
                    break;
                }
            }
            return changed;


            int findDnaInd(UMADnaBase dna, string dnaParameter) {
                for (int i = 0; i < dna.Names.Length; i++) {
                    if (dna.Names[i] == dnaParameter) {
                        return i;
                    }
                }
                return -1;
            }
        }

        private void UpdateExpressions(string areaId, float force01) {
            if (_umaExpressionPlayer != null) {
                foreach (var area in _damageableAreas) {
                    if (area.AreaId == areaId) {
                        if (area.UmaExpressionConfigs != null) {
                            foreach (var exprConfig in area.UmaExpressionConfigs) {
                                int exprIndex = (int)exprConfig.ExprIndex;
                                float newValue = _exprValues[exprIndex] + force01 * exprConfig.ForceFactor;
                                _exprValues[exprIndex] = Mathf.Clamp(newValue, exprConfig.MinMax.x, exprConfig.MinMax.y);
                            }
                            _umaExpressionPlayer.Values = _exprValues;
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateHats(Vector3 position, Vector3 normal, float force01) {
            var wardrobeRecipes = _avatar.WardrobeRecipes;
            _hatSlots.Clear();
            if (wardrobeRecipes != null) {
                foreach (var wr in wardrobeRecipes) {
                    if (IsHatToPutOff(wr.Key, force01)) {
                        _hatSlots.Add(wr.Key);
                    }
                }
            }
            foreach (var ws in _hatSlots) {
                SpawnHat(ws, position, normal, force01);
                _avatar.ClearSlot(ws);
            }
            if (_hatSlots.Count > 0) {
                _avatar.BuildCharacter(true);
            }
        }


        void HandleCharacterCreation(UMAData umaData) {
            if (_damageableAreas == null) {
                return;
            }
            if (_materialData != null && _materialData.Count > 0) {
                HandleCharacterUpdate(umaData);
            } else {
                ReadMaterialDataFromAvatar(umaData);
            }
            InitUmaExpressionPlayer();
            InitUmaDnaValues();
        }

        void HandleCharacterUpdate(UMAData umaData) {
            if (_damageableAreas == null) {
                return;
            }
            if (_materialData != null && _materialData.Count > 0) {
                foreach (var mData in _materialData) {
                    foreach (var areaData in _damageableAreas) {
                        Renderer renderer = FindRenderer(umaData, areaData.Renderer);
                        if (renderer == null) {
                            continue;
                        }
                        Material mat = null;
                        int matInd = -1;
                        for (int i = 0; i < renderer.materials.Length; i++) {
                            var m = renderer.materials[i];
                            if (m.shader.name == areaData.ShaderName) {
                                mat = m;
                                matInd = i;
                                break;
                            }
                        }
                        if (mat == null) {
                            continue;
                        }
                        if (_areaMatMap.TryGetValue(areaData.AreaId, out var matKey)
                            && _materialData.TryGetValue(matKey, out var matParam)) {
                            matParam.Material = mat;
                            matParam.Material.SetFloat(matParam.ParamIndex, matParam.CurrentValue);
                        }
                    }
                }
            }
        }


        private GameObject SpawnHat(string ws, Vector3 position, Vector3 normal, float force01) {
            var rec = _avatar.WardrobeRecipes[ws];
            // This is non-production code that does not handle all possible options
            GameObject hatGo = new GameObject("Head Accessory");
            var meshFilter = hatGo.AddComponent<MeshFilter>();
            var mRenderer = hatGo.AddComponent<MeshRenderer>();
            Mesh hatMesh = new Mesh();
            var umarecipe = rec.GetUMARecipe();
            List<Material> materials = new List<Material>();
            List<SlotDataAsset> slotAssets;
            var hatSlot = umarecipe.GetFirstSlot();
            if (hatSlot == null) {
                return null;
            }
            var mData = hatSlot.asset.meshData;
            hatMesh.vertices = mData.vertices;
            for (int i = 0; i < mData.subMeshCount; i++) {
                hatMesh.SetTriangles(mData.submeshes[i].getBaseTriangles(), i);
            }
            hatMesh.normals = mData.normals;
            hatMesh.uv = mData.uv;
            var overlay = hatSlot.GetOverlay(0);
            Material material = new Material(overlay.asset.material.material);
            var umaMaterial = overlay.asset.material;
            for (int i = 0; i < overlay.ChannelCount; i++) {
                material.SetTexture(umaMaterial.GetTexturePropertyNames()[i], overlay.GetTexture(i));
            }
            materials.Add(material);

            mRenderer.SetMaterials(materials);
            meshFilter.mesh = hatMesh;

            var globalTrans = _avatar.umaData.umaRoot.transform;
            hatGo.transform.SetPositionAndRotation(globalTrans.position, globalTrans.rotation);

            // Adding collider and rigidbosy (with head bone offset)
            var colGo = new GameObject("Collider");
            colGo.transform.parent = hatGo.transform;
            //var hatCol = colGo.AddComponent<SphereCollider>();
            var hatCol = colGo.AddComponent<BoxCollider>();
            colGo.transform.localPosition = Vector3.zero;
            var rb = hatGo.AddComponent<Rigidbody>();
            rb.mass = 0.1f;

            var bounds = mRenderer.bounds;
            hatCol.center = hatCol.transform.InverseTransformPoint(bounds.center);
            hatCol.size = bounds.size;
            rb.centerOfMass = rb.transform.InverseTransformPoint(bounds.center);

            rb.AddForceAtPosition((-normal + Vector3.up) * force01, position, ForceMode.Impulse);
            return hatGo;
        }


        private void ReadMaterialDataFromAvatar(UMAData umaData) {
            _areaMatMap.Clear();
            _materialData.Clear();
            foreach (var areaData in _damageableAreas) {
                Material mat = GetMaterial(umaData, areaData, out var matInd);
                if (mat == null) {
                    continue;
                }
                (string Renderer, int matInd, int) mKey = (areaData.Renderer, matInd, Shader.PropertyToID(areaData.MaterialParam));
                _areaMatMap.Add(areaData.AreaId, mKey);
                if (!_materialData.TryGetValue(mKey, out var mData)) {
                    _materialData[mKey] = new MaterialParam(
                        mat,
                        Shader.PropertyToID(areaData.MaterialParam),
                        areaData.MaterialForceFactor
                    );
                }
            }
        }

        private static Material GetMaterial(UMAData umaData, DamageableArea renderData, out int matInd) {
            Material mat = null;
            matInd = -1;
            Renderer renderer = FindRenderer(umaData, renderData.Renderer);
            if (renderer == null) {
                Debug.Log($"Renderer is null");
            }
            if (renderer != null) {
                for (int i = 0; i < renderer.materials.Length; i++) {
                    var m = renderer.materials[i];
                    if (m.shader.name == renderData.ShaderName) {
                        matInd = i;
                        mat = m;
                        break;
                    }
                }
            }
            return mat;
        }

        private bool IsHatToPutOff(string slotGroup, float force) {
            if (_punchOff == null) {
                return false;
            }
            foreach (var item in _punchOff) {
                if (item.slotGroup == slotGroup) {
                    if (item.MinForce <= force) {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        private static Renderer FindRenderer(UMAData umaData, string renderer) {
            for (int i = 0; i < umaData.rendererCount; i++) {
                if (umaData.GetRenderer(i).name == renderer) {
                    return umaData.GetRenderer(i);
                }
            }
            return null;
        }

        private void InitUmaExpressionPlayer() {
            var exprPlayer = _avatar.GetComponent<ExpressionPlayer>();
            if (exprPlayer != null) {
                _exprInitValues = copyExpressionValuesToArray(exprPlayer, _exprInitValues);
                _exprValues = copyExpressionValuesToArray(exprPlayer, _exprValues);
            }
            _umaExpressionPlayer = exprPlayer;

            float[] copyExpressionValuesToArray(ExpressionPlayer exprPlayer, float[] toArray) {
                float[] arr = toArray;
                if (arr == null || arr.Length < exprPlayer.Values.Length) {
                    arr = new float[exprPlayer.Values.Length];
                }
                Array.Copy(exprPlayer.Values, arr, exprPlayer.Values.Length);
                return arr;
            }
        }

        private void InitUmaDnaValues() {
            _dnaInitValues.Clear();
            _dnaDeltas.Clear();
            foreach (var areaEffects in _damageableAreas) {
                foreach (var dnaPair in areaEffects.DnaEffectConfigs) {
                    var dnaAsset = dnaPair.DnaAsset;
                    if (!_dnaInitValues.TryGetValue(dnaAsset.dnaTypeHash, out var dnaIinitParams)) {
                        dnaIinitParams = new List<float>();
                        _dnaInitValues[dnaAsset.dnaTypeHash] = dnaIinitParams;
                        var dnaDeltas = new List<float>();
                        _dnaDeltas[dnaAsset.dnaTypeHash] = dnaDeltas;

                        for (int i = 0; i < dnaAsset.Names.Length; i++) {
                            var dna = _avatar.umaData.GetDna(dnaAsset.dnaTypeHash);
                            dnaIinitParams.Add(dna.GetValue(i));
                            dnaDeltas.Add(0f);
                        }
                    }
                }
            }
        }


        [Serializable]
        public class DamageableArea {
            public string AreaId;
            public string Renderer;
            public string ShaderName;
            public string MaterialParam;
            public float MaterialForceFactor = .25f;

            public ExpressionEffectConfig[] UmaExpressionConfigs;
            public DnaEffectConfig[] DnaEffectConfigs;
        }


        [Serializable]
        public class DnaEffectConfig {
            public DynamicUMADnaAsset DnaAsset;
            public string DnaParameter;
            public float MinForce = .05f;
            public float MaxForce = 1f;
            public float ForceFactor = .25f;
            public Vector2 MinMaxDelta = new(-1f, 1f);
        }

        [Serializable]
        public class ExpressionEffectConfig {
            public UmaExpressionIndex ExprIndex;
            public float MinForce = .05f;
            public float MaxForce = 1f;
            public float ForceFactor = .25f;
            public Vector2 MinMax = new(-1f, 1f);
        }

        [Serializable]
        private sealed class MaterialParam {
            public Material Material;
            public int ParamIndex;
            public float ForceFactor;
            public float CurrentValue;

            public MaterialParam(Material mat, int paramId, float changeForceFactor) {
                Material = mat;
                ParamIndex = paramId;
                ForceFactor = changeForceFactor;
            }

            public void Inc(float delta) {
                CurrentValue += delta;
            }
        }

        [Serializable]
        public class PunchOffStuff {
            public string slotGroup = "Helmet";
            public float MinForce = 0.3f;
        }

        public enum UmaExpressionIndex {
            neckUp_Down,
            neckLeft_Right,
            neckTiltLeft_Right,
            headUp_Down,
            headLeft_Right,
            headTiltLeft_Right,
            jawOpen_Close,
            jawForward_Back,
            jawLeft_Right,
            mouthLeft_Right,
            mouthUp_Down,
            mouthNarrow_Pucker,
            tongueOut,
            tongueCurl,
            tongueUp_Down,
            tongueLeft_Right,
            tongueWide_Narrow,
            leftMouthSmile_Frown,
            rightMouthSmile_Frown,
            leftLowerLipUp_Down,
            rightLowerLipUp_Down,
            leftUpperLipUp_Down,
            rightUpperLipUp_Down,
            leftCheekPuff_Squint,
            rightCheekPuff_Squint,
            noseSneer,
            leftEyeOpen_Close,
            rightEyeOpen_Close,
            leftEyeUp_Down,
            rightEyeUp_Down,
            leftEyeIn_Out,
            rightEyeIn_Out,
            browsIn,
            leftBrowUp_Down,
            rightBrowUp_Down,
            midBrowUp_Down,

            leftGrasp,
            rightGrasp,
            leftPeace,
            rightPeace,
            leftRude,
            rightRude,
            leftPoint,
            rightPoint
        }
    }
}