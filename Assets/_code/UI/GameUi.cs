using R3;
using UnityEngine;
using UnityEngine.UI;

namespace AncientAnaesthesia {

    public class GameUi : MonoBehaviour {

        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private Button _regenerateButton;
        [SerializeField]
        private Button _healButton;
        [SerializeField]
        private Slider _forceSlider;
        [SerializeField]
        private Image _healthImage;
        [SerializeField]
        private Button _exitButton;


        private void Awake() {
            if (_healthImage != null) {
                _gameController.OnPatientHealthChanged01.ToObservable().Subscribe(h => _healthImage.fillAmount = h)
                    .AddTo(this);
            }
            if (_forceSlider != null) {
                _gameController.OnPunchForceChanged01.ToObservable().Subscribe(f => _forceSlider.value = f).AddTo(this);
            }
            if (_regenerateButton != null) {
                _regenerateButton.OnClickAsObservable().Subscribe(_ => _gameController.GeneratePatient()).AddTo(this);
            }
            if (_healButton != null) {
                _healButton.OnClickAsObservable().Subscribe(_ => _gameController.HealPatient()).AddTo(this);
            }
            if (_exitButton != null) {
                _exitButton.OnClickAsObservable().Subscribe(_ => _gameController.Exit()).AddTo(this);
            }
        }
    }
}