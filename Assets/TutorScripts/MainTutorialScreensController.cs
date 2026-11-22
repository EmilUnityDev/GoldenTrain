using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Scenes;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Popups.TutorialHintsPopup;
using Game.Scripts.Tutorial.Root.View;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using UnityEngine;
using VContainer;

namespace TutorScripts
{
    public class MainTutorialScreensController : MonoBehaviour
    {
        private SkillsService _skillsService;
        private SlotGameManager _gameManager;
        private SpinSystem.SpinSystem _spinSystem;
        private UITutorialRootBinder _uiTutorialRootBinder;
        private IPopupService _popupService;
        private IUIOverlayService _overlayService;
        private UIRootView _uiRoot;
        private IFeedbackService _feedbackService;
        private ISceneService _sceneService;
        public bool IsGameOver;
        
        [Inject]
        public void Construct(
            SlotGameManager gameManager,
            UITutorialRootBinder uiTutorialRootBinder,
            IPopupService popupService,
            IUIOverlayService overlayService,
            UIRootView uiRoot,
            IFeedbackService feedbackService, ISceneService sceneService)
        {
            _gameManager = gameManager;
            _uiTutorialRootBinder = uiTutorialRootBinder;
            _popupService = popupService;
            _overlayService = overlayService;
            _uiRoot = uiRoot;
            _feedbackService = feedbackService;
            _sceneService = sceneService;
        }

        public void SetUIBinder(UITutorialRootBinder binder)
        {
            _uiTutorialRootBinder = binder;
        }

        private void Start()
        {
            if (_overlayService == null)
            {
                Debug.LogError("[MainTutorialScreensController] _overlayService is null! Cannot show video.");
                return;
            }

            _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                new TutorialHintsViewModel(TutorialHintGroup.FirstGroup)
            );

            _feedbackService?.PlayMusic("BgMusic");
        }

        public void StartRoll()
        {
            _gameManager.SpinSystem.Spin();
            _uiTutorialRootBinder.UpdateButtonsInteractable();
        }

        public void EndTutorial()
        {
            IsGameOver = true;
            LocalEventLogger.SendEvent("ftue_1_completed");
            _overlayService.ShowLoadingWithTransition(null, StartLocalGame);
            _popupService.CloseCurrentPopup();
        }

        public void SkipTutorial()
        {
            IsGameOver = true;
            LocalEventLogger.SendEvent("ftue_1_skipped");
            _popupService.CloseCurrentPopup();
            _overlayService.ShowLoadingWithTransition(null, StartLocalGame);
        }

        private void StartLocalGame()
        {
            _uiRoot.ClearSceneUI();
            _popupService.CloseCurrentPopup();
            _overlayService.HideAllLoading();
            _sceneService.LoadScene(Scenes.GAMEPLAY);
        }
    }
}