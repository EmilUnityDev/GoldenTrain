using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

namespace Game.Scripts.Services.Popups.MegaWinPopup
{
    public class MegaWinPopupView : MonoBehaviour, IPopup
    {
        [Header("Win Panels")]
        [SerializeField] private GameObject bigWinPanel;
        [SerializeField] private GameObject megaWinPanel;
        [SerializeField] private GameObject superWinPanel;

        [Header("Settings")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private float appearDuration = 0.35f;

        [Header("Digits")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];
        [SerializeField] private RectTransform digitsRow;
        [SerializeField] private Image digitPrefab;
        [SerializeField] private float digitSpacing = 140f;

        [SerializeField] private float rollDuration = 0.85f;
        [SerializeField] private float perDigitDelay = 0.07f;
        [SerializeField] private float rollFPS = 26f;
        [SerializeField] private float settlePunch = 1.08f;
        [SerializeField] private float settlePunchTime = 0.14f;
        [SerializeField] private int initialPool = 8;

        [Header("Auto Close")]
        [SerializeField] private float autoCloseDelay = 4f;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;


        private readonly List<Image> _pool = new();
        private readonly List<Image> _active = new();

        private MegaWinPopupViewModel vm;
        private IFeedbackService audioService;

        private Coroutine coinsRoutine;
        private Coroutine digitsRoutine;
        private Coroutine autoCloseRoutine;

        public event Action OnClose;

        [Inject]
        public void Construct(IFeedbackService audioService)
        {
            this.audioService = audioService;
        }

        public void Bind(IPopupData data = null)
        {
            vm = data as MegaWinPopupViewModel;

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);
            LocalEventLogger.SendEvent("big_win_shown");

            InitializePanelGroup();
            SetWinVisuals(ResolveMegaWinType(vm?.Value ?? 0));

            if (digitPrefab != null && digitPrefab.gameObject.activeSelf)
                digitPrefab.gameObject.SetActive(false);

            EnsurePool(initialPool);

            closeButton?.onClick.RemoveListener(ClosePopup);
            closeButton?.onClick.AddListener(ClosePopup);

            if (closeButton != null)
                closeButton.interactable = false;

            StartCoroutine(ShowRoutine(vm?.Value ?? 0));

            if (autoCloseRoutine != null)
                StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
        }

        private void InitializePanelGroup()
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            panelGroup.gameObject.SetActive(true);
        }

        private MegaWinType ResolveMegaWinType(int value)
        {
            if (value >= 14000) return MegaWinType.MegaWin;
            if (value >= 7000) return MegaWinType.SuperWin;
            return MegaWinType.BigWin;
        }

        private void SetWinVisuals(MegaWinType type)
        {
            bigWinPanel?.SetActive(type == MegaWinType.BigWin);
            megaWinPanel?.SetActive(type == MegaWinType.MegaWin);
            superWinPanel?.SetActive(type == MegaWinType.SuperWin);
        }

        private IEnumerator ShowRoutine(int value)
        {
            panelGroup.DOFade(1f, appearDuration).SetUpdate(true);
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;

            transform.localScale = Vector3.one * 0.92f;
            transform.DOScale(1f, appearDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            yield return new WaitForSecondsRealtime(appearDuration);

            StartDigitsRoll(value);
           
        }

        private void StartDigitsRoll(int value)
        {
            if (digitsRoutine != null)
                StopCoroutine(digitsRoutine);

            digitsRoutine = StartCoroutine(DigitsRollRoutine(value));
        }

        private IEnumerator DigitsRollRoutine(int value)
        {
            if (value < 0)
                value = 0;

            string valueString = value.ToString();

            PrepareActiveDigits(valueString.Length);

            for (int i = 0; i < _active.Count; i++)
            {
                int finalDigit = valueString[i] - '0';
                StartCoroutine(RollDigit(_active[i], finalDigit, i * perDigitDelay));
            }

            float totalTime = perDigitDelay * Mathf.Max(0, _active.Count - 1)
                              + rollDuration
                              + settlePunchTime
                              + 0.05f;

            yield return new WaitForSecondsRealtime(totalTime);

            if (closeButton != null)
                closeButton.interactable = true;

            digitsRoutine = null;
        }

        private void EnsurePool(int count)
        {
            if (digitPrefab == null || digitsRow == null)
                return;

            while (_pool.Count < count)
            {
                var digit = Instantiate(digitPrefab, digitsRow);
                digit.gameObject.SetActive(false);
                _pool.Add(digit);
            }
        }

        private void PrepareActiveDigits(int needed)
        {
            foreach (var img in _active)
            {
                if (img != null)
                    img.gameObject.SetActive(false);
            }

            _active.Clear();

            EnsurePool(needed);

            float totalWidth = (needed - 1) * digitSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < needed; i++)
            {
                var img = _pool[i];

                img.gameObject.SetActive(true);
                img.transform.localScale = Vector3.one;

                img.sprite = digitSprites != null && digitSprites.Length > 0
                    ? digitSprites[0]
                    : null;

                RectTransform rt = img.rectTransform;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                rt.sizeDelta = new Vector2(160, 220);

                float x = startX + i * digitSpacing;
                rt.anchoredPosition = new Vector2(x, 0f);

                _active.Add(img);
            }

            for (int i = needed; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    _pool[i].gameObject.SetActive(false);
            }
        }

        private void ApplyNativeSizeToLayout(Image img)
        {
            if (img == null)
                return;

            var layout = img.GetComponent<LayoutElement>();
            if (layout == null)
                layout = img.gameObject.AddComponent<LayoutElement>();

            if (img.sprite != null)
            {
                layout.preferredWidth = 160f;
                layout.preferredHeight = 220f;
            }
            else
            {
                layout.preferredWidth = 0f;
                layout.preferredHeight = 0f;
            }

            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private IEnumerator RollDigit(Image img, int finalDigit, float delay)
        {
            if (img == null)
                yield break;

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            float frameTime = 1f / Mathf.Max(1f, rollFPS);
            float frameTimer = 0f;

            while (elapsed < rollDuration)
            {
                float dt = Time.unscaledDeltaTime;

                elapsed += dt;
                frameTimer += dt;

                if (frameTimer >= frameTime)
                {
                    frameTimer = 0f;

                    int randomDigit = Random.Range(0, 10);

                    if (digitSprites != null && randomDigit < digitSprites.Length)
                    {
                        img.sprite = digitSprites[randomDigit];
                        ApplyNativeSizeToLayout(img);
                    }
                }

                yield return null;
            }

            finalDigit = Mathf.Clamp(finalDigit, 0, 9);

            if (digitSprites != null && finalDigit < digitSprites.Length)
            {
                img.sprite = digitSprites[finalDigit];
                ApplyNativeSizeToLayout(img);
            }

            img.transform
                .DOPunchScale(Vector3.one * (settlePunch - 1f), settlePunchTime, 1, 0.75f)
                .SetUpdate(true);
        }


        private void StopCoinsLoop()
        {
            if (coinsRoutine != null)
            {
                StopCoroutine(coinsRoutine);
                coinsRoutine = null;
            }
        }


        public void ClosePopup()
        {
            StopCoinsLoop();

            if (autoCloseRoutine != null)
            {
                StopCoroutine(autoCloseRoutine);
                autoCloseRoutine = null;
            }

            if (digitsRoutine != null)
            {
                StopCoroutine(digitsRoutine);
                digitsRoutine = null;
            }

            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            PopupAnimationHandler.PlayCloseAnimation(gameObject, () =>
            {
                OnClose?.Invoke();
                Destroy(gameObject);
            });
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSecondsRealtime(autoCloseDelay);
            autoCloseRoutine = null;
            ClosePopup();
        }
    }
}