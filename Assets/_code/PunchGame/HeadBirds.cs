using UnityEngine;

namespace AncientAnaesthesia {

    public class HeadBirds : MonoBehaviour {

        [SerializeField]
        private AudioClip[] _sounds;

        [SerializeField]
        private Vector2 _sfxMinMaxDelay = new(0.3f, 0.5f);

        [SerializeField]
        private AudioSource[] _audioSourcesPool;

        bool _enabled;
        private float _timer;
        private float _curDelay;

        private void Awake() {
            if (_audioSourcesPool != null) {
                for (int i = 0; i < _audioSourcesPool.Length; i++) {
                    _audioSourcesPool[i].playOnAwake = false;
                    _audioSourcesPool[i].clip = null;
                    _audioSourcesPool[i].loop = false;
                }
            }
        }

        private void OnEnable() {
            _enabled = true;
        }

        private void OnDisable() {
            _enabled = false;
            _timer = 0f;
            _curDelay = 0f;
        }

        void Update() {
            _timer += Time.deltaTime;
            if (_timer > _curDelay) {
                if (PlaySfx()) {
                    _timer = 0f;
                    _curDelay = Random.Range(_sfxMinMaxDelay.x, _sfxMinMaxDelay.y);
                }
            }
        }

        private bool PlaySfx() {
            if (_audioSourcesPool == null) {
                return true;
            }
            for (int i = 0; i < _audioSourcesPool.Length; i++) {
                var aSrc = _audioSourcesPool[i];
                if (!aSrc.isPlaying) {
                    aSrc.PlayOneShot(GetRandomClip());
                    return true;
                }
            }
            return false;
        }

        private AudioClip GetRandomClip() {
            if (_sounds == null) {
                return null;
            }
            return _sounds[Random.Range(0, _sounds.Length)];
        }
    }
}