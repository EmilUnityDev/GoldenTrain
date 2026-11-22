using System.Collections;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.Services.Popups.MiniGamePopup;
using Game.Scripts.Services.Score;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class LuxuryWagonsBonusView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button wagonButton;

    [Header("Open Button")]
    [SerializeField] private Image openButtonImage;
    [SerializeField] private Sprite openButtonSprite;
    [SerializeField] private Sprite checkButtonSprite;
    [SerializeField] private GameObject txt;

    [Header("Wagon UI")]
    [SerializeField] private Image wagonImage;
    [SerializeField] private Sprite closedWagonSprite;
    
    [Header("Wagon Sprites")]
    [SerializeField] private Sprite[] wagonSprites;

    [Header("Multiplier Inside Wagon")]
    [SerializeField] private GameObject multiplierObject;
    [SerializeField] private Transform wagonDigitsParent;
    [SerializeField] private float wagonDigitsScale = 1f;

    [Header("Top UI")]
    [SerializeField] private TMP_Text totalMultiplierText;

    [Header("Final Result")]
    [SerializeField] private int baseRewardValue = 1;
    [SerializeField] private Transform finalDigitsParent;
    [SerializeField] private float finalDigitsScale = 1.5f;

    [Header("Digit Sprites")]
    [SerializeField] private Image digitPrefab;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("Finish Panels")]
    [SerializeField] private GameObject conductorFinishPanel;
    [SerializeField] private GameObject bonusFinishedPanel;
    
    [Header("Final Result Position")]
    [SerializeField] private RectTransform finalResultContainer;
    [SerializeField] private Vector2 finalOneDigitPosition;
    [SerializeField] private Vector2 finalTwoDigitsPosition;
    [SerializeField] private Vector2 finalThreeDigitsPosition;
    [SerializeField] private MiniGamePopupView tutorialView;
    [SerializeField] private int startReward = 100;
    [SerializeField] private GameObject skipButton;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private float timerDuration = 90f;
    
    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialMultiplierPanel;
    [SerializeField] private GameObject tutorialConductorPanel;
    [SerializeField] private CanvasGroup hintPanel;
    [SerializeField] private Button hintButton;
    [SerializeField] private TutorialHandManager handManager;
    [SerializeField] private GameObject firstPanel;
    
    [Header("Conductor Lose Result")]
    [SerializeField] private TMP_Text conductorMultiplierText;

    private bool _isTutorial;
    private int _tutorialOpenCount;
    private int _tutorialClickCount;

    private Coroutine _timerCoroutine;
    private LuxuryWagonsBonusGame _bonusGame;
    private ScoreService _scoreService;
    private IUIOverlayService _uiOverlayService;

    private IFeedbackService _audioService;



    [Inject]
    public void Construct(ScoreService scoreService,IUIOverlayService uiOverlayService, IFeedbackService audioService)
    {
        _scoreService = scoreService;
        _uiOverlayService = uiOverlayService;
        _audioService = audioService;
    }

    private void Awake()
    {
        _isTutorial = SceneManager.GetActiveScene().name == "Tutorial";
        if (!_isTutorial)
            firstPanel.SetActive(false);
        if (hintButton != null)
            hintButton.onClick.AddListener(CloseHintPanel);
        // StartCoroutine(StartHandAnimation());
        _bonusGame = new LuxuryWagonsBonusGame();

        leftButton.onClick.AddListener(_bonusGame.PreviousWagon);
        rightButton.onClick.AddListener(_bonusGame.NextWagon);
        wagonButton.onClick.AddListener(_bonusGame.OpenCurrentWagon);

        _bonusGame.OnWagonChanged += HandleWagonChanged;
        _bonusGame.OnWagonOpened += HandleWagonOpened;
        _bonusGame.OnMultiplierChanged += HandleMultiplierChanged;
        _bonusGame.OnBonusFinished += HandleBonusFinished;
        
        if (SceneManager.GetActiveScene().name == "Tutorial")
        {
            // if (hintPanel != null)
            //     hintPanel.gameObject.SetActive(true);
        }
        else
        {
            if (skipButton != null)
                skipButton.SetActive(false);
        }
    }

    private IEnumerator StartHandAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        handManager.HandClickAnim(handManager.transform);
    }
    
    private void OnEnable()
    {
        conductorFinishPanel.SetActive(false);
        bonusFinishedPanel.SetActive(false);

        leftButton.gameObject.SetActive(true);
        rightButton.gameObject.SetActive(true);
        wagonButton.gameObject.SetActive(true);

        leftButton.interactable = true;
        rightButton.interactable = true;
        wagonButton.interactable = true;

        ClearDigits(wagonDigitsParent);
        ClearDigits(finalDigitsParent);

        if (_isTutorial)
        {
            _tutorialOpenCount = 0;
            _bonusGame.SetupTutorial(wagonSprites);
        }
        else
        {
            _bonusGame.StartBonus(startReward, wagonSprites);
        }
        StartTimer();

        totalMultiplierText.text = "x0";

        RefreshCurrentWagon();
    }

    private void OnDestroy()
    {
        _bonusGame.OnWagonChanged -= HandleWagonChanged;
        _bonusGame.OnWagonOpened -= HandleWagonOpened;
        _bonusGame.OnMultiplierChanged -= HandleMultiplierChanged;
        _bonusGame.OnBonusFinished -= HandleBonusFinished;
        StopTimer();
    }

    private void HandleWagonChanged(int wagonIndex)
    {
        RefreshCurrentWagon();
    }

    private void HandleWagonOpened(int wagonIndex, WagonContent wagon)
    {
        RefreshCurrentWagon();

        if (!_isTutorial)
            return;

        _tutorialOpenCount++;

        if (_tutorialOpenCount == 1)
        {
            StartCoroutine(ShowTutorialMultiplierPanel());
        }
    }
    
    private IEnumerator ShowTutorialMultiplierPanel()
    {
        CanvasGroup cg = tutorialMultiplierPanel.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = tutorialMultiplierPanel.AddComponent<CanvasGroup>();

        tutorialMultiplierPanel.SetActive(true);

        cg.alpha = 0f;
        tutorialMultiplierPanel.transform.localScale = Vector3.one * 0.8f;

        cg.DOFade(1f, 0.25f);
        tutorialMultiplierPanel.transform
            .DOScale(1f, 0.25f)
            .SetEase(Ease.OutBack);

        float timer = 2f;

        while (timer > 0f)
        {
            if (_tutorialOpenCount >= 2)
                break;

            timer -= Time.deltaTime;
            yield return null;
        }

        cg.DOFade(0f, 0.2f);
        tutorialMultiplierPanel.transform
            .DOScale(0.9f, 0.2f)
            .SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.2f);

        tutorialMultiplierPanel.SetActive(false);
    }

    private void HandleMultiplierChanged(int multiplier)
    {
        totalMultiplierText.text = $"x{multiplier}";
    }

    private void HandleBonusFinished(int multiplier)
    {
        if (_isTutorial)
        {
            StartCoroutine(ShowTutorialConductor());
            return;
        }
        StopTimer();
        leftButton.interactable = false;
        rightButton.interactable = false;
        wagonButton.interactable = false;

        WagonContent currentWagon = _bonusGame.Wagons[_bonusGame.CurrentWagonIndex];

        if (currentWagon.Type == WagonContentType.Conductor)
        {
            StartCoroutine(ShowConductorLosePanel(multiplier));
            _audioService.PlaySfx("miniGameLose");
            _audioService.PlaySfx("manyCoins");

        }
        else
        {
            ShowFinalPanel(multiplier);
            StartCoroutine(CallFinishMethodAfterDelay());
            _audioService.PlaySfx("miniGameWin");
            _audioService.PlaySfx("manyCoins");
        }
    }
    
    private IEnumerator ShowTutorialConductor()
    {
        yield return StartCoroutine(ShowPanelAnimated(tutorialConductorPanel, 3f));

        ShowFinalPanel(_bonusGame.TotalMultiplier);

        yield return new WaitForSeconds(2f);

        StartCoroutine(OnFinalPanelFinished());
    }
    
    private IEnumerator ShowConductorLosePanel(int multiplier)
    {
        conductorFinishPanel.SetActive(true);

        conductorMultiplierText.text = $"x{multiplier}";

        yield return new WaitForSeconds(2f);

        StartCoroutine(OnFinalPanelFinished());
    }

    private IEnumerator CallFinishMethodAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        StartCoroutine(OnFinalPanelFinished());
    }

    private void ShowFinalPanel(int multiplier)
    {
        bonusFinishedPanel.SetActive(true);

        UpdateFinalResultPosition(multiplier);
        DrawNumber(finalDigitsParent, multiplier, finalDigitsScale);

        CanvasGroup canvasGroup = bonusFinishedPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = bonusFinishedPanel.AddComponent<CanvasGroup>();

        canvasGroup.DOKill();
        bonusFinishedPanel.transform.DOKill();

        canvasGroup.alpha = 0f;
        bonusFinishedPanel.transform.localScale = Vector3.one * 0.8f;

        canvasGroup
            .DOFade(1f, 0.25f)
            .SetEase(Ease.OutQuad);

        bonusFinishedPanel.transform
            .DOScale(1f, 0.25f)
            .SetEase(Ease.OutBack);
    }
    
    private void UpdateFinalResultPosition(int value)
    {
        if (finalResultContainer == null)
            return;

        int digitsCount = Mathf.Abs(value).ToString().Length;

        if (digitsCount <= 1)
        {
            finalResultContainer.anchoredPosition = finalOneDigitPosition;
        }
        else if (digitsCount == 2)
        {
            finalResultContainer.anchoredPosition = finalTwoDigitsPosition;
        }
        else
        {
            finalResultContainer.anchoredPosition = finalThreeDigitsPosition;
        }
    }

    private IEnumerator OnFinalPanelFinished()
    {
        Debug.Log("Bonus final panel finished");

        if (_isTutorial)
        {
            yield return StartCoroutine(OnTutorialBonusFinished());
            yield break;
        }

        int finalpoints = startReward * _bonusGame.TotalMultiplier;

        if (_scoreService != null)
            _scoreService.AddBonusPoints(finalpoints);

        yield return new WaitForSeconds(2f);
        if (_uiOverlayService != null)
            _uiOverlayService.ShowTransition();

        yield return new WaitForSeconds(1.3f);
        if (tutorialView != null)
            tutorialView.ClosePopup();
    }
    
    private IEnumerator OnTutorialBonusFinished()
    {
        Debug.Log("Tutorial bonus finished");
        if (_uiOverlayService != null)
            _uiOverlayService.ShowTransition();

        yield return new WaitForSeconds(1.3f);
        if (tutorialView != null)
            tutorialView.ShowFinalHint();
        yield return new WaitForSeconds(3f);

        // StartCoroutine(FinishTutorialBonus());
    }
    
    // private IEnumerator FinishTutorialBonus()
    // {
    //    
    //     
    //     // if (tutorialView != null)
    //     //     tutorialView.StartEndTutorial();
    // }

    private void RefreshCurrentWagon()
    {
        WagonContent wagon = _bonusGame.Wagons[_bonusGame.CurrentWagonIndex];

        wagonImage.sprite = wagon.WagonSprite != null ? wagon.WagonSprite : closedWagonSprite;
        wagonImage.SetNativeSize();

        bool isOpened = wagon.IsOpened;

        wagonButton.interactable = !isOpened;
        openButtonImage.sprite = isOpened ? checkButtonSprite : openButtonSprite;
        openButtonImage.SetNativeSize();
        txt.SetActive(!isOpened);

        ClearDigits(wagonDigitsParent);

        if (!isOpened || wagon.Type != WagonContentType.Multiplier)
        {
            multiplierObject.SetActive(false);
            return;
        }

        multiplierObject.SetActive(true);
        DrawNumber(wagonDigitsParent, wagon.Multiplier, wagonDigitsScale);
    }

    private void DrawNumber(Transform parent, int number, float scale)
    {
        if (parent == null || digitPrefab == null)
            return;

        if (digitSprites == null || digitSprites.Length < 10)
            return;

        ClearDigits(parent);

        string numberText = Mathf.Abs(number).ToString();

        foreach (char symbol in numberText)
        {
            int digit = symbol - '0';

            Image digitImage = Instantiate(digitPrefab, parent);
            digitImage.sprite = digitSprites[digit];
            digitImage.SetNativeSize();
            digitImage.gameObject.SetActive(true);
            digitImage.rectTransform.localScale = Vector3.one * scale;
        }
    }
    
    private void StartTimer()
    {
        if (timerFillImage != null)
            timerFillImage.fillAmount = 1f;
        StopTimer();
        _timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private void StopTimer()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    private IEnumerator TimerRoutine()
    {
        float timeLeft = timerDuration;

        while (timeLeft > 0f && _bonusGame.IsActive && !_bonusGame.IsFinished)
        {
            timeLeft -= Time.deltaTime;

            float normalized = Mathf.Clamp01(timeLeft / timerDuration);

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;

                timerText.text = $"{minutes}:{remainingSeconds:00}";
            }

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = normalized;
            }

            yield return null;
        }

        if (_bonusGame.IsActive && !_bonusGame.IsFinished)
        {
            _bonusGame.FinishByTimer();
        }
    }
    
    public void CloseHintPanel()
    {
        if (hintPanel == null) return;

        hintPanel.blocksRaycasts = false;
        hintPanel.interactable = false;

        hintPanel.DOFade(0f, 0.25f)
            .SetEase(Ease.InCubic);

        hintPanel.transform.DOScale(Vector3.one * 0.8f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                hintPanel.gameObject.SetActive(false);
                hintPanel.transform.localScale = Vector3.one;
            });
    }
    private IEnumerator ShowPanelAnimated(GameObject panel, float showTime)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        panel.SetActive(true);

        canvasGroup.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.8f;

        canvasGroup.DOFade(1f, 0.25f);
        panel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(showTime);

        canvasGroup.DOFade(0f, 0.2f);
        panel.transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.2f);

        panel.SetActive(false);
    }
    
    private void ClearDigits(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}