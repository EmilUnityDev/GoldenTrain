using System;
using UnityEngine;

namespace Game.Scripts.Services.Score
{
    public class ScoreService
    {
        private int _score;
        private int _highScore;
        private int _currentSpinScore;
        private int _bonusScore;
        private int _deferredPoints;

        private float _megaWinCooldown = 10f;
        private float _lastMegaWinTime = -999f;

        public float ScoreMultiplier { get; set; } = 1f;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnHighScoreChanged;
        public event Action<int> OnCurrentSpinScoreChanged;
        public event Action<int> OnBonusScoreChanged;
        public event Action<int> OnGameScoreChanged;
        public event Action<int> OnFutureScoreChanged;

        public int Score
        {
            get => _score;
            private set
            {
                if (_score == value) return;
                _score = value;
                OnScoreChanged?.Invoke(_score);
                OnGameScoreChanged?.Invoke(GameScore);
                OnFutureScoreChanged?.Invoke(FutureScore);
            }
        }

        public int CurrentSpinScore
        {
            get => _currentSpinScore;
            private set
            {
                _currentSpinScore = value;
                OnCurrentSpinScoreChanged?.Invoke(_currentSpinScore);
            }
        }

        public int BonusScore
        {
            get => _bonusScore;
            private set
            {
                if (_bonusScore == value) return;
                _bonusScore = value;
                OnBonusScoreChanged?.Invoke(_bonusScore);
                OnGameScoreChanged?.Invoke(GameScore);
            }
        }

        public int GameScore => Score - BonusScore;
        
        public int FutureScore => Score + _deferredPoints;

        public int HighScore
        {
            get => _highScore;
            private set
            {
                _highScore = value;
                OnHighScoreChanged?.Invoke(_highScore);
            }
        }

        public ScoreService()
        {
            _highScore = PlayerPrefs.GetInt("HighScore", 0);
            _score = 0;
        }

        public void AddPoints(int pts)
        {
            var totalPoints = pts * ScoreMultiplier;
            var pointsToAdd = (int)totalPoints;

            Score += pointsToAdd;
            CurrentSpinScore += pointsToAdd;

            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt("HighScore", HighScore);
                PlayerPrefs.Save();
            }
        }

        public void AddPointsDeferred(int pts)
        {
            var totalPoints = pts * ScoreMultiplier;
            var pointsToAdd = (int)totalPoints;

            _deferredPoints += pointsToAdd;
            CurrentSpinScore += pointsToAdd;
            OnFutureScoreChanged?.Invoke(FutureScore);
        }

        public void CommitDeferredPoints()
        {
            if (_deferredPoints > 0)
            {
                Score += _deferredPoints;
                
                if (Score > HighScore)
                {
                    HighScore = Score;
                    PlayerPrefs.SetInt("HighScore", HighScore);
                    PlayerPrefs.Save();
                }
                
                _deferredPoints = 0;
            }

            CurrentSpinScore = 0;
        }

        public void ResetCurrentSpinScore()
        {
            CurrentSpinScore = 0;
            _deferredPoints = 0;
            OnFutureScoreChanged?.Invoke(FutureScore);
        }
        
        public void IncreaseScoreMultiplier()
        {
            ScoreMultiplier += 0.2f;
        }

        public void SetCurrentSpinScore(int score, bool needToAdd = false)
        {
            CurrentSpinScore = score;
            if(needToAdd) Score += score;
        }

        public void AddBonusPoints(int pts)
        {
            var totalPoints = pts * ScoreMultiplier;
            var pointsToAdd = (int)totalPoints;

            Score += pointsToAdd;
            BonusScore += pointsToAdd;
        }
    }
}
