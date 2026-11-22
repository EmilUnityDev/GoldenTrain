using System.Collections;
using System.Collections.Generic; // Добавлено для List
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts {
    public class SymbolScarab : SymbolBase {
        [Header("References")]
        // Заменили одиночный SpriteRenderer на GameObject
        [SerializeField] private GameObject symbolVisual;
        [SerializeField] private Transform tweenTarget;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnPunchScale = 0.07f;
        [SerializeField] private float spawnPunchDuration = 0.26f;
        [SerializeField] private float spawnTiltAngle = 10f;
        [SerializeField] private float spawnTiltDuration = 0.22f;

        [Header("Idle Settings")]
        [SerializeField] private float idleSwayAngle = 8f;
        [SerializeField] private float idleSwayDuration = 1.25f;
        [SerializeField] private float idleBobOffset = 0.025f;
        [SerializeField] private float idleBobDuration = 1.4f;

        [Header("Destroy Settings")]
        [SerializeField] private float destroyFadeDuration = 0.35f;

        [Header("Animator Triggers")]
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        private bool _isDestroying;
        private Vector3 _idleBaseLocalPos;
        private Vector3 _startScale;

        // Список для хранения всех вложенных рендереров
        private SpriteRenderer[] _renderers;
        private List<Color> _baseColors = new List<Color>();

        // Твины для контроля
        private Tween _spawnPunch;
        private Tween _spawnTilt;
        private Tween _idleSway;
        private Tween _idleBob;

        private Transform Target => tweenTarget != null ? tweenTarget : transform;

        private void Awake() {
            if (symbolVisual != null) {
                // Собираем все спрайты внутри иерархии
                _renderers = symbolVisual.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in _renderers) {
                    _baseColors.Add(sr.color);
                }
            }

            _idleBaseLocalPos = Target.localPosition;
            _startScale = Target.localScale;
        }

        private void OnDisable() {
            KillAllTweens();
            _isDestroying = false;
        }

        private void KillAllTweens() {
            _spawnPunch?.Kill();
            _spawnTilt?.Kill();
            _idleSway?.Kill();
            _idleBob?.Kill();
            // Твины уничтожения убивать обычно не нужно, так как объект удаляется
        }

        public override void SymbolSpawn() {
            if (useDOTweenForSpawn) PlaySpawnDOTween();
            else PlaySpawnAnimator();
        }

        public override void IdleAnimation() {
            if (useDOTweenForIdle) PlayIdleDOTween();
            else PlayIdleAnimator();
        }

        public override void DestroyAnim() {
            if (useDOTweenForDestroy) PlayDestroyDOTween();
            else PlayDestroyAnimator();
        }

        public override void PlaySpawnDOTween() {
            KillAllTweens();
            _isDestroying = false;

            Target.localScale = _startScale;
            Target.localRotation = Quaternion.identity;
            Target.localPosition = _idleBaseLocalPos;

            // Возвращаем исходный цвет всем частям
            for (int i = 0; i < _renderers.Length; i++) {
                _renderers[i].color = _baseColors[i];
            }

            if (spawnPunchScale > 0f && spawnPunchDuration > 0f)
                _spawnPunch = Target.DOPunchScale(_startScale * spawnPunchScale, spawnPunchDuration, 8, 0.6f);

            if (spawnTiltAngle > 0f && spawnTiltDuration > 0f) {
                _spawnTilt = Target
                    .DORotate(new Vector3(0f, 0f, spawnTiltAngle), spawnTiltDuration)
                    .SetEase(Ease.OutSine)
                    .OnComplete(() =>
                        Target.DORotate(Vector3.zero, spawnTiltDuration).SetEase(Ease.InSine));
            }

            DOVirtual.DelayedCall(0.1f, () => { if (!_isDestroying) IdleAnimation(); });
        }

        public override void PlayIdleDOTween() {
            if (_isDestroying) return;

            if (idleSwayAngle > 0f && idleSwayDuration > 0f) {
                Target.localRotation = Quaternion.identity;
                _idleSway = Target
                    .DORotate(new Vector3(0f, 0f, idleSwayAngle), idleSwayDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (idleBobOffset > 0f && idleBobDuration > 0f) {
                Target.localPosition = _idleBaseLocalPos;
                _idleBob = Target
                    .DOLocalMoveY(_idleBaseLocalPos.y + idleBobOffset, idleBobDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public override void PlayDestroyDOTween() {
            _isDestroying = true;
            KillAllTweens();

            Sequence seq = DOTween.Sequence();

            if (_renderers != null) {
                for (int i = 0; i < _renderers.Length; i++) {
                    seq.Join(_renderers[i].DOFade(0f, destroyFadeDuration));
                }
            }

            seq.Join(Target.DOScale(Vector3.zero, destroyFadeDuration).SetEase(Ease.InBack));
            seq.OnComplete(() => Destroy(gameObject));
        }

        public override void PlaySpawnAnimator() {
            if (animator == null) return;
            animator.SetTrigger(spawnTrigger);
            
        }

        public override void PlayIdleAnimator() {
            if (animator == null) return;
            animator.SetTrigger(idleTrigger);
        }

        public void OnDestroyAnimationComplete() {
            Destroy(gameObject);
        }

        public override void PlayDestroyAnimator() {
            if (animator == null) return;

            animator.SetTrigger(destroyTrigger);
        }
    }
}