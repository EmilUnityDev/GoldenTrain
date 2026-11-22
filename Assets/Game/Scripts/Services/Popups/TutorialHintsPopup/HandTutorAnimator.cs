using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class HandTutorAnimator : MonoBehaviour {
    [Header("Animation Settings")]
    [SerializeField] private float totalDuration = 2f;
    [SerializeField] private float flyDuration = 1.2f;
    [SerializeField] private float moveUpPercent = 0.1f;
    [SerializeField] private float rotateAngle = 10f;

    private RectTransform rect;
    private Vector2 startPos;
    private Quaternion startRot;
    private Sequence seq;

    private void Awake() {
        rect = GetComponent<RectTransform>();
        CaptureState();
    }

    public void CaptureState() {
        startPos = rect.anchoredPosition;
        startRot = rect.localRotation;
    }

    private void OnEnable() {
        Play();
    }

    private void OnDisable() {
        StopAndReset();
    }

    public void StopAndReset() {
        seq?.Kill();
        rect.anchoredPosition = startPos;
        rect.localRotation = startRot;
    }

    public void Play() {
        seq?.Kill();

        float backDuration = totalDuration - flyDuration;
        float moveOffset = rect.rect.height * moveUpPercent;

        seq = DOTween.Sequence();
        seq.SetLoops(-1);
        seq.Append(rect.DOAnchorPosY(startPos.y + moveOffset, flyDuration)
            .SetEase(Ease.OutQuad));

        seq.Join(rect.DOLocalRotate(new Vector3(0, 0, -rotateAngle), flyDuration)
            .SetEase(Ease.OutQuad));

        seq.Append(rect.DOAnchorPos(startPos, backDuration)
            .SetEase(Ease.InQuad));

        seq.Join(rect.DOLocalRotateQuaternion(startRot, backDuration)
            .SetEase(Ease.InQuad));
    }
}