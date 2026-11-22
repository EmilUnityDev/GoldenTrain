using System;
using UnityEngine;
using UnityEngine.UI;

public class CityMapPoint : MonoBehaviour
{
    [SerializeField] private int requiredProgress;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private bool isStart;

    [Header("City Panel Data")]
    [SerializeField] private Sprite cityPanelSprite;

    public int RequiredProgress => requiredProgress;
    public Sprite CityPanelSprite => cityPanelSprite;
    public bool IsFirstCity => requiredProgress == 1;

    public bool IsUnlocked { get; private set; }

    private Action<CityMapPoint> _onClick;

    public void Init(int playerProgress, Action<CityMapPoint> onClick)
    {
        _onClick = onClick;

        IsUnlocked = playerProgress >= requiredProgress;

        button.interactable = IsUnlocked;
        buttonImage.sprite = IsUnlocked ? activeSprite : lockedSprite;

        button.onClick.RemoveAllListeners();

        if (isStart)
            return;

        button.onClick.AddListener(() =>
        {
            if (!IsUnlocked)
                return;

            _onClick?.Invoke(this);
        });
    }
}