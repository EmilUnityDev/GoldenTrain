using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Services.Popups.CollectionsPopup
{
    public class CollectionCardPopupView : MonoBehaviour
    {
        [SerializeField] private RectTransform _animatedRect;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private float _moveUpAmount = 200f;
        [SerializeField] private float _duration = 1.5f;

        private IFeedbackService _audioService;

        [Inject]
        public void Construct(IFeedbackService audioService) {
            _audioService = audioService;
        }

        public void SetCount(int count)
        {
            if (_countText != null)
                _countText.text = count.ToString();
        }

        private void Start()
        {
            var startPos = _animatedRect.anchoredPosition;
            var targetPos = startPos + Vector2.up * _moveUpAmount;

            _canvasGroup.alpha = 1f;

            _animatedRect.DOAnchorPos(targetPos, _duration).SetEase(Ease.OutCubic);
            _animatedRect.DOScale(0.5f, _duration).SetEase(Ease.InCubic);
            _canvasGroup.DOFade(0f, _duration).SetEase(Ease.InCubic)
                .OnComplete(() => Destroy(gameObject));


            _audioService?.PlaySfx("useSkill0");
        }
    }
}
