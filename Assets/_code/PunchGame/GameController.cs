using Coolball;
using R3;
using Sergei.Safonov.UMA;
using Sergei.Safonov.Unity.Util;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace AncientAnaesthesia {

    /// <summary>
    /// Main game component. It manages the game and user input.
    /// </summary>
    public class GameController : MonoBehaviour, Controls.IGameplayActions {

        private const string PatientName = "Patient";
        private const float MaxHealth = 100f;
        // Damage = force01 * force factor
        private const float ForceFactor = 4f;
        // Puch back force on knock out
        private const int KnockOutForce = 100;

        [SerializeField, Range(1f, 10f)]
        private float _forceSliderSpeed = 2;
        [SerializeField]
        private PunchController _puncher;
        [SerializeField]
        private UmaRandomPatient _umaAvatarGenerator;
        [SerializeField]
        private Transform _patientPosition;
        [SerializeField]
        private DamageableArea[] _damageableAreas;
        [SerializeField]
        private AudioSource _hitsAudioSource;
        [SerializeField]
        private string _healthAnimParam = "health";


        public IObservable<bool> PunchesEnabled => _punchesEnabled.AsSystemObservable();
        private readonly ReactiveProperty<bool> _punchesEnabled = new(false);

        public IObservable<float> OnPunchForceChanged01 => _punchForce.AsSystemObservable();
        private readonly ReactiveProperty<float> _punchForce = new(0f);

        public IObservable<float> OnPatientHealthChanged01 => _patientHealth.Select(h => h / MaxHealth).AsSystemObservable();
        private readonly ReactiveProperty<float> _patientHealth = new(MaxHealth);

        private IPatient _patient;
        private Animator _animator;

        private float _startTime;

        private Controls _input;
        private Controls.GameplayActions _gameplayInput;

        private readonly Dictionary<GameObject, IObjectPool<GameObject>> _gameObjectPools = new();

        // Time scale
        private readonly ReactiveProperty<float> _timeScale = new ReactiveProperty<float>(1f);
        private IDisposable _timeScaleSub;
        private float _fixedDeltaFactor;

        private void Awake() {
            _fixedDeltaFactor = Time.fixedDeltaTime / Time.timeScale;
        }

        private void OnEnable() {
            if (_input == null) {
                _input = new Controls();
                _gameplayInput = _input.Gameplay;
                _gameplayInput.SetCallbacks(this);
            }
            _gameplayInput.Enable();
            _timeScaleSub = _timeScale.ObserveOnMainThread().Subscribe(ts => {
                Time.timeScale = ts;
                Time.fixedDeltaTime = _fixedDeltaFactor * ts;
            });
        }

        private void OnDisable() {
            _gameplayInput.RemoveCallbacks(this);
            _gameplayInput.Disable();
            if (_timeScaleSub != null) {
                _timeScaleSub.Dispose();
            }
        }

        void Start() {
            _startTime = Time.time;
            _patientHealth.Subscribe(MonitorPatientHealth).AddTo(this);
            _umaAvatarGenerator.RandomAvatarGenerated.AddListener(HandleNewCharacterGeneration);
            SpawnPatient();
            _punchesEnabled.OnNext(true);
        }

        void Update() {
            _punchForce.Value = (1 + Mathf.Sin(Time.time * _forceSliderSpeed - _startTime)) / 2f;
        }

        private void OnDestroy() {
            if (_umaAvatarGenerator != null) {
                _umaAvatarGenerator.RandomAvatarGenerated.RemoveListener(HandleNewCharacterGeneration);
            }
        }

        public void OnMousePosition(InputAction.CallbackContext context) {
            _puncher.SetMouseCoordinates(context.ReadValue<Vector2>());
        }

        public void OnClick(InputAction.CallbackContext context) {
            if (context.started && _punchesEnabled.Value) {
                (Collider hitCollider, Vector3 hitCoordinates, Vector3 hitNormal)? hit = _puncher.TryPunch();
                if (hit.HasValue) {
                    Punch(hit.Value.hitCoordinates, hit.Value.hitNormal, hit.Value.hitCollider);
                    _patientHealth.Value -= ForceFactor * _punchForce.Value;
                }
            }
        }

        public void HealPatient() {
            _patientHealth.Value = MaxHealth;
            _patient.Heal();
            _punchesEnabled.OnNext(true);
        }

        public void GeneratePatient() {
            _umaAvatarGenerator.transform.DestroyAllChildObjects();
            SpawnPatient();
            _patientHealth.Value = MaxHealth;
            _punchesEnabled.OnNext(true);
        }

        public void Exit() {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        private void SpawnPatient() {
            _umaAvatarGenerator.GenerateRandomCharacter(
                _patientPosition.position,
                _patientPosition.rotation,
                PatientName,
                _umaAvatarGenerator.transform
            );
        }

        private void HandleNewCharacterGeneration(GameObject generator, GameObject generatedAvatar) {
            Animator anim = generatedAvatar.GetComponentInChildren<Animator>();
            _animator = anim;
            if (generatedAvatar.TryGetComponent<UmaDamageableAvatar>(out var damAvatar)) {
                _patient = damAvatar;
            }
        }

        private void MonitorPatientHealth(float health) {
            if (_animator != null && !String.IsNullOrEmpty(_healthAnimParam)) {
                _animator.SetFloat(_healthAnimParam, health / MaxHealth);
            }
            if (health <= 0) {
                _punchesEnabled.OnNext(false);
                _patient.KnockOut((Vector3.forward + Vector3.up) * KnockOutForce);
                SlowTime(.2f, 3f);
            }
        }

        private void Punch(Vector3 hitPosition, Vector3 hitNormal, Collider hitCollider) {
            string areaId = GetHitArea(hitCollider, hitPosition);
            if (areaId == null) {
                return;
            }

            ProcessAreaEffects(areaId, _punchForce.Value, hitPosition, hitNormal);

            // UMA Avatar special updates
            _patient.Damage(areaId, hitPosition, hitNormal, _punchForce.Value);
        }

        private string GetHitArea(Collider hitCollider, Vector3 position) {
            return hitCollider.gameObject.name;
        }

        private void ProcessAreaEffects(string areaId, float force, Vector3? hitPoint, Vector3? hitNormal) {
            if (_damageableAreas == null) {
                return;
            }
            foreach (var wEffects in _damageableAreas) {
                if (wEffects.AreaId != areaId || wEffects.AreaEffects == null) {
                    continue;
                }
                for (int i = 0; i < wEffects.AreaEffects.Length; i++) {
                    var wEffect = wEffects.AreaEffects[i];

                    if (force >= wEffect.MinForce && force < wEffect.MaxForce) {
                        var chance = UnityEngine.Random.value;
                        if (chance >= wEffect.Probability) {
                            continue;
                        }
                        var eff = wEffect.Effect;
                        // SFX
                        PlayHitSound(eff.HitSfx);

                        // Animator
                        processAnimator(eff);

                        SpawnAdditionalObjects(eff.ObjectsToSpawn, hitPoint, hitNormal);

                        if (eff.SetSlowMotion) {
                            SlowTime(eff.SlowMotionTimeScale, eff.SlowMotonTime);
                        }
                    }
                }
            }

            void processAnimator(AreaEffect eff) {
                if (_animator != null) {
                    if (!String.IsNullOrWhiteSpace(eff.AnimForceParam)) {
                        _animator.SetFloat(eff.AnimForceParam, _punchForce.Value);
                    }
                    if (eff.AnimBoolParameters != null) {
                        for (int i = 0; i < eff.AnimBoolParameters.Length; i++) {
                            var boolParam = eff.AnimBoolParameters[i];
                            _animator.SetBool(boolParam.ParamName, boolParam.Value);
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(eff.AnimTriggerParam)) {
                        _animator.SetTrigger(eff.AnimTriggerParam);
                    }
                }
            }
        }

        private void SlowTime(float timeScale, float interval) {
            if (timeScale <= 0f) {
                Debug.LogWarning($"Time scale can be set to positive number only");
                return;
            }
            _timeScale.Value *= timeScale;
            Observable.Timer(TimeSpan.FromSeconds(interval), TimeProvider.System)
                .Subscribe(_ => _timeScale.Value /= timeScale).AddTo(this);
        }

        private void SpawnAdditionalObjects(ObjectToSpawn[] objectsToSpawn, Vector3? hitPoint, Vector3? hitNormal) {
            if (_animator == null) {
                return;
            }
            foreach (var objToSpawn in objectsToSpawn) {
                if (objToSpawn.Prefab != null) {
                    IObjectPool<GameObject> pool = GetGameObjectPool(objToSpawn.Prefab);
                    var effectObj = pool.Get();
                    Transform boneTrans = _animator.GetBoneTransform(objToSpawn.Bone);
                    if (objToSpawn.LinkToBone) {
                        effectObj.transform.parent = boneTrans;
                    }
                    if (objToSpawn.SetNormalDirection && hitNormal.HasValue) {
                        effectObj.transform.rotation = Quaternion.LookRotation(hitNormal.Value);
                    } else {
                        if (objToSpawn.IsLocalRotation) {
                            effectObj.transform.localEulerAngles = objToSpawn.Rotation;
                        } else {
                            effectObj.transform.eulerAngles = objToSpawn.Rotation;
                        }
                    }
                    if (objToSpawn.IsRelativeToHitPoint && hitPoint.HasValue) {
                        if (objToSpawn.IsLocalOffset) {
                            effectObj.transform.localPosition = hitPoint.Value + effectObj.transform.rotation * objToSpawn.Offset;
                        } else {
                            effectObj.transform.position = hitPoint.Value + objToSpawn.Offset;
                        }
                    } else {
                        if (objToSpawn.IsLocalOffset) {
                            effectObj.transform.localPosition = objToSpawn.Offset;
                        } else {
                            effectObj.transform.position = boneTrans.position + objToSpawn.Offset;
                        }
                    }
                    if (objToSpawn.DestroyAfterTime) {
                        var releaser = effectObj.GetOrAddComponent<ReleaseByTime>();
                        releaser.ReleaseAfter(pool, effectObj, objToSpawn.DestroyDelay);
                    }
                }
            }
        }

        private void PlayHitSound(AudioClip sfx) {
            if (_hitsAudioSource != null && sfx != null) {
                _hitsAudioSource.PlayOneShot(sfx);
            }
        }


        #region Pools
        private IObjectPool<GameObject> GetGameObjectPool(GameObject prefab) {
            if (!_gameObjectPools.TryGetValue(prefab, out var pool)) {
                pool = new
                 ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab, transform),
                    actionOnGet: SwitchGameObjectOn,
                    actionOnRelease: SwitchGameObjectOff,
                    actionOnDestroy: Destroy
                );
                _gameObjectPools.Add(prefab, pool);
            }
            return pool;
        }


        private void SwitchGameObjectOn(GameObject obj) {
            obj.gameObject.SetActive(true);
        }

        private void SwitchGameObjectOff(GameObject obj) {
            obj.gameObject.SetActive(false);
        }
        #endregion

        #region Damageable Area Data type
        [Serializable]
        public class DamageableArea {
            public string AreaId;

            public WeightedEffect[] AreaEffects;

            [Serializable]
            public class WeightedEffect {
                public float MinForce;
                public float MaxForce;
                [Range(0f, 1f)]
                public float Probability = 1f;
                public AreaEffect Effect;
            }
        }

        [Serializable]
        public class AreaEffect {
            public string AnimForceParam = "force";
            public string AnimTriggerParam = "hit";
            public AnimBoolParameter[] AnimBoolParameters;
            public AudioClip HitSfx;
            public ObjectToSpawn[] ObjectsToSpawn;
            public bool SetSlowMotion;
            public float SlowMotonTime;
            public float SlowMotionTimeScale;
        }

        [Serializable]
        public class AnimBoolParameter {
            public string ParamName;
            public bool Value;
        }

        [Serializable]
        public class ObjectToSpawn {
            public GameObject Prefab;
            public HumanBodyBones Bone = HumanBodyBones.Head;
            /// <summary>
            /// If it's true offset is relative to hit point (not bone).
            /// </summary>
            public bool IsRelativeToHitPoint;
            public Vector3 Offset = default;
            public bool IsLocalOffset = true;
            /// <summary>
            /// If it's true the object is rotated towards hit normal. It can be useful this for spawning particles.
            /// </summary>
            public bool SetNormalDirection = false;
            public Vector3 Rotation = default;
            public bool IsLocalRotation = true;
            public bool LinkToBone = true;
            public bool DestroyAfterTime = true;
            public float DestroyDelay = 3f;
        }
        #endregion
    }
}