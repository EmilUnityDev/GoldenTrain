using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CollectionPageSwitcher : MonoBehaviour {
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;

    private int _currentPageIndex;
    private bool _isSwitching;

    private void Awake() {
        previousButton.onClick.AddListener(ShowPreviousPage);
        nextButton.onClick.AddListener(ShowNextPage);
        ShowPageInstant(_currentPageIndex);
    }

    private void OnDestroy() {
        previousButton.onClick.RemoveListener(ShowPreviousPage);
        nextButton.onClick.RemoveListener(ShowNextPage);
    }

    public void ShowNextPage() => ShowPage(_currentPageIndex + 1);
    public void ShowPreviousPage() => ShowPage(_currentPageIndex - 1);

    public void ShowPage(int pageIndex) {
        if (_isSwitching || pages.Length == 0)
            return;

        pageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);

        if (pageIndex == _currentPageIndex)
            return;

        StartCoroutine(SwitchPageRoutine(pageIndex));
    }

    private IEnumerator SwitchPageRoutine(int newIndex) {
        _isSwitching = true;
        SetButtonsInteractable(false);

        GameObject oldPage = pages[_currentPageIndex];
        GameObject newPage = pages[newIndex];

        CanvasGroup oldGroup = GetOrAddCanvasGroup(oldPage);
        CanvasGroup newGroup = GetOrAddCanvasGroup(newPage);

        // Страница ещё SetActive(false) — сбрасываем материалы до первого рендера
        PrepareItemsOnPage(newPage);

        newPage.SetActive(true);
        newGroup.alpha = 0f;

        float timer = 0f;
        while (timer < fadeDuration) {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            oldGroup.alpha = 1f - t;
            newGroup.alpha = t;
            yield return null;
        }

        oldGroup.alpha = 0f;
        oldPage.SetActive(false);
        newGroup.alpha = 1f;

        _currentPageIndex = newIndex;
        UpdateButtons();
        SetButtonsInteractable(true);

        NotifyItemsOnPage(newPage);

        _isSwitching = false;
    }

    private void ShowPageInstant(int pageIndex) {
        // Шаг 1: все страницы деактивированы — готовим материалы для ВСЕХ,
        // пока ни одна ещё не рендерилась в этом кадре
        for (int i = 0; i < pages.Length; i++) {
            pages[i].SetActive(false);
            GetOrAddCanvasGroup(pages[i]).alpha = 0f;
        }

        // Шаг 2: готовим материалы всех страниц пока они все выключены
        for (int i = 0; i < pages.Length; i++)
            PrepareItemsOnPage(pages[i]);

        // Шаг 3: только теперь активируем нужную страницу
        pages[pageIndex].SetActive(true);
        GetOrAddCanvasGroup(pages[pageIndex]).alpha = 1f;

        _currentPageIndex = pageIndex;
        UpdateButtons();

        // Шаг 4: запускаем анимацию — материалы уже в 0, моргания не будет
        NotifyItemsOnPage(pages[pageIndex]);
    }

    private void PrepareItemsOnPage(GameObject page) {
        var items = page.GetComponentsInChildren<CollectionItemView>(true);
        foreach (var item in items)
            item.PrepareForAnimation();
    }

    private void NotifyItemsOnPage(GameObject page) {
        var items = page.GetComponentsInChildren<CollectionItemView>(true);
        foreach (var item in items)
            item.Refresh(playAnimation: true);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject page) {
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        if (group == null)
            group = page.AddComponent<CanvasGroup>();
        return group;
    }

    private void UpdateButtons() {
        previousButton.interactable = _currentPageIndex > 0;
        nextButton.interactable = _currentPageIndex < pages.Length - 1;
    }

    private void SetButtonsInteractable(bool interactable) {
        previousButton.interactable = interactable;
        nextButton.interactable = interactable;
    }
}