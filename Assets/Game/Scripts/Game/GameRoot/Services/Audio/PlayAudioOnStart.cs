using Game.Scripts.GameRoot.Services.Feedback;
using Lofelt.NiceVibrations;
using UnityEngine;
using VContainer;

public class PlayAudioOnStart : MonoBehaviour {
    [SerializeField] private string SfxClipName = "click1";
    [SerializeField] private HapticPatterns.PresetType HapticPreset = HapticPatterns.PresetType.SoftImpact;

    private IFeedbackService _audioService;

    [Inject]
    public void Construct(IFeedbackService audioService)
    {
        _audioService = audioService;
    }

    private void OnEnable() {
        if (_audioService == null) return;

        _audioService.PlaySfx(SfxClipName);
        HapticPatterns.PlayPreset(HapticPreset);
    }
}
