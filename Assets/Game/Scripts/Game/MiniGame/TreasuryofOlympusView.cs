using System;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Game.MiniGame
{
    public class TreasuryofOlympusView : MonoBehaviour, IPopup
    {
        private TreasuryofOlympusViewModel _vm;
        public event Action OnClose;

        [SerializeField] private Button _closePopupButton;

        [SerializeField] private Image _backImage;

        public void Bind(IPopupData data)
        {
            _vm = data as TreasuryofOlympusViewModel;
            PopupAnimationHandler.PlaySpawnAnimation(gameObject);

            LocalEventLogger.SendEvent("TreasuryofOlympus_popup_opened");

            if (_closePopupButton != null)
                _closePopupButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    ClosePopup();
                });
        }

        private void PlayClickSound()
        {
        }

        public void ClosePopup()
        {
            LocalEventLogger.SendEvent("TreasuryofOlympus_popup_closed");
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}