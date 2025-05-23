using UMA.CharacterSystem;
using UnityEngine;


namespace AncientAnaesthesia {


    /// <summary>
    /// Component tat is responsible for aiming.
    /// </summary>
    public class PunchController : MonoBehaviour {

        [SerializeField]
        private Camera _gameplayCamera;
        [SerializeField]
        private LayerMask _punchableLayers;


        private Vector3? _hitCoordinates;
        private Collider _hitCollider;
        private Vector3 _hitNormal;


        public void SetMouseCoordinates(Vector2 mousePosition) {
            _hitCoordinates = GetHitCoordinates(mousePosition);
        }


        public (Collider hitCollider, Vector3 hitCoordinates, Vector3 hitNormal)? TryPunch() {
            return _hitCoordinates.HasValue ? (_hitCollider, _hitCoordinates.Value, _hitNormal) : null;
        }


        private Vector3? GetHitCoordinates(Vector2 mousePosition) {
            if (_gameplayCamera == null) {
                return default;
            }
            var ray = _gameplayCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, _punchableLayers)) {
                _hitCollider = hitInfo.collider;
                _hitNormal = hitInfo.normal;
                return hitInfo.point;
            } else {
                _hitCollider = null;
                return null;
            }
        }
    }
}