using System;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Popups;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Services.Popups.CollectionsPopup.Collections
{
    public class CollectionsPopupView : MonoBehaviour, IPopup
    {
        public event Action OnClose;
    
        [SerializeField] private Image _backImage;

        public void Bind(IPopupData data)
        {
            PopupAnimationHandler.PlaySpawnAnimation(gameObject);
        }

        public void ClosePopup()
        {
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}
