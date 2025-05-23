using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UMA.Dynamics;
using UnityEngine;


namespace Sergei.Safonov.UMA {

    /// <summary>
    /// Extended version of UMA out of the box PhysicsAvatar.
    /// </summary>
    public class UmaPhysicsExtAvatar : MonoBehaviour {

        public bool AreTriggersOnStart;

        public bool AreKinematicOnStart;

        public bool UseGravityOnStart;

        public bool SetCollidersLayerOnStart;
        public int CollidersLayerOnStart;

        public bool AnimatorEnabledOnStart;

        public bool UpdateWhenOffScreenOnStart;

        [Tooltip("Set this to snap the Avatar to the position of it's hip after ragdoll is over")]
        public bool UpdateTransformAfterRagdoll = true;

        [Tooltip("List of Physics Elements, see UMAPhysicsElement class")]
        public List<UMAPhysicsElement> elements = new List<UMAPhysicsElement>();


        private DynamicCharacterAvatar _avatar;
        private UMAData _umaData;
        private Rigidbody _rootBone;
        private readonly List<Rigidbody> _rigidbodies = new List<Rigidbody>();

        public List<BoxCollider> BoxColliders => _boxColliders;
        private readonly List<BoxCollider> _boxColliders = new List<BoxCollider>();

        // This is pair list not just sphere list because of Unity Cloth API that delas with such pairs only.
        public List<ClothSphereColliderPair> SphereColliders => _sphereColliders;
        private readonly List<ClothSphereColliderPair> _sphereColliders = new List<ClothSphereColliderPair>();

        public List<CapsuleCollider> CapsuleColliders => _capsuleColliders;
        private readonly List<CapsuleCollider> _capsuleColliders = new List<CapsuleCollider>();


        struct CachedBone {
            public Transform boneTransform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public CachedBone(Transform transform) {
                boneTransform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }
        }
        private readonly List<CachedBone> cachedBones = new List<CachedBone>();

        // Use this for initialization
        void Start() {
            _avatar = GetComponent<DynamicCharacterAvatar>();
            //Using DCS
            if (_avatar != null) {
                _avatar.CharacterCreated.AddListener(OnCharacterCreatedCallback);
                _avatar.CharacterBegun.AddListener(OnCharacterBegunCallback);
                _avatar.CharacterUpdated.AddListener(OnCharacterUpdatedCallback);
            } else {
                //if we're not using the DCS then this will be created through a recipe
                _umaData = gameObject.GetComponent<UMAData>();

                if (_umaData != null) {
                    _umaData.CharacterCreated.AddListener(OnCharacterCreatedCallback);
                    _umaData.CharacterBegun.AddListener(OnCharacterBegunCallback);
                    _umaData.CharacterUpdated.AddListener(OnCharacterUpdatedCallback);
                }
            }
        }

        void OnDestroy() {
            if (_avatar != null) {
                _avatar.CharacterCreated.RemoveListener(OnCharacterCreatedCallback);
                _avatar.CharacterBegun.RemoveListener(OnCharacterBegunCallback);
                _avatar.CharacterUpdated.RemoveListener(OnCharacterUpdatedCallback);
            } else {
                if (_umaData != null) {
                    _umaData.CharacterCreated.RemoveListener(OnCharacterCreatedCallback);
                    _umaData.CharacterBegun.RemoveListener(OnCharacterBegunCallback);
                    _umaData.CharacterUpdated.RemoveListener(OnCharacterUpdatedCallback);
                }
            }
        }


        public void OnCharacterCreatedCallback(UMAData umaData) {
            Init();
        }

        public void Init() {
            if (_umaData == null) {
                _umaData = gameObject.GetComponent<UMAData>();
            }

            if (_umaData == null) {
                if (Debug.isDebugBuild) {
                    Debug.LogError("UMAData is null!", this);
                }
                return;
            }

            for (int i = 0; i < elements.Count; i++) {
                UMAPhysicsElement element = elements[i];
                if (element != null) {
                    // add Generic Info
                    GameObject bone = _umaData.GetBoneGameObject(element.boneName);
                    if (bone == null) {
                        if (Debug.isDebugBuild) {
                            Debug.LogWarning($"{nameof(Init)}: {element.boneName} not found!", this);
                        }
                        continue; //if we don't find the bone then go to the next iteration
                    }

                    if (!bone.TryGetComponent<Rigidbody>(out var rigidBody)) {
                        rigidBody = bone.AddComponent<Rigidbody>();
                    }
                    _rigidbodies.Add(rigidBody);
                    if (element.isRoot) {
                        _rootBone = rigidBody;
                    }
                    rigidBody.isKinematic = AreKinematicOnStart;
                    rigidBody.useGravity = UseGravityOnStart;
                    rigidBody.mass = element.mass;

                    for (int j = 0; j < element.colliders.Length; j++) {
                        ColliderDefinition colDef = element.colliders[j];
                        // Add Appropriate Collider
                        if (colDef.colliderType == ColliderDefinition.ColliderType.Box) {
                            _boxColliders.Add(addToBoneBoxCollider(bone, colDef));
                        } else if (colDef.colliderType == ColliderDefinition.ColliderType.Sphere) {
                            _sphereColliders.Add(new ClothSphereColliderPair(addToBoneSphereCollider(bone, colDef)));
                        } else if (colDef.colliderType == ColliderDefinition.ColliderType.Capsule) {
                            _capsuleColliders.Add(addToBoneCapsuleCollider(bone, colDef));
                        }
                    }
                }
            }

            //Second pass to add joints
            for (int i = 0; i < elements.Count; i++) {
                UMAPhysicsElement element = elements[i];
                if (element == null) {
                    continue;
                }
                GameObject bone = _umaData.GetBoneGameObject(element.boneName);
                if (bone == null) {
                    continue; //if we don't find the bone then go to the next iteration
                }

                // Add Character Joint
                if (!element.isRoot) {
                    // Make Temp SoftJoint
                    CharacterJoint joint = bone.AddComponent<CharacterJoint>();
                    // possible error if parent not yet created.
                    joint.connectedBody = _umaData.GetBoneGameObject(element.parentBone).GetComponent<Rigidbody>();
                    joint.axis = element.axis;
                    joint.swingAxis = element.swingAxis;
                    joint.lowTwistLimit = new SoftJointLimit() { limit = element.lowTwistLimit };
                    joint.highTwistLimit = new SoftJointLimit() { limit = element.highTwistLimit };
                    joint.swing1Limit = new SoftJointLimit() { limit = element.swing1Limit };
                    joint.swing2Limit = new SoftJointLimit() { limit = element.swing2Limit };
                    joint.enablePreprocessing = element.enablePreprocessing;
                }
            }

            UpdateClothColliders();

            SetProperties(
                areTriggers: AreTriggersOnStart,
                areKinematic: AreKinematicOnStart,
                useGravity: UseGravityOnStart,
                setCollidersLayer: SetCollidersLayerOnStart,
                collidersLayer: CollidersLayerOnStart,
                enableAnim: AnimatorEnabledOnStart,
                // Assuming it is not resurrecting in init time
                moveAvatarToRootBonePosition: false,
                updateWhenOffScreen: UpdateWhenOffScreenOnStart
            );

            BoxCollider addToBoneBoxCollider(GameObject bone, ColliderDefinition colDef) {
                BoxCollider boxCollider = bone.AddComponent<BoxCollider>();
                boxCollider.center = colDef.colliderCentre;
                boxCollider.size = colDef.boxDimensions;
                boxCollider.isTrigger = AreTriggersOnStart;
                return boxCollider;
            }

            SphereCollider addToBoneSphereCollider(GameObject bone, ColliderDefinition colDef) {
                SphereCollider sphereCollider = bone.AddComponent<SphereCollider>();
                sphereCollider.center = colDef.colliderCentre;
                sphereCollider.radius = colDef.sphereRadius;
                sphereCollider.isTrigger = AreTriggersOnStart;
                return sphereCollider;
            }

            CapsuleCollider addToBoneCapsuleCollider(GameObject bone, ColliderDefinition colDef) {
                CapsuleCollider capsuleCollider = bone.AddComponent<CapsuleCollider>();
                capsuleCollider.center = colDef.colliderCentre;
                capsuleCollider.radius = colDef.capsuleRadius;
                capsuleCollider.height = colDef.capsuleHeight;
                capsuleCollider.isTrigger = AreTriggersOnStart;
                capsuleCollider.direction = (int)colDef.capsuleAlignment;
                return capsuleCollider;
            }
        }

        public void OnCharacterBegunCallback(UMAData umaData) {
            if (isAnimatorDisabled(umaData)) {
                cachedBones.Clear();
                foreach (int hash in umaData.skeleton.BoneHashes) {
                    Transform boneTransform = umaData.skeleton.GetBoneTransform(hash);
                    if (boneTransform != null) {
                        CachedBone cachedBone = new CachedBone(boneTransform);
                        cachedBones.Add(cachedBone);
                    }
                }
            }
        }

        public void OnCharacterUpdatedCallback(UMAData umaData) {
            if (isAnimatorDisabled(umaData)) {
                foreach (var cachedbone in cachedBones) {
                    cachedbone.boneTransform.localPosition = cachedbone.localPosition;
                    cachedbone.boneTransform.localRotation = cachedbone.localRotation;
                    cachedbone.boneTransform.localScale = cachedbone.localScale;
                }
                cachedBones.Clear();
            }
        }

        public void SetProperties(
            bool areTriggers,
            bool areKinematic,
            bool useGravity,
            bool setCollidersLayer,
            int collidersLayer,
            bool enableAnim,
            bool moveAvatarToRootBonePosition = false,
            bool updateWhenOffScreen = false
        ) {
            // iterate through all rigidbodies and switch kinematic mode on/off
            //Set all rigidbodies.isKinematic to opposite of ragdolled state
            TuneRigidbodies(areKinematic, useGravity);
            TuneColliders(areTriggers, setCollidersLayer, collidersLayer);
            // switch animator on/off
            if (_umaData.animator != null) {
                _umaData.animator.enabled = enableAnim;
            }

            // Prevent Mismatched Culling
            // Skinned mesh renderers cull based on their origonal position before ragdolling.
            // We use this property to prevent ragdolled meshes from popping in and out unexpectedly.
            SetUpdateWhenOffscreen(updateWhenOffScreen);

            if (moveAvatarToRootBonePosition) {
                //We were ragdolled and now we're not
                gameObject.transform.position = _rootBone.transform.position;
            }
        }

        public void AddForce(Vector3 force, Vector3 position, ForceMode forceMode = default) {
            if (_rootBone != null) {
                _rootBone.AddForceAtPosition(force, position, forceMode);
            }
        }

        //Update all cloth components
        public void UpdateClothColliders() {
            if (_umaData == null) {
                return;
            }
            SkinnedMeshRenderer[] umaRenderers = _umaData.GetRenderers();
            foreach (var renderer in umaRenderers) {
                if (!renderer.TryGetComponent<Cloth>(out var cloth)) {
                    continue;
                }
                cloth.sphereColliders = _sphereColliders.ToArray();
                cloth.capsuleColliders = _capsuleColliders.ToArray();
                if (Debug.isDebugBuild && (cloth.capsuleColliders.Length + cloth.sphereColliders.Length) > 10) {
                    Debug.LogWarning(
                        "Cloth Collider count is high." +
                        " You might experience strange behavior with the cloth simulation."
                    );
                }
            }
        }

        private void TuneRigidbodies(bool kinematic, bool useGravity) {
            foreach (var rigidbody in _rigidbodies) {
                if (rigidbody != null) {
                    rigidbody.isKinematic = kinematic;
                    rigidbody.useGravity = useGravity;
                }
            }
        }

        private void TuneColliders(bool trigger, bool setCollidersLayer, int layer) {
            foreach (var collider in _boxColliders) {
                collider.isTrigger = trigger;
                setColliderLayer(collider, layer, setCollidersLayer);
            }
            foreach (var collider in _sphereColliders) {
                collider.first.isTrigger = trigger;
                setColliderLayer(collider.first, layer, setCollidersLayer);
            }
            foreach (var collider in _capsuleColliders) {
                collider.isTrigger = trigger;
                setColliderLayer(collider, layer, setCollidersLayer);
            }

            static void setColliderLayer(Collider collider, int layer, bool setCollidersLayer) {
                if (setCollidersLayer) {
                    collider.gameObject.layer = layer;
                }
            }
        }

        private void SetUpdateWhenOffscreen(bool updateWhenOffScreen) {
            if (_umaData == null) {
                return;
            }
            SkinnedMeshRenderer[] renderers = _umaData.GetRenderers();
            if (renderers != null) {
                foreach (var renderer in renderers) {
                    renderer.updateWhenOffscreen = updateWhenOffScreen;
                }
            }
        }

        private static bool isAnimatorDisabled(UMAData umaData) {
            return umaData == null || umaData.animator == null || !umaData.animator.enabled;
        }
    }
}
