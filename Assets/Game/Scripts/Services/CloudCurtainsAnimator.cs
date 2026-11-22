using UnityEngine;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using VContainer;

public class CloudCurtainsAnimator : MonoBehaviour {
    [Header("Cloud Groups")]
    [SerializeField] private RectTransform[] leftClouds;
    [SerializeField] private RectTransform[] rightClouds;

    [Header("Background / Overlay")]
    [SerializeField] private CanvasGroup overlayGroup; 
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float stayDuration = 0.3f;

    [Tooltip("На сколько ширин тучка смещается (4 = 4 ширины)")]
    [SerializeField] private float moveWidthFactor = 4f;

    [Header("Stagger")]
    [SerializeField] private float minStaggerDelay = 0.04f;
    [SerializeField] private float maxStaggerDelay = 0.12f;

    [Header("DOTween Ease")]
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private Vector2[] leftStartPos;
    private Vector2[] rightStartPos;
    private Vector2[] leftTargetPos;
    private Vector2[] rightTargetPos;

    private IFeedbackService _audioService;

    [Inject]
    public void Construct(IFeedbackService audioService) {
        _audioService = audioService;
    }

    private void Awake() {
        CachePositions();

        if (overlayGroup != null) overlayGroup.alpha = 0;
    }

    private void CachePositions() {
        leftStartPos = new Vector2[leftClouds.Length];
        rightStartPos = new Vector2[rightClouds.Length];
        leftTargetPos = new Vector2[leftClouds.Length];
        rightTargetPos = new Vector2[rightClouds.Length];

        for (int i = 0; i < leftClouds.Length; i++) {
            RectTransform cloud = leftClouds[i];
            Vector2 start = cloud.anchoredPosition;
            float shift = cloud.rect.width * moveWidthFactor;
            leftStartPos[i] = start;
            leftTargetPos[i] = new Vector2(start.x + shift, start.y);
        }

        for (int i = 0; i < rightClouds.Length; i++) {
            RectTransform cloud = rightClouds[i];
            Vector2 start = cloud.anchoredPosition;
            float shift = cloud.rect.width * moveWidthFactor;
            rightStartPos[i] = start;
            rightTargetPos[i] = new Vector2(start.x - shift, start.y);
        }
    }

    public void Play() {
        DOTween.Kill(gameObject);
        if (overlayGroup != null) overlayGroup.DOKill();

        if (overlayGroup != null) {
            overlayGroup.DOFade(1f, fadeDuration);
        }
        _audioService.PlaySfx("curtain");
        AnimateClouds(leftClouds, leftStartPos, leftTargetPos);
        AnimateClouds(rightClouds, rightStartPos, rightTargetPos);
    }

    private void AnimateClouds(RectTransform[] clouds, Vector2[] from, Vector2[] to) {
        int count = clouds.Length;
        int[] order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;
        Shuffle(order);

        bool fadeTriggered = false;

        for (int i = 0; i < count; i++) {
            int index = order[i];
            float randomDelay = Random.Range(minStaggerDelay, maxStaggerDelay);

            clouds[index].DOAnchorPos(to[index], moveDuration)
                .SetEase(easeType)
                .SetDelay(randomDelay)
                .OnComplete(() => {

                    if (!fadeTriggered && overlayGroup != null) {
                        fadeTriggered = true;

                        overlayGroup.DOFade(0f, fadeDuration).SetDelay(stayDuration);
                    }

                    clouds[index].DOAnchorPos(from[index], moveDuration)
                        .SetEase(easeType)
                        .SetDelay(stayDuration + randomDelay);
                });
        }
    }

    private void Shuffle(int[] array) {
        for (int i = array.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}