using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class CollectionPageProgressView : MonoBehaviour
{
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text unlockedCountText;
    [SerializeField] private TMP_Text earnedGoldText;
    [SerializeField] private int goldPerCard = 200;

    private CollectionItemView[] _items;
    
    private CollectionUnlockService _collectionUnlockService;
    
    [Inject]
    private void Construct(CollectionUnlockService collectionUnlockService)
    {
        _collectionUnlockService = collectionUnlockService;
    }

    private void Awake()
    {
        _items = GetComponentsInChildren<CollectionItemView>(true);
    }

    private void OnEnable()
    {
        _collectionUnlockService.OnItemUnlocked += HandleItemUnlocked;
        Refresh();
    }

    private void OnDisable()
    {
        if (_collectionUnlockService != null)
            _collectionUnlockService.OnItemUnlocked -= HandleItemUnlocked;
    }

    public void Refresh()
    {
        if (_items == null || _items.Length == 0)
            _items = GetComponentsInChildren<CollectionItemView>(true);

        int total = _items.Length;
        int unlocked = 0;

        foreach (var item in _items)
        {
            if (item != null && item.IsUnlocked)
                unlocked++;
        }

        float progress = total > 0 ? (float)unlocked / total : 0f;
        int percent = Mathf.RoundToInt(progress * 100f);
        int earnedGold = unlocked * goldPerCard;

        if (progressFillImage != null)
            progressFillImage.fillAmount = progress;

        if (progressText != null)
            progressText.text = $"{percent}%";

        if (unlockedCountText != null)
            unlockedCountText.text = $"{unlocked}/{total}";

        if (earnedGoldText != null)
            earnedGoldText.text = earnedGold.ToString();
    }

    private void HandleItemUnlocked(string key)
    {
        Refresh();
    }
}