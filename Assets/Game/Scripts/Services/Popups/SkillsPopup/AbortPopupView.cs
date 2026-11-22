using System;
using DG.Tweening;
using Game.Scripts.Game.Gameplay.Root;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Popups.SkillsPopup
{
    public class AbortPopupView : MonoBehaviour, IPopup
    {
        private AbortPopupViewModel _vm;
        public event Action OnClose;

        private IFeedbackService _audioService;
        private GameplayController _gameplayController;

        [SerializeField] private Button _closePopupButton;
        [SerializeField] private Button _endGameButton;
        
        [SerializeField] private Image _backImage;

        [Inject]
        public void Construct(IFeedbackService audioService, GameplayController gameplayController)
        {
            _audioService = audioService;
            _gameplayController = gameplayController;
        }

        public void Bind(IPopupData data)
        {
            _vm = data as AbortPopupViewModel;
            
            PopupAnimationHandler.PlaySpawnAnimation(gameObject);
            
            LocalEventLogger.SendEvent("abort_popup_opened");
            
            _closePopupButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                ClosePopup();
            });
            
            _endGameButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                EndGamePopup();
            });
        }
        
        private void PlayClickSound()
        {
            _audioService?.PlaySfx("click1");
        }

        public void EndGamePopup()
        {
            LocalEventLogger.SendEvent("match_aborted");

            if (_gameplayController != null)
                _gameplayController.EndGame();
            Destroy(gameObject);
        }
        
        public void ClosePopup()
        {
            LocalEventLogger.SendEvent("abort_popup_closed");
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}