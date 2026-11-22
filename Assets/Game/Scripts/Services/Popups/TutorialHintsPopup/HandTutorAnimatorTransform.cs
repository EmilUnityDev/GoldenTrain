using DG.Tweening;
using UnityEngine;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup
{
    public class HandTutorAnimatorTransform : MonoBehaviour {
        [Header("Animation Settings")]
        [SerializeField] private float totalDuration = 2f;
        [SerializeField] private float flyDuration = 1.2f;
        [SerializeField] private float moveUpOffset = 30f;
        [SerializeField] private float rotateAngle = 10f;

        private Transform _transform;
        private Vector3 startPos;
        private Quaternion startRot;
        private Sequence seq;

        private void Awake() {
            _transform = transform;
            CaptureState();
        }

        public void CaptureState() {
            startPos = _transform.localPosition;
            startRot = _transform.localRotation;
        }

        private void OnEnable() {
            Play();
        }

        private void OnDisable() {
            StopAndReset();
        }

        public void StopAndReset() {
            seq?.Kill();
            _transform.localPosition = startPos;
            _transform.localRotation = startRot;
        }

        public void Play() {
            seq?.Kill();

            float backDuration = totalDuration - flyDuration;

            seq = DOTween.Sequence();
            seq.SetLoops(-1);
            seq.Append(_transform.DOLocalMoveY(startPos.y + moveUpOffset, flyDuration)
                .SetEase(Ease.OutQuad));

            seq.Join(_transform.DOLocalRotate(new Vector3(0, 0, -rotateAngle), flyDuration)
                .SetEase(Ease.OutQuad));

            seq.Append(_transform.DOLocalMove(startPos, backDuration)
                .SetEase(Ease.InQuad));

            seq.Join(_transform.DOLocalRotateQuaternion(startRot, backDuration)
                .SetEase(Ease.InQuad));
        }
    }
}