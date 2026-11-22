using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts
{
    public class SymbolRaEye : SymbolBase
    {
        [SerializeField] private SpriteRenderer symbolRenderer;
        [SerializeField] private Transform tweenTarget;

        [SerializeField] private float spawnPunchScale = 0.08f;
        [SerializeField] private float spawnPunchDuration = 0.28f;

        [SerializeField] private float idlePulseScale = 0.02f;
        [SerializeField] private float idlePulseDuration = 1.4f;

        [SerializeField] private float destroyFadeDuration = 0.35f;
        [SerializeField] private float destroyScaleDuration = 0.35f;
        [SerializeField] private float destroyScaleAmount = 0.85f;

        [SerializeField] private float destroyDelay = 0f;

        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        [SerializeField] private GameObject destroyVFX;
        [SerializeField] private ParticleSpawner destroyParticles;

        private Color _baseColor;
        private bool _isDestroying;

        private Tween _spawnPunch;
        private Tween _idlePulse;
        private Tween _destroyFade;
        private Tween _destroyScale;

        private Vector3 _startScale;

        private Transform Target => tweenTarget != null ? tweenTarget : transform;

        private void Awake()
        {
            if (symbolRenderer != null)
            {
                _baseColor = symbolRenderer.color;
            }

            _startScale = Target.localScale;

            destroyVFX.SetActive(false);
        }

        private void OnDisable()
        {
            KillAllTweens();
            _isDestroying = false;
        }

        private void KillAllTweens()
        {
            _spawnPunch?.Kill();
            _idlePulse?.Kill();
            _destroyFade?.Kill();
            _destroyScale?.Kill();
        }

        public override void SymbolSpawn()
        {
            if (useDOTweenForSpawn)
                PlaySpawnDOTween();
            else
                PlaySpawnAnimator();
        }

        public override void IdleAnimation()
        {
            if (useDOTweenForIdle)
                PlayIdleDOTween();
            else
                PlayIdleAnimator();
        }

        public override void DestroyAnim()
        {
            if (useDOTweenForDestroy)
                PlayDestroyDOTween();
            else
                PlayDestroyAnimator();

            destroyVFX.SetActive(true);
            if (destroyParticles != null) destroyParticles.SpawnAndAttract();
        }

        public override void PlaySpawnDOTween()
        {
            KillAllTweens();
            _isDestroying = false;
            Target.localScale = _startScale;
            if (symbolRenderer != null)
                symbolRenderer.color = _baseColor;

            if (spawnPunchScale > 0f && spawnPunchDuration > 0f)
            {
                _spawnPunch = Target.DOPunchScale(_startScale * spawnPunchScale, spawnPunchDuration, 8, 0.6f).OnComplete(() => IdleAnimation());
            }
        }

        public override void PlaySpawnAnimator()
        {
            if (animator == null) return;

            animator.SetTrigger(spawnTrigger);
            StartCoroutine(WaitForSpawnEndAndStartIdle());
        }

        private IEnumerator WaitForSpawnEndAndStartIdle()
        {
            yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsName(spawnTrigger));
            IdleAnimation();
        }

        public override void PlayIdleDOTween()
        {
            if (idlePulseScale > 0f && idlePulseDuration > 0f)
            {
                Target.localScale = _startScale;
                _idlePulse = Target
                    .DOScale(_startScale * (1f + idlePulseScale), idlePulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public override void PlayIdleAnimator()
        {
            if (animator == null) return;
            animator.SetTrigger(idleTrigger);
        }

        public override void PlayDestroyDOTween()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            _spawnPunch?.Kill();
            _idlePulse?.Kill();

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