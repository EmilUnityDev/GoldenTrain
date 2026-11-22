using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Services.Score.View
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private bool _isCurrentScore;
        [SerializeField] private bool _isBonusScore;
        [SerializeField] private bool _isGameScore;
        [SerializeField] private bool _isFutureScore;

        [SerializeField] private ParticleSystem _flyCoinsPS2;

        private ScoreService _scoreService;
        private IFeedbackService _audioService;
        private Tween _countTween;
        private int _currentDisplayedScore;

        [Inject]
        public void Construct(IFeedbackService audioService, ScoreService scoreService)
        {
            _audioService = audioService;
            _scoreService = scoreService;
        }

        private void Start()
        {
            if (_scoreService != null)
            {
                if (_isCurrentScore)
                {
                    _scoreService.OnCurrentSpinScoreChanged += OnScoreChanged;
                    UpdateScoreText(_scoreService.CurrentSpinScore);
                }
                else if (_isBonusScore)
                {
                    _scoreService.OnBonusScoreChanged += OnScoreChanged;
                    UpdateScoreText(_scoreService.BonusScore);
                }
                else if (_isGameScore)
                {
                    _scoreService.OnGameScoreChanged += OnScoreChanged;
                    UpdateScoreText(_scoreService.GameScore);
                }
                else if (_isFutureScore)
                {
                    _scoreService.OnFutureScoreChanged += OnScoreChanged;
                    UpdateScoreText(_scoreService.FutureScore);
                }
                else
                {
                    _scoreService.OnScoreChanged += OnScoreChanged;
                    UpdateScoreText(_scoreService.Score);
                }
            }
            else
            {
                ApplyText(0);
            }
        }

        private void OnScoreChanged(int newScore)
        {
            UpdateScoreText(newScore);
        }

        private void UpdateScoreText(int newScore)
        {
            if (_scoreText == null) return;

            if (_isCurrentScore && newScore == 0)
            {
                AnimateToZero();
                return;
            }

            _countTween?.Kill();

            var start = _currentDisplayedScore;
            var target = newScore;

            _countTween = DOVirtual.Int(start, target, 1f, val => { _scoreText.text = val.ToString(); })
                .SetEase(Ease.OutCubic).OnComplete(() => _currentDisplayedScore = target);
        }

        private void AnimateToZero()
        {
            if (_scoreText == null) return;

            if (_countTween != null && _countTween.IsActive())
            {
                _countTween.OnComplete(StartAnimationToZero);
                return;
            }

            StartAnimationToZero();
        }

        private void StartAnimationToZero()
        {
            if (_scoreText == null) return;

            var currentValue = 0;
            if (int.TryParse(_scoreText.text, out var parsed))
                currentValue = parsed;
            else
                currentValue = _currentDisplayedScore;

            if (currentValue == 0)
            {
                _currentDisplayedScore = 0;
                _scoreText.text = "0";
                return;
            }


            _flyCoinsPS2?.Play();

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
            _audioService?.PlaySfx("coins");
            _countTween = DOVirtual.Int(currentValue, 0, 1f, val => { _scoreText.text = val.ToString(); })
                .SetEase(Ease.OutCubic).OnComplete(() => _currentDisplayedScore = 0);
        }

        private void ApplyText(int score)
        {
            if (_scoreText == null) return;
            _scoreText.text = score.ToString();
            _currentDisplayedScore = score;
        }

        private string FormatScore(int score)
        {
            if (score < 1000) return score.ToString();

            if (score < 1000000)
            {
                var kScore = score / 1000f;
                return kScore % 1 == 0 ? $"{(int)kScore}k" : $"{kScore:F1}k";
            }

            var mScore = score / 1000000f;
            return mScore % 1 == 0 ? $"{(int)mScore}m" : $"{mScore:F1}m";
        }

        private void OnDestroy()
        {
            if (_scoreService != null)
            {
                if (_isCurrentScore)
                    _scoreService.OnCurrentSpinScoreChanged -= OnScoreChanged;
                else if (_isBonusScore)
                    _scoreService.OnBonusScoreChanged -= OnScoreChanged;
                else if (_isGameScore)
                    _scoreService.OnGameScoreChanged -= OnScoreChanged;
                else if (_isFutureScore)
                    _scoreService.OnFutureScoreChanged -= OnScoreChanged;
                else
                    _scoreService.OnScoreChanged -= OnScoreChanged;
            }

            _countTween?.Kill();
        }
    }
}