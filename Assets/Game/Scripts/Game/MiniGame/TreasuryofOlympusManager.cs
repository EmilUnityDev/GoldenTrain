using System.Collections;
using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.Services.Score;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

namespace Game.Scripts.Game.MiniGame
{
    public enum BonusType
    {
        Snake,
        Money
    }

    public class TreasuryofOlympusManager : MonoBehaviour
    {
        [Header("Vases")] [SerializeField] private List<TreasureOfOlympusVaseManager> vases;

        [Header("Flash Settings")] [SerializeField]
        private float flashTargetAlpha = 0.5f;

        [SerializeField] private float flashDuration = 1.0f;
        [SerializeField] private float flashFadeTime = 0.2f;

        [Header("Attempts & Final")] [SerializeField]
        private int maxAttempts = 3;

        [SerializeField] private List<Image> attempts = new();

        [Header("Attempts Sprites")] [SerializeField]
        private Sprite attemptActiveSprite;

        [SerializeField] private Sprite attemptUsedSprite;

        [SerializeField] private CanvasGroup finalPanel;
        [SerializeField] private Animator animator;

        [Header("Final Counter (Sprite Digits)")] [SerializeField]
        private List<Sprite> digitSprites = new(10);

        [SerializeField] private RectTransform digitsRoot;
        [SerializeField] private Image digitPrefab;
        [SerializeField] private float digitSpacing = 8f;
        [SerializeField] private float counterAnimTime = 1.0f;

        private readonly List<Image> _digitPool = new();
        private Coroutine _counterRoutine;

        private const int SNAKE_COUNT = 3;
        private readonly int[] moneyRewards = { 2000, 3500, 5000 };

        private int currentWin;
        private bool gameEnded;
        private int attemptsLeft;

        private readonly List<BonusType> bonusTypes = new();

        private IFeedbackService _audioService;
        private ScoreService _scoreService;

        [Inject]
        public void Construct(IFeedbackService audioService, ScoreService scoreService)
        {
            _audioService = audioService;
            _scoreService = scoreService;
        }

        private void Start()
        {
            InitAttemptsUI();
            HideFinalPanel();

            SetUpVases();
            StartCoroutine(PlayMemoryFlash());
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

                var isActiveSlot = i < maxAttempts;
                attempts[i].gameObject.SetActive(isActiveSlot);

                if (!isActiveSlot) continue;

                attempts[i].enabled = true;
                attempts[i].sprite = attemptActiveSprite;
            }
        }

        private void ConsumeAttempt()
        {
            if (attemptsLeft <= 0) return;

            var indexToUse = maxAttempts - attemptsLeft;
            attemptsLeft--;

            if (indexToUse >= 0 && indexToUse < attempts.Count && attempts[indexToUse] != null)
            {
                attempts[indexToUse].sprite = attemptUsedSprite;
                attempts[indexToUse].SetNativeSize();
            }
        }

        private void EndGame()
        {
            if (gameEnded) return;
            gameEnded = true;
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
            bonusTypes.Clear();

            var total = vases.Count;

            for (var i = 0; i < Mathf.Min(SNAKE_COUNT, total); i++)
                bonusTypes.Add(BonusType.Snake);

            for (var i = SNAKE_COUNT; i < total; i++)
                bonusTypes.Add(BonusType.Money);

            Shuffle(bonusTypes);

            for (var i = 0; i < vases.Count; i++)
            {
                var vase = vases[i];
                if (vase == null) continue;

                vase.ResetVisuals();
                vase.SetInteractable(true);

                vase.Clicked -= OnVaseClicked;
                vase.Clicked += OnVaseClicked;

                var type = bonusTypes[i];

                if (type == BonusType.Snake)
                {
                    vase.Setup(BonusType.Snake);
                }
                else
                {
                    var moneyIndex = Random.Range(0, 3);
                    vase.Setup(BonusType.Money, moneyIndex);
                }
            }
        }

        private IEnumerator PlayMemoryFlash()
        {
            SetAllInteractable(false);

            for (var i = 0; i < vases.Count; i++)
            {
                if (vases[i] == null) continue;

                if (vases[i].prizeImage != null)
                    vases[i].prizeImage.gameObject.SetActive(true);

                vases[i].FadeVaseTo(flashTargetAlpha, flashFadeTime);
            }

            yield return new WaitForSeconds(flashDuration);

            for (var i = 0; i < vases.Count; i++)
            {
                if (vases[i] == null) continue;

                if (vases[i].prizeImage != null)
                    vases[i].prizeImage.gameObject.SetActive(false);

                vases[i].FadeVaseTo(1f, flashFadeTime);
            }

            SetAllInteractable(true);
        }

        private void OnVaseClicked(TreasureOfOlympusVaseManager vase, BonusType type)
        {
            if (gameEnded) return;

            if (vase != null)
                vase.SetInteractable(false);

            if (type == BonusType.Snake)
            {
                if (vase != null) vase.RevealSnake();
                EndGame();
                return;
            }

            var idx = vase != null ? Mathf.Clamp(vase.MoneyIndex, 0, 2) : 0;
            var added = moneyRewards[idx];
            currentWin += added;

            if (vase != null)
                vase.RevealMoney();

            ConsumeAttempt();

            if (attemptsLeft <= 0)
                EndGame();
        }

        private void SetAllInteractable(bool value)
        {
            if (vases == null) return;
            for (var i = 0; i < vases.Count; i++)
                if (vases[i] != null)
                    vases[i].SetInteractable(value && !gameEnded);
        }

        private void ShowFinalPanel()
        {
            if (finalPanel == null) return;

            finalPanel.gameObject.SetActive(true);
            finalPanel.alpha = 1f;
            finalPanel.interactable = true;
            finalPanel.blocksRaycasts = true;
            animator.SetTrigger("popup");

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

        private IEnumerator AnimateWinCounter(int targetValue, float duration)
        {
            if (digitSprites == null || digitSprites.Count < 10)
                yield break;

            if (digitsRoot == null || digitPrefab == null)
                yield break;

            targetValue = Mathf.Max(0, targetValue);

            var startValue = 0;
            SetNumberSpritesCentered(startValue);

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / duration);
                var value = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, k));
                SetNumberSpritesCentered(value);
                yield return null;
            }

            SetNumberSpritesCentered(targetValue);
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
                var digit = s[i] - '0';
                var img = _digitPool[i];
                img.sprite = digitSprites[digit];
                img.SetNativeSize();
                img.gameObject.SetActive(true);
            }

            var totalWidth = 0f;
            for (var i = 0; i < s.Length; i++)
            {
                var rt = (RectTransform)_digitPool[i].transform;
                totalWidth += rt.rect.width;
                if (i < s.Length - 1)
                    totalWidth += digitSpacing;
            }

            var x = -totalWidth * 0.5f;
            for (var i = 0; i < s.Length; i++)
            {
                var rt = (RectTransform)_digitPool[i].transform;
                var w = rt.rect.width;
                rt.anchoredPosition = new Vector2(x + w * 0.5f, 0f);
                x += w + digitSpacing;
            }
        }

        private void EnsureDigitPool(int needed)
        {
            while (_digitPool.Count < needed)
            {
                var img = Instantiate(digitPrefab, digitsRoot);
                img.gameObject.SetActive(false);

                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
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