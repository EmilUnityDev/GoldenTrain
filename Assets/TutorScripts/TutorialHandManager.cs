using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TutorScripts
{
    public class TutorialHandManager : MonoBehaviour
    {
        [SerializeField] private Image handHint;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera uiCamera;

        private Sequence _activeSeq;
        private const string ClickTweenId = "TutorialHand_Click";
        private const string SwipeTweenId = "TutorialHand_Swipe";
        private const string Swipe3TweenId = "TutorialHand_Swipe3";

        public void HandClickAnim(Transform pos)
        {
            StopAllHandAnimsInstant();

            var hand = handHint.rectTransform;
            var target = ToCanvasPosition(pos);
            hand.anchoredPosition = target;
            hand.localScale = Vector3.one;
            SetAlpha(1f);
            handHint.enabled = true;

            float clickPreDelay = 0.1f;
            float clickDownDuration = 0.25f;
            float clickUpDuration = 0.3f;
            float clickPressOffset = 6f;
            float clickScale = 0.9f;
            Ease clickEase = Ease.OutQuad;

            var baseY = hand.anchoredPosition.y;

            _activeSeq = DOTween.Sequence().SetId(ClickTweenId).SetUpdate(true);
            _activeSeq.AppendInterval(clickPreDelay);
            _activeSeq.Append(hand.DOScale(clickScale, clickDownDuration).SetEase(clickEase));
            _activeSeq.Join(hand.DOAnchorPosY(baseY - clickPressOffset, clickDownDuration).SetEase(clickEase));
            _activeSeq.Append(hand.DOScale(1f, clickUpDuration).SetEase(clickEase));
            _activeSeq.Join(hand.DOAnchorPosY(baseY, clickUpDuration).SetEase(clickEase));
            _activeSeq.AppendInterval(1f);
            _activeSeq.SetLoops(-1, LoopType.Restart);
        }

        public void HandSwipeAnim(Transform start, Transform end)
        {
            StopAllHandAnimsInstant();

            var hand = handHint.rectTransform;
            var p0 = ToCanvasPosition(start);
            var p1 = ToCanvasPosition(end);

            hand.anchoredPosition = p0;
            hand.localScale = Vector3.one;
            SetAlpha(1f);
            handHint.enabled = true;

            float swipePreDelay = 0.15f;
            float swipePressDuration = 0.12f;
            float swipeMoveDuration = 0.8f;
            float swipeReleaseDuration = 0.12f;
            float swipePressOffset = 6f;
            float swipeScale = 0.92f;
            Ease swipeEase = Ease.InOutSine;

            var baseY = hand.anchoredPosition.y;

            var seq = DOTween.Sequence().SetId(SwipeTweenId).SetUpdate(true);
            seq.AppendInterval(swipePreDelay);
            seq.Append(hand.DOScale(swipeScale, swipePressDuration));
            seq.Join(hand.DOAnchorPosY(baseY - swipePressOffset, swipePressDuration));
            seq.Append(hand.DOAnchorPos(p1, swipeMoveDuration).SetEase(swipeEase));
            seq.Append(hand.DOScale(1f, swipeReleaseDuration));
            seq.Join(hand.DOAnchorPosY(p1.y, swipeReleaseDuration));
            seq.AppendInterval(0.25f);
            seq.Append(hand.DOAnchorPos(p0, swipeMoveDuration).SetEase(swipeEase));
            seq.SetLoops(-1, LoopType.Restart);

            _activeSeq = seq;
        }

        public void HandSwipeAnim3Points(Transform first, Transform second, Transform third)
        {
            StopAllHandAnimsInstant();

            var hand = handHint.rectTransform;
            var p0 = ToCanvasPosition(first);
            var p1 = ToCanvasPosition(second);
            var p2 = ToCanvasPosition(third);
            hand.anchoredPosition = p0;
            hand.localScale = Vector3.one;
            SetAlpha(1f);
            handHint.enabled = true;
            float swipePreDelay = 0.15f;
            float swipePressDuration = 0.12f;
            float swipeMoveDuration = 0.8f;
            float swipeReleaseDuration = 0.12f;
            float swipePressOffset = 6f;
            float swipeScale = 0.92f;
            Ease swipeEase = Ease.InOutSine;

            var baseY = hand.anchoredPosition.y;

            var seq = DOTween.Sequence().SetId(Swipe3TweenId).SetUpdate(true);
            seq.AppendInterval(swipePreDelay);
            seq.Append(hand.DOScale(swipeScale, swipePressDuration));
            seq.Join(hand.DOAnchorPosY(baseY - swipePressOffset, swipePressDuration));
            seq.Append(hand.DOAnchorPos(p1, swipeMoveDuration).SetEase(swipeEase));
            seq.Append(hand.DOAnchorPos(p2, swipeMoveDuration).SetEase(swipeEase));
            seq.Append(hand.DOScale(1f, swipeReleaseDuration));
            seq.Join(hand.DOAnchorPosY(p2.y, swipeReleaseDuration));
            seq.AppendInterval(0.25f);
            seq.Append(hand.DOAnchorPos(p0, swipeMoveDuration).SetEase(swipeEase));
            seq.SetLoops(-1, LoopType.Restart);

            _activeSeq = seq;
        }

        public void StopSwipeAnim3Points() => FadeOutAndKill(Swipe3TweenId);

        public void StopClickAnim() => FadeOutAndKill(ClickTweenId);
        public void StopSwipeAnim() => FadeOutAndKill(SwipeTweenId);

        public void StopAllHandAnims()
        {
            FadeOutAndKill(ClickTweenId);
            FadeOutAndKill(SwipeTweenId);
        }

        private void StopAllHandAnimsInstant()
        {
            KillActive();
            handHint.DOKill();
            handHint.enabled = false;
        }

        private void FadeOutAndKill(string tweenId)
        {
            DOTween.Kill(tweenId);
            if (_activeSeq != null && _activeSeq.IsActive())
                _activeSeq.Kill();

            _activeSeq = null;

            if (handHint.enabled)
            {
                handHint.DOKill();
                handHint.DOFade(0f, 0.25f).OnComplete(() => handHint.enabled = false);
            }
        }

        private void KillActive()
        {
            if (_activeSeq != null && _activeSeq.IsActive())
            {
                _activeSeq.Kill();
                _activeSeq = null;
            }
        }

        private void SetAlpha(float a)
        {
            var c = handHint.color;
            c.a = a;
            handHint.color = c;
        }

        private Vector2 ToCanvasPosition(Transform t)
        {
            var canvasRect = canvas.transform as RectTransform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, t.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out var localPoint);
            return localPoint;
        }
        private Vector2 ToCanvasPosition(Vector3 worldPos)
        {
            var canvasRect = canvas.transform as RectTransform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out var localPoint);
            return localPoint;
        }
    }
}
