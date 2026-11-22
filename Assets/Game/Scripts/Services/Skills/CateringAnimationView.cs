using Game.Scripts.GameRoot.Services.Feedback;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Skills
{
    public class CateringAnimationView : MonoBehaviour
    {
        [SerializeField] private RectTransform _maskRect;
        [SerializeField] private RectTransform _trainsParent;

        [Header("Trains")]
        [SerializeField] private Image[] _trainImages = new Image[4];
        [SerializeField] private Vector2[] _trainTargetPositions = new Vector2[4];
        [SerializeField] private float _trainMoveDuration = 1f;
        [SerializeField] private float _trainDelay = 0.3f;

        private IFeedbackService _audioService;

        [Inject]
        public void Construct(IFeedbackService audioService) {
            _audioService = audioService;
        }

        public IEnumerator PlayAndWait(Vector3 worldPos, Camera worldCamera, Canvas canvas)
        {
            _audioService?.PlaySfx("skillOne");

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 localPos);

            foreach (var train in _trainImages)
            {
                if (train == null) continue;
                train.gameObject.SetActive(false);
            }

            _maskRect.localPosition = localPos;
            if (_trainsParent != null)
                _trainsParent.localPosition = localPos;

            _maskRect.sizeDelta = new Vector2(60f, 60f);

            float elapsed = 0f;
            const float duration = 2f;
            Vector2 startSize = new Vector2(60f, 60f);
            Vector2 endSize = new Vector2(950f, 950f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _maskRect.sizeDelta = Vector2.Lerp(startSize, endSize, t);
                yield return null;
            }

            _maskRect.sizeDelta = endSize;

            int completedTrains = 0;
            int totalTrains = 0;

            for (int i = 0; i < _trainImages.Length; i++)
            {
                if (_trainImages[i] != null) totalTrains++;
            }

            for (int i = 0; i < _trainImages.Length; i++)
            {
                if (_trainImages[i] == null) continue;

                var img = _trainImages[i];
                var rt = img.rectTransform;
                var targetPos2D = _trainTargetPositions[i];

                img.gameObject.SetActive(true);
                img.color = new Color(1f, 1f, 1f, 0f);
                rt.localScale = Vector3.one * 0.5f;

                var spawnSeq = DOTween.Sequence();
                spawnSeq.Append(img.DOFade(1f, 0.2f));
                spawnSeq.Join(rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
                yield return spawnSeq.WaitForCompletion();

                var moveSeq = DOTween.Sequence();
                moveSeq.Append(rt.DOLocalMove(targetPos2D, _trainMoveDuration).SetEase(Ease.InOutQuad));
                moveSeq.OnComplete(() =>
                {
                    var bounceSeq = DOTween.Sequence();
                    bounceSeq.Append(rt.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad));
                    bounceSeq.Append(rt.DOScale(0.95f, 0.08f).SetEase(Ease.InOutSine));
                    bounceSeq.Append(rt.DOScale(1f, 0.06f).SetEase(Ease.OutSine));
                    bounceSeq.OnComplete(() => completedTrains++);
                });

                yield return new WaitForSeconds(_trainDelay);
            }

            yield return new WaitUntil(() => completedTrains >= totalTrains);
            _audioService?.PlaySfx("useSkill0");
            _audioService?.PlaySfx("coins");
            yield return new WaitForSeconds(0.3f);

            Destroy(gameObject);
        }
    }
}

