using System.Collections;
using Game.Scripts.Game.Gameplay.Root.View;
using Game.Scripts.Game.Gameplay.Services;
using Game.Scripts.Game.SlideshowGamePopup;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Utils;
using SlotsLogic.Match3Slots.Scripts.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.Gameplay.Root
{
    public class GameplaySceneEntryPoint : IStartable
    {
        private readonly UIGameplayRootBinder _uiPrefab;
        private readonly Camera _camera;
        private readonly SlotGameManager _slotGameManager;
        private readonly UIRootView _uiRoot;
        private readonly IUIOverlayService _overlayService;
        private readonly IPopupService _popupService;
        private readonly Coroutines _coroutines;
        private readonly IObjectResolver _container;
        private readonly IFeedbackService _feedbackService;

        public GameplaySceneEntryPoint(
            UIGameplayRootBinder uiPrefab,
            Camera camera,
            SlotGameManager slotGameManager,
            UIRootView uiRoot,
            IUIOverlayService overlayService,
            IPopupService popupService,
            Coroutines coroutines,
            IObjectResolver container,
            IFeedbackService feedbackService)
        {
            _uiPrefab = uiPrefab;
            _camera = camera;
            _slotGameManager = slotGameManager;
            _uiRoot = uiRoot;
            _overlayService = overlayService;
            _popupService = popupService;
            _coroutines = coroutines;
            _container = container;
            _feedbackService = feedbackService;
        }

        public void Start()
        {
            Debug.Log("[GameplaySceneEntryPoint] Start called, initializing UI and gameplay...");

            _popupService.RegisterSceneResolver(_container);

            _feedbackService.PlayMusic("BgMusic");

            if (_uiPrefab == null)
            {
                Debug.LogError("[GameplaySceneEntryPoint] UI Prefab is NULL!");
                return;
            }

            var uiScene = Object.Instantiate(_uiPrefab);
            _container.Inject(uiScene);
            _container.InjectGameObject(uiScene.gameObject);

            _uiRoot.AttachSceneUI(uiScene.gameObject, _camera);

            var timer = uiScene.GetComponentInChildren<TimerComponent>(true);
            if (timer != null)
                _container.Inject(timer);
            else
                Debug.LogError("[GameplaySceneEntryPoint] TimerComponent not found!");

            if (_slotGameManager != null) _slotGameManager.Initialize(_container);

            if (_slotGameManager != null)
            {
                uiScene.Initialize(_slotGameManager.SkillsService, _slotGameManager.SpinSystem);
                Debug.Log("[GameplaySceneEntryPoint] UI initialized with SkillsService and SpinSystem!");
            }

            var gameplayController = _container.Resolve<GameplayController>();
            if (gameplayController != null && timer != null) gameplayController.SetTimer(timer);

            StartGame(timer);

            Debug.Log("[GameplaySceneEntryPoint] Initialization complete!");
        }

        private void StartGame(TimerComponent timer)
        {
            if (PlayerPrefs.GetInt("TutorialGame", 0) == 0)
            {
                _popupService.ShowPopup<SlideshowGamePopupView>();
                PlayerPrefs.SetInt("TutorialGame", 1);
                PlayerPrefs.Save();
            }
            else
            {
                _overlayService.ShowLoadingWithTransition();
                _coroutines.StartCoroutine(StartGameCor(timer));
            }
        }

        private IEnumerator StartGameCor(TimerComponent timer)
        {
            yield return new WaitForSeconds(3f);
            timer.StartTimer();
            _slotGameManager.StartGame();
        }
    }
}