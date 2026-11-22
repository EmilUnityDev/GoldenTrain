using System;
using Game.Scripts.Game.Gameplay.Root;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Game.SlideshowGamePopup
{
    public class SlideshowGamePopupView : MonoBehaviour, IPopup
    {
        private SlideshowGamePopupViewModel _vm;

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipButtonText;
        public event Action OnClose;

        private SceneContextProvider _sceneContextProvider;
        private IUIOverlayService _overlayService;
        private GameplayController _gameplayController;
        private Coroutines _coroutines;

        private void EnsureServices()
        {
            if (_gameplayController == null)
                _gameplayController = FindObjectOfType<GameplayController>();
        }

        [Inject]
        public void Construct(IUIOverlayService overlayService, SceneContextProvider sceneContextProvider,
            Coroutines coroutines)
        {
            _overlayService = overlayService;
            _sceneContextProvider = sceneContextProvider;
            _coroutines = coroutines;
        }

        public void Bind(IPopupData data)
        {
            EnsureServices();
            _vm = data as SlideshowGamePopupViewModel;

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);

            var isMainMenu = _sceneContextProvider != null && _sceneContextProvider.IsMainMenuScene();

            if (isMainMenu)
            {
                _playButton.gameObject.SetActive(false);
                _skipButton.image.color = new Color(255f, 255f, 255f, 255f);
                _skipButton.gameObject.SetActive(true);
                var skipButtonRect = _skipButton.GetComponent<RectTransform>();
                if (skipButtonRect != null)
                {
                    skipButtonRect.anchorMin = new Vector2(0f, 1f);
                    skipButtonRect.anchorMax = new Vector2(0f, 1f);
                    skipButtonRect.pivot = new Vector2(0.5f, 0.5f);
                    skipButtonRect.anchoredPosition = new Vector2(193f, -175f);
                }

                _skipButtonText.text = "";
            }

            _skipButton.onClick.AddListener(() =>
            {
                LocalEventLogger.SendEvent("slideshow_skipped");
                PlayClickSound();
                ClosePopup();
            });

            _playButton.onClick.AddListener(() =>
            {
                LocalEventLogger.SendEvent("slideshow_completed");
                PlayClickSound();
                ClosePopup();
            });
        }

        private void PlayClickSound()
        {
        }

        private void ClaimPrize()
        {
        }

        public void ClosePopup()
        {
            PopupAnimationHandler.PlayCloseAnimation(gameObject, () =>
            {
                OnClose?.Invoke();
                EnsureServices();
                if (_sceneContextProvider != null && !_sceneContextProvider.IsMainMenuScene())
                {
                    _overlayService?.ShowLoadingWithTransition();
                    if (_gameplayController == null)
                        Debug.LogWarning("[SlideshowGamePopupView] GameplayController not found; cannot start game.");
                    else if (_coroutines != null) _coroutines.StartCoroutine(_gameplayController.StartGameCor());
                }

                Destroy(gameObject);
            });
        }
    }
}