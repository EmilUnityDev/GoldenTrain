using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts {
    public class SymbolAgh : SymbolBase {
        [SerializeField] private SpriteRenderer symbolRenderer;
        [SerializeField] private float spawnScaleDuration = 0.8f;
        [SerializeField] private float spawnFadeDuration = 0.6f;
        [SerializeField] private float idleRotateAngle = 10f;
        [SerializeField] private float idleRotateDuration = 1.5f;
        [SerializeField] private float destroyFadeDuration = 0.5f;
        [SerializeField] private float destroyScaleDuration = 0.5f;
        [SerializeField] private float destroyDelay = 0f;

        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        [SerializeField] private GameObject destroyVFX;
        [SerializeField] private ParticleSpawner destroyParticles;
        private Color _baseColor;
        private Tween _idleTween;
        private Vector3 _startScale;

        private void Awake() {
            if (symbolRenderer != null) {
                _baseColor = symbolRenderer.color;
            }

            _startScale = transform.localScale;

            if (destroyVFX != null) {
                destroyVFX.SetActive(false);
            }
        }

        public override void SymbolSpawn() {
            if (useDOTweenForSpawn)
                PlaySpawnDOTween();
            else
                PlaySpawnAnimator();
        }

        public override void IdleAnimation() {
            if (useDOTweenForIdle)
                PlayIdleDOTween();
            else
                PlayIdleAnimator();
        }

        public override void DestroyAnim() {
            if (useDOTweenForDestroy)
                PlayDestroyDOTween();
            else
                PlayDestroyAnimator();

            if (destroyVFX != null) {
                destroyVFX.SetActive(true);
            }
            if (destroyParticles != null) destroyParticles.SpawnAndAttract();
        }

        public override void PlaySpawnDOTween() {
            if (symbolRenderer == null) return;

            symbolRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(_startScale, spawnScaleDuration).SetEase(Ease.OutBack));
            seq.Join(symbolRenderer.DOFade(1f, spawnFadeDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() => IdleAnimation());
        }

        public override void PlaySpawnAnimator() {
            if (animator == null) return;
            animator.SetTrigger(spawnTrigger);  
        }

        public override void PlayIdleDOTween() {
            if (symbolRenderer == null) return;

            _idleTween?.Kill();
            symbolRenderer.transform.rotation = Quaternion.identity;

            _idleTween = symbolRenderer.transform
                .DORotate(new Vector3(0f, 0f, idleRotateAngle), idleRotateDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public override void PlayIdleAnimator() {
            if (animator == null) return;
            animator.SetTrigger(idleTrigger);
        }

        public override void PlayDestroyDOTween() {
            if (symbolRenderer == null) return;

            _idleTween?.Kill();

            float halfScale = destroyScaleDuration * 0.5f;
            Sequence seq = DOTween.Sequence();

            Vector3 spriteStartScale = symbolRenderer.transform.localScale;
            Vector3 spriteBoostScale = spriteStartScale * 1.2f;

            if (destroyDelay > 0f) {
                seq.Append(symbolRenderer.transform
                    .DOScale(spriteBoostScale, destroyDelay)
                    .SetEase(Ease.OutQuad));
            } else {
                seq.AppendInterval(0f);
            }

            seq.Append(transform
                .DOScale(Vector3.zero, destroyScaleDuration)
                .SetEase(Ease.InBack));

            seq.Insert(destroyDelay + halfScale,
                symbolRenderer
                    .DOFade(0f, halfScale)
                    .SetEase(Ease.InQuad)
            );

            seq.OnComplete(() => Destroy(gameObject));
        }

        public override void PlayDestroyAnimator() {
            if (animator == null) return;

            animator.SetTrigger(destroyTrigger);
        }

        public void OnDestroyAnimationComplete() {
            Destroy(gameObject);
        }
    }
}