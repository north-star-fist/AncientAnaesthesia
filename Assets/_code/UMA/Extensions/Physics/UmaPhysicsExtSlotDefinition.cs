using Sergei.Safonov.Unity.Util;
using System.Collections.Generic;
using UMA;
using UMA.Dynamics;
using UnityEngine;

namespace Sergei.Safonov.UMA {

    public class UmaPhysicsExtSlotDefinition : MonoBehaviour {
        [SerializeField]
        private bool _areTriggersOnStart;
        [SerializeField]
        private bool _areKinematicOnStart;
        [SerializeField]
        private bool _useGravityOnStart;
        [SerializeField]
        private bool _animatorEnabledOnStart;
        [SerializeField]
        private bool _updateWhenOffScreenOnStart;
        [SerializeField, Tooltip("Set this to snap the Avatar to the position of it's hip after ragdoll is finished")]
        private bool _updateTransformAfterRagdoll = true;
        [SerializeField, Tooltip("List of Physics Elements, see UMAPhysicsElement class")]
        private List<UMAPhysicsElement> _physicsElements;
        [SerializeField]
        private bool _setCollidersLayerOnStart;
        [SerializeField, Layer]
        private int _collidersLayerToSet;

        public void SetupPhysicsAvatar(UMAData umaData) {
            UmaPhysicsExtAvatar physicsAvatar = umaData.gameObject.GetOrAddComponent<UmaPhysicsExtAvatar>();
            physicsAvatar.AnimatorEnabledOnStart = _animatorEnabledOnStart;
            physicsAvatar.AreKinematicOnStart = _areKinematicOnStart;
            physicsAvatar.UseGravityOnStart = _useGravityOnStart;
            physicsAvatar.AreTriggersOnStart = _areTriggersOnStart;
            physicsAvatar.UpdateWhenOffScreenOnStart = _updateWhenOffScreenOnStart;
            physicsAvatar.UpdateTransformAfterRagdoll = _updateTransformAfterRagdoll;
            physicsAvatar.elements = _physicsElements;
            physicsAvatar.SetCollidersLayerOnStart = _setCollidersLayerOnStart;
            physicsAvatar.CollidersLayerOnStart = _collidersLayerToSet;
            physicsAvatar.Init();
        }
    }
}