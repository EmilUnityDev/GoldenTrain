using System.Collections;
using DG.Tweening;
using Game.Scripts.Game.Gameplay.Services;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Popups.GameOverPopup;
using Game.Scripts.Services.Score;
using SlotsLogic.Match3Slots.Scripts.Core;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Game.Gameplay.Root
{
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private SlotGameManager _slotGameManager;

        private bool _isGameOver;
        private TimerComponent _timer;
        private IPopupService _popupService;

        [Inject]
        public void Construct(IPopupService popupService, ScoreService scoreService)
        {
            _popupService = popupService;
            ScoreService = scoreService;
        }

        public ScoreService ScoreService { get; private set; }

        private void Start() 
        {
            AudioListener.pause = false;
        }

        public void SetTimer(TimerComponent timer)
        {
            _timer = timer;
        }

        public IEnumerator StartGameCor()
        {
            yield return new WaitForSeconds(3f);
            if (_timer != null)
                _timer.StartTimer();
            _slotGameManager.StartGame();
        }

        public void EndGame()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            if (_timer != null)
                _timer.StopTimer();
            _popupService.ShowPopup<GameOverPopupView>();
        }

        public bool IsGameOver()
        {
            return _isGameOver;
        }

        private void OnDestroy()
        {
            DOTween.KillAll();
            DOTween.Clear(true);
        }
    }
}