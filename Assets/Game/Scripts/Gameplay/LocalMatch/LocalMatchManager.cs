using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Analytics;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.GameRoot.Services.Scenes;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Gameplay.LocalMatch
{
    public class LocalMatchManager : MonoBehaviour
    {
        [HideInInspector] public float submittedScore;
        [HideInInspector] public bool hasSubmittedScore;

        [SerializeField] private GameObject _submitOverlay;

        private IAnalyticEventService _eventService;
        private IUIOverlayService _overlayService;
        private IFeedbackService _feedbackService;
        private IPopupService _popupService;
        private UIRootView _uiRoot;
        private ISceneService _sceneService;
        private bool _hasSubmitted;

        [Inject]
        public void Construct(IAnalyticEventService eventService, IUIOverlayService overlayService,
            IFeedbackService feedbackService, IPopupService popupService, UIRootView uiRoot,
            ISceneService sceneService)
        {
            _eventService = eventService;
            _overlayService = overlayService;
            _feedbackService = feedbackService;
            _popupService = popupService;
            _uiRoot = uiRoot;
            _sceneService = sceneService;
        }

        private void Start()
        {
            if (_submitOverlay != null) _submitOverlay.SetActive(false);
        }

        public void SubmitScore(float score)
        {
            if (_hasSubmitted) return;
            _hasSubmitted = true;
            submittedScore = score;
            hasSubmittedScore = true;

            var roundedScore = Mathf.RoundToInt(score);
            var count = PlayerPrefs.GetInt("LocalMatchCount", 0);
            var total = PlayerPrefs.GetFloat("LocalMatchScoreTotal", 0f);
            var best = PlayerPrefs.GetInt("HighScore", 0);
            PlayerPrefs.SetInt("LocalMatchCount", count + 1);
            PlayerPrefs.SetFloat("LocalMatchScoreTotal", total + score);
            PlayerPrefs.SetInt("HighScore", Mathf.Max(best, roundedScore));
            PlayerPrefs.Save();
            _eventService?.SendEvent(AnalyticEventType.ScoreSaved);

            _feedbackService?.StopMusic();
            _feedbackService?.StopAllLoopingSfx();
            if (_submitOverlay != null) _submitOverlay.SetActive(true);

            if (_overlayService != null)
                _overlayService.ShowLoadingWithTransition(null, ReturnToMenu);
            else
                ReturnToMenu();
        }

        public void AbortMatch()
        {
            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            _popupService?.CloseCurrentPopup();
            _uiRoot?.ClearSceneUI();
            _sceneService?.LoadScene(Scenes.MAIN_MENU);
        }
    }
}
