using Game.Scripts.GameRoot.Services.Feedback;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Audio
{
    [RequireComponent(typeof(Button))]
    public class AudioHapticButton : MonoBehaviour
    {
        [SerializeField] private string SfxClipName = "click1";
        [SerializeField] private HapticPatterns.PresetType HapticPreset = HapticPatterns.PresetType.SoftImpact;

        private Button _button;
        private IFeedbackService _audioService;

        [Inject]
        public void Construct(IFeedbackService audioService)
        {
            _audioService = audioService;
        }

        private void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            if (!string.IsNullOrEmpty(SfxClipName) && _audioService != null)
                _audioService.PlaySfx(SfxClipName);
            HapticPatterns.PlayPreset(HapticPreset);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}