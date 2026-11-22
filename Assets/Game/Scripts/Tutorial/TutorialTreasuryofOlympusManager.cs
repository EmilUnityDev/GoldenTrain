using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.Services.Score;
using TutorScripts;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

namespace Game.Scripts.Game.MiniGame
{
    public class TutorialTreasuryofOlympusManager : MonoBehaviour
    {
        [Serializable]
        public struct FixedVaseSetup
        {
            public TreasureOfOlympusVaseManager vase;
            public BonusType type;
            [Range(0, 2)] public int moneyIndex;
        }

        [Header("Vases")] [SerializeField] private List<TreasureOfOlympusVaseManager> vases;

        [Header("Fixed Vases")] [SerializeField]
        private List<FixedVaseSetup> fixedVases = new();

        [Header("Tutorial Steps (click order)")] [SerializeField]
        private List<TreasureOfOlympusVaseManager> tutorialClickOrder = new();

        [SerializeField] private bool endGameWhenStepsFinished = true;

        [Header("Flash Settings")] [SerializeField]
        private float flashTargetAlpha = 0.5f;

        [SerializeField] private float flashDuration = 1.0f;
        [SerializeField] private float flashFadeTime = 0.2f;

        [Header("Attempts & Final")] [SerializeField]
        private int maxAttempts = 3;

        [SerializeField] private List<Image> attempts = new();
        [SerializeField] private CanvasGroup finalPanel;
        [SerializeField] private GameObject shineEffect;

        [Header("Final Counter (Sprite Digits)")] [SerializeField]
        private List<Sprite> digitSprites = new(10);

        [SerializeField] private RectTransform digitsRoot;
        [SerializeField] private Image digitPrefab;
        [SerializeField] private float digitSpacing = 8f;
        [SerializeField] private float counterAnimTime = 1.0f;

        [Header("Tutorial Setup")] [SerializeField]
        private CanvasGroup tutorialHint;

        [SerializeField] private TutorialHandManager handHint;
        [SerializeField] private List<GameObject> tutorialHintPos;
        [SerializeField] private CanvasGroup tutorialFinalPanel;
        [SerializeField] private Button battleButton;

        [SerializeField] private GameObject startVFX;


        private int id;

        private readonly List<Image> _digitPool = new();
        private Coroutine _counterRoutine;

        private const int SNAKE_COUNT = 3;
        private readonly int[] moneyRewards = { 2000, 3500, 5000 };

        private int currentWin;
        private bool gameEnded;
        private int attemptsLeft;

        private int _tutorialStep;
        private bool _tutorialModeActive;

        private IUIOverlayService _uiOverlayService;
        private MainTutorialScreensController _tutorialController;
        private IFeedbackService _audioService;
        private ScoreService _scoreService;

        [Inject]
        public void Construct(
            IUIOverlayService uiOverlayService,
            IFeedbackService audioService,
            MainTutorialScreensController tutorialController,
            ScoreService scoreService)
        {
            _uiOverlayService = uiOverlayService;
            _audioService = audioService;
            _tutorialController = tutorialController;
            _scoreService = scoreService;
        }

        private void Start()
        {
            InitAttemptsUI();
            HideFinalPanel();
            HideTutorialFinalPanel();
            SetUpVases();
            SetAllInteractable(false);
            battleButton.onClick.AddListener(AcceptButtonClicked);
        }

        private void AcceptButtonClicked()
        {
            battleButton.interactable = false;
            StartCoroutine(EndTutorialCoroutine());
        }

        private IEnumerator EndTutorialCoroutine()
        {
            if (_uiOverlayService == null || _tutorialController == null)
            {
                Debug.LogError("[TutorialTreasuryofOlympusManager] Missing tutorial services; cannot end tutorial.");
                yield break;
            }

            _uiOverlayService.ShowTransition();
            yield return new WaitForSeconds(1.3f);
            _tutorialController.EndTutorial();
        }

        public void StartTutorGame()
        {
            _tutorialStep = 0;
            _tutorialModeActive = true;
            id = 0;
            HideTutorialFinalPanel();
            StartCoroutine(PlayMemoryFlashThenEnableStep());
        }

        private void OnDestroy()
        {
            if (vases == null) return;
            for (var i = 0; i < vases.Count; i++)
                if (vases[i] != null)
                    vases[i].Clicked -= OnVaseClicked;
        }

        private void InitAttemptsUI()
        {
            attemptsLeft = Mathf.Clamp(maxAttempts, 0, maxAttempts);

            for (var i = 0; i < attempts.Count; i++)
            {
                if (attempts[i] == null) continue;
                attempts[i].gameObject.SetActive(i < maxAttempts);
                if (i < maxAttempts) attempts[i].enabled = true;
            }
        }

        private void ConsumeAttempt()
        {
            if (attemptsLeft <= 0) return;

            var indexToDisable = maxAttempts - attemptsLeft;
            attemptsLeft--;

            if (indexToDisable >= 0 && indexToDisable < attempts.Count && attempts[indexToDisable] != null)
                attempts[indexToDisable].enabled = false;
        }

        private void EndGame()
        {
            if (gameEnded) return;
            gameEnded = true;
            _tutorialModeActive = false;
            SetAllInteractable(false);
            ShowFinalPanel();
        }

        public void SetUpVases()
        {
            if (vases == null || vases.Count == 0) return;

            currentWin = 0;
            gameEnded = false;

            InitAttemptsUI();
            HideFinalPanel();
            HideTutorialFinalPanel();

            var fixedIndices = new HashSet<int>();
            var fixedSnakes = 0;

            for (var i = 0; i < fixedVases.Count; i++)
            {
                if (fixedVases[i].vase == null) continue;
                var idx = vases.IndexOf(fixedVases[i].vase);
                if (idx < 0) continue;

                if (fixedIndices.Add(idx) && fixedVases[i].type == BonusType.Snake)
                    fixedSnakes++;
            }

            var snakesToPlace = Mathf.Clamp(SNAKE_COUNT - fixedSnakes, 0, vases.Count);

            var freeIndices = new List<int>();
            for (var i = 0; i < vases.Count; i++)
                if (!fixedIndices.Contains(i))
                    freeIndices.Add(i);

            snakesToPlace = Mathf.Clamp(snakesToPlace, 0, freeIndices.Count);
            Shuffle(freeIndices);

            var randomSnakeIndices = new HashSet<int>();
            for (var i = 0; i < snakesToPlace; i++)
                randomSnakeIndices.Add(freeIndices[i]);

            for (var i = 0; i < vases.Count; i++)
            {
                var vase = vases[i];
                if (vase == null) continue;

                vase.ResetVisuals();
                vase.SetInteractable(false);

                vase.Clicked -= OnVaseClicked;
                vase.Clicked += OnVaseClicked;

                if (TryApplyFixedSetup(vase, out var fixedSetup))
                {
                    if (fixedSetup.type == BonusType.Snake)
                        vase.Setup(BonusType.Snake);
                    else
                        vase.Setup(BonusType.Money, Mathf.Clamp(fixedSetup.moneyIndex, 0, 2));
                }
                else if (randomSnakeIndices.Contains(i))
                {
                    vase.Setup(BonusType.Snake);
                }
                else
                {
                    vase.Setup(BonusType.Money, Random.Range(0, 3));
                }
            }
        }

        private bool TryApplyFixedSetup(TreasureOfOlympusVaseManager vase, out FixedVaseSetup setup)
        {
            for (var i = 0; i < fixedVases.Count; i++)
                if (fixedVases[i].vase == vase)
                {
                    setup = fixedVases[i];
                    return true;
                }

            setup = default;
            return false;
        }

        private IEnumerator PlayMemoryFlashThenEnableStep()
        {
            SetAllInteractable(false);
            startVFX.SetActive(true);
            for (var i = 0; i < vases.Count; i++)
            {
                if (vases[i]?.prizeImage != null)
                    vases[i].prizeImage.gameObject.SetActive(true);

                vases[i]?.FadeVaseTo(flashTargetAlpha, flashFadeTime);
            }

            yield return new WaitForSeconds(flashDuration);

            for (var i = 0; i < vases.Count; i++)
            {
                if (vases[i]?.prizeImage != null)
                    vases[i].prizeImage.gameObject.SetActive(false);

                vases[i]?.FadeVaseTo(1f, flashFadeTime);
            }

            tutorialHint.DOFade(1, 0.25f).OnComplete(() => StartCoroutine(ContinueGame()));
        }

        private IEnumerator ContinueGame()
        {
            yield return new WaitForSeconds(1.5f);
            tutorialHint.DOFade(0, 0.25f).OnComplete(() => { ApplyTutorialStepInteractable(); });
        }

        public void StartHandAnim()
        {
            if (tutorialHintPos == null || tutorialHintPos.Count == 0) return;

            handHint.GetComponent<CanvasGroup>().DOFade(1, 0.25f);
            var index = Mathf.Clamp(id, 0, tutorialHintPos.Count - 1);
            handHint.HandClickAnim(tutorialHintPos[index].transform);

            id++;
        }

        private void ApplyTutorialStepInteractable()
        {
            if (handHint != null)
                handHint.GetComponent<CanvasGroup>().DOFade(0, 0.25f);


            if (gameEnded)
            {
                SetAllInteractable(false);
                return;
            }

            if (!_tutorialModeActive || tutorialClickOrder == null || tutorialClickOrder.Count == 0)
            {
                SetAllInteractable(true);
                return;
            }

            if (_tutorialStep >= tutorialClickOrder.Count)
            {
                if (endGameWhenStepsFinished) EndGame();
                else SetAllInteractable(true);
                return;
            }

            var allowed = tutorialClickOrder[_tutorialStep];

            for (var i = 0; i < vases.Count; i++)
            {
                var v = vases[i];
                if (v == null) continue;
                v.SetInteractable(v == allowed);
            }

            StartHandAnim();
        }

        private void OnVaseClicked(TreasureOfOlympusVaseManager vase, BonusType type)
        {
            if (handHint != null)
                handHint.GetComponent<CanvasGroup>().DOFade(0, 0.25f);


            if (gameEnded) return;

            if (_tutorialModeActive && tutorialClickOrder != null && tutorialClickOrder.Count > 0)
            {
                var allowed = _tutorialStep >= 0 && _tutorialStep < tutorialClickOrder.Count
                    ? tutorialClickOrder[_tutorialStep]
                    : null;

                if (allowed != null && vase != allowed) return;
            }

            vase?.SetInteractable(false);

            if (type == BonusType.Snake)
            {
                vase?.RevealSnake();
                EndGame();
                return;
            }

            var idx = Mathf.Clamp(vase.MoneyIndex, 0, 2);
            currentWin += moneyRewards[idx];

            vase?.RevealMoney();
            ConsumeAttempt();

            if (_tutorialModeActive && tutorialClickOrder != null && tutorialClickOrder.Count > 0)
            {
                _tutorialStep++;
                ApplyTutorialStepInteractable();
                return;
            }

            if (attemptsLeft <= 0)
                EndGame();
        }

        private void SetAllInteractable(bool value)
        {
            if (vases == null) return;
            for (var i = 0; i < vases.Count; i++)
                vases[i]?.SetInteractable(value && !gameEnded);
        }

        private void ShowFinalPanel()
        {
            if (finalPanel == null) return;

            finalPanel.gameObject.SetActive(true);
            finalPanel.alpha = 1f;
            finalPanel.interactable = true;
            finalPanel.blocksRaycasts = true;
            shineEffect.SetActive(true);
            if (_counterRoutine != null)
                StopCoroutine(_counterRoutine);

            _counterRoutine = StartCoroutine(AnimateWinCounter(currentWin, counterAnimTime));
            _audioService?.PlaySfx("bonusGameWin");
            _scoreService?.AddBonusPoints(currentWin);
        }

        private void HideFinalPanel()
        {
            if (finalPanel == null) return;

            if (_counterRoutine != null)
            {
                StopCoroutine(_counterRoutine);
                _counterRoutine = null;
            }

            finalPanel.alpha = 0f;
            finalPanel.interactable = false;
            finalPanel.blocksRaycasts = false;
            finalPanel.gameObject.SetActive(false);
        }

        private void ShowTutorialFinalPanel()
        {
            if (tutorialFinalPanel == null) return;

            tutorialFinalPanel.gameObject.SetActive(true);
            tutorialFinalPanel.alpha = 1f;
            tutorialFinalPanel.interactable = true;
            tutorialFinalPanel.blocksRaycasts = true;
            //TODO: добавить переход на экран слайдшоу и загрузки
            //battleButton.onClick.AddListener();
        }

        private void HideTutorialFinalPanel()
        {
            if (tutorialFinalPanel == null) return;

            tutorialFinalPanel.alpha = 0f;
            tutorialFinalPanel.interactable = false;
            tutorialFinalPanel.blocksRaycasts = false;
            tutorialFinalPanel.gameObject.SetActive(false);
        }

        private IEnumerator AnimateWinCounter(int targetValue, float duration)
        {
            targetValue = Mathf.Max(0, targetValue);
            var startValue = 0;
            SetNumberSpritesCentered(startValue);

            if (duration <= 0f)
            {
                SetNumberSpritesCentered(targetValue);
                ShowTutorialFinalPanel();
                _counterRoutine = null;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var value = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, Mathf.Clamp01(t / duration)));
                SetNumberSpritesCentered(value);
                yield return null;
            }

            SetNumberSpritesCentered(targetValue);
            ShowTutorialFinalPanel();
            _counterRoutine = null;
        }

        private void SetNumberSpritesCentered(int value)
        {
            var s = Mathf.Max(0, value).ToString();
            EnsureDigitPool(s.Length);

            for (var i = 0; i < _digitPool.Count; i++)
                _digitPool[i].gameObject.SetActive(false);

            for (var i = 0; i < s.Length; i++)
            {
                var img = _digitPool[i];
                img.sprite = digitSprites[s[i] - '0'];
                img.SetNativeSize();
                img.gameObject.SetActive(true);
            }

            var totalWidth = 0f;
            for (var i = 0; i < s.Length; i++)
            {
                var rt = (RectTransform)_digitPool[i].transform;
                totalWidth += rt.rect.width + (i < s.Length - 1 ? digitSpacing : 0f);
            }

            var x = -totalWidth * 0.5f;
            for (var i = 0; i < s.Length; i++)
            {
                var rt = (RectTransform)_digitPool[i].transform;
                rt.anchoredPosition = new Vector2(x + rt.rect.width * 0.5f, 0f);
                x += rt.rect.width + digitSpacing;
            }
        }

        private void EnsureDigitPool(int needed)
        {
            while (_digitPool.Count < needed)
            {
                var img = Instantiate(digitPrefab, digitsRoot);
                img.gameObject.SetActive(false);

                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * 0.5f;
                rt.anchoredPosition = Vector2.zero;

                _digitPool.Add(img);
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var rnd = Random.Range(0, i + 1);
                (list[i], list[rnd]) = (list[rnd], list[i]);
            }
        }
    }
}