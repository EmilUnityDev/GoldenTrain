using UnityEngine;
using VContainer;

public class CollectionScreenView : MonoBehaviour {
    [SerializeField] private CollectionItemView[] items;
    [SerializeField] private CollectionPageProgressView[] pageProgressViews;

    private CollectionUnlockService _collectionUnlockService;

    [Inject]
    private void Construct(CollectionUnlockService collectionUnlockService) {
        _collectionUnlockService = collectionUnlockService;
    }

    private void OnEnable() {
        // FIX: убран RefreshAll() отсюда.
        // Анимацией управляет PageSwitcher через PrepareForAnimation + Refresh(playAnimation: true).
        // Вызов здесь создавал гонку: OnEnable мог выставить FillAmount=1f
        // раньше или позже PrepareForAnimation, и порядок не гарантирован.
    }

    // playAnimation = false: тихое обновление спрайтов без анимации (например, после загрузки сохранения)
    // playAnimation = true: с анимацией fill (например, после разблокировки карточек)
    public void RefreshAll(bool playAnimation = false) {
        foreach (var item in items) {
            if (item != null)
                item.Refresh(playAnimation);
        }

        foreach (var page in pageProgressViews) {
            if (page != null)
                page.Refresh();
        }
    }

    public void UnlockCards(int count) {
        _collectionUnlockService.UnlockRandomCount(count);
        RefreshAll(playAnimation: true); // FIX: разблокировка должна анимироваться
    }
}