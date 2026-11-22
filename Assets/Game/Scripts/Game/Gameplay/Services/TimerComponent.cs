using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.Game.Gameplay.Root;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Analytics;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Game.Gameplay.Services
{
    public class TimerComponent : MonoBehaviour
    {
        [Header("Настройки таймера")]
        [SerializeField] private float _durationSingleGame = 120f;
        [SerializeField] private float _durationMultiGame = 120f;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Text _timerLegacyText;
        // [SerializeField] private Image _timerFilledImage;

        private Tween _fillTween;
        private float _remaining;
        private bool _running;
        private bool _timerExpiredInBackground;
        private DateTime _backgroundTime;
        private IPopupService _popupService;
        private IFeedbackService _audioService;
        private GameplayController _gameplayController;
        private SceneContextProvider _sceneContextProvider;
        private IAnalyticEventService _analyticEventService;

        private float _duration;
        private readonly HashSet<int> _sentThresholds = new();

        [Inject]
        public void Construct(IPopupService popupService, IFeedbackService audioService, SceneContextProvider sceneContextProvider, IAnalyticEventService analyticEventService, GameplayController gameplayController = null)
        {
            _popupService = popupService;
            _audioService = audioService;
            _sceneContextProvider = sceneContextProvider;
            _analyticEventService = analyticEventService;
            _gameplayController = gameplayController;
        }

        public void Initialize()
        {
            _duration = _durationMultiGame;
            ResetTimer();
        }

        private void Update()
        {
            if (!_running) return;

            var prevRemaining = _remaining;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                OnTimerComplete();
            }
            
            int prevSeconds = Mathf.CeilToInt(Mathf.Max(0f, prevRemaining));
            int currentSeconds = Mathf.CeilToInt(Mathf.Max(0f, _remaining));
            int[] thresholds = {150, 120, 90, 60};
            foreach (var t in thresholds)
            {
                if (!_sentThresholds.Contains(t) && prevSeconds > t && currentSeconds <= t)
                {
                    var ev = new Dictionary<string, string>
                    {
                        { "current_scene", SceneManager.GetActiveScene().name },
                        { "app_ver", Application.version },
                        { "ts_unix", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                    };
                    Debug.Log($"custom_game_{t}_seconds_timer, current_scene={ev["current_scene"]}, app_ver={ev["app_ver"]}, ts_unix={ev["ts_unix"]}");
                    _analyticEventService?.SendEvent($"game_{t}_seconds_timer", ev);
                    _sentThresholds.Add(t);
                }
            }

            UpdateDisplay(_remaining);
        }

        private void UpdateDisplay(float time)
        {
            var minutes = Mathf.FloorToInt(time / 60f);
            var seconds = Mathf.FloorToInt(time % 60f);

            if (_timerText != null)
                _timerText.text = $"{minutes:00}:{seconds:00}";
            else if (_timerLegacyText != null)
                _timerLegacyText.text = $"{minutes:00}:{seconds:00}";
        }

        public void StartTimer()
        {
            _running = true;

            _fillTween?.Kill();

            // if (_timerFilledImage == null) return;
            //
            // _timerFilledImage.fillAmount = 1f;
            //
            // _fillTween = DOTween
            //     .To(
            //         () => _timerFilledImage.fillAmount,
            //         x => _timerFilledImage.fillAmount = x,
            //         0f,
            //         _durationMultiGame
            //     )
            //     .SetEase(Ease.Linear)
            //     .SetUpdate(true); 
        }

        public void PauseTimer()
        {
            _running = false;
            _fillTween?.Pause();
        }

        public void StopTimer()
        {
            _running = false;
            _fillTween?.Kill();
            _fillTween = null;

            // if (_timerFilledImage != null)
            //     _timerFilledImage.fillAmount = 0f;
        }

        public void ResetTimer()
        {
            _running = false;
            _remaining = _duration;
            UpdateDisplay(_remaining);

            _fillTween?.Kill();
            // if (_timerFilledImage != null)
            //     _timerFilledImage.fillAmount = 1f;
        }

        public void SetDuration(float seconds)
        {
            _duration = Mathf.Max(0f, seconds);
            _remaining = _duration;


            UpdateDisplay(_remaining);
        }

        private void OnTimerComplete()
        {
            if(_sceneContextProvider.IsTutorialScene()) return;
            
            _audioService.PlaySfx("WinSound");
            _gameplayController?.EndGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _backgroundTime = DateTime.UtcNow;
            }
            else
            {
                if (_running)
                {
                    var elapsed = (float)(DateTime.UtcNow - _backgroundTime).TotalSeconds;
                    _remaining -= elapsed;
                    if (_remaining <= 0f)
                    {
                        _remaining = 0f;
                        _running = false;
                        UpdateDisplay(_remaining);
                        OnTimerComplete();
                        return;
                    }
                    UpdateDisplay(_remaining);
                }
            }
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
        }
    }
}