using System;
using DG.Tweening;
using Game.Scripts.Gameplay.LocalMatch;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Score;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Popups.GameOverPopup
{
    public class GameOverPopupView : MonoBehaviour, IPopup
    {
        [SerializeField] private Button _submitButton;
        [SerializeField] private Image _filledImage;

        private GameOverPopupViewModel _vm;
        public event Action OnClose;

        private IFeedbackService _audioService;
        private LocalMatchManager _localMatchManager;
        private ScoreService _scoreService;
        private IUIOverlayService _overlayService;
        private UIRootView _uiRoot;
        private Tween _fillTween;
        private bool _isSubmitting;
        private MapProgressService _mapProgressService;

        [Inject]
        public void Construct(IFeedbackService audioService, IUIOverlayService overlayService, UIRootView uiRoot,
            LocalMatchManager localMatchManager, ScoreService scoreService, MapProgressService mapProgressService)
        {
            _audioService = audioService;
            _overlayService = overlayService;
            _uiRoot = uiRoot;
            _localMatchManager = localMatchManager;
            _scoreService = scoreService;
            _mapProgressService = mapProgressService;
        }

        public void Bind(IPopupData data)
        {
            _vm = data as GameOverPopupViewModel;

            _mapProgressService?.AddCounterPoint();

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);

            _submitButton.onClick.AddListener(() =>
            {
                _fillTween?.Kill();
                _fillTween = null;
                PlayClickSound();
                OnExitButtonClicked();
            });

            _filledImage.fillAmount = 0f;
            _fillTween = _filledImage.DOFillAmount(1f, 10f).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
            {
                OnExitButtonClicked();
            });

            LocalEventLogger.SendEvent("game_over");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_isSubmitting) return;

            if (pauseStatus)
                _fillTween?.Pause();
            else
                _fillTween?.Play();
        }

        private void PlayClickSound()
        {
            if (_audioService == null) return;
            _audioService.PlaySfx("click1");
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
        }

        public void ClosePopup()
        {
            _fillTween?.Kill();
            _fillTween = null;
            OnClose?.Invoke();
            Destroy(gameObject);
        }

        private void OnExitButtonClicked()
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            LocalEventLogger.SendEvent("submit_score_clicked");

            if (_localMatchManager != null && _scoreService != null)
                _localMatchManager.SubmitScore(_scoreService.Score);
        }
    }
}