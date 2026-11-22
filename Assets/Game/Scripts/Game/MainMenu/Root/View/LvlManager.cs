using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LvlManager : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text levelText;

    [SerializeField] private float fillAnimationDuration = 0.35f;

    private LevelProgressService _levelProgressService;
    private Tween _fillTween;

    [Inject]
    private void Construct(LevelProgressService levelProgressService)
    {
        _levelProgressService = levelProgressService;
    }

    private void OnEnable()
    {
        if (_levelProgressService == null)
            _levelProgressService = LevelProgressService.Instance;

        LevelProgressService.OnAnyProgressChanged -= HandleProgressChanged;
        LevelProgressService.OnAnyProgressChanged += HandleProgressChanged;

        UpdateViewInstant(
            _levelProgressService.Level,
            _levelProgressService.Experience
        );
    }

    private void OnDisable()
    {
        LevelProgressService.OnAnyProgressChanged -= HandleProgressChanged;
        _fillTween?.Kill();
    }

    private void HandleProgressChanged(int level, float experience)
    {
        UpdateViewAnimated(level, experience);
    }

    private void UpdateViewInstant(int level, float experience)
    {
        float fillValue = Mathf.Clamp01(experience / 100f);

        if (fillImage != null)
            fillImage.fillAmount = fillValue;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(experience)}%";

        if (levelText != null)
            levelText.text = $"{level}";
    }

    private void UpdateViewAnimated(int level, float experience)
    {
        float targetFill = Mathf.Clamp01(experience / 100f);

        if (levelText != null)
            levelText.text = $"{level}";

        _fillTween?.Kill();

        if (fillImage == null)
            return;

        _fillTween = fillImage
            .DOFillAmount(targetFill, fillAnimationDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                if (percentText != null)
                    percentText.text = $"{Mathf.RoundToInt(fillImage.fillAmount * 100f)}%";
            });
    }
}