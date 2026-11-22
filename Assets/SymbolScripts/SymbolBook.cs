using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts
{
    public class SymbolBook : SymbolBase
    {
        [SerializeField] private SpriteRenderer bookRenderer;
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        [SerializeField] private GameObject destroyVFX;
        [SerializeField] private ParticleSpawner destroyParticles;

        private Color _baseColor;
        private Vector3 _startScale;

        private void Awake()
        {
            if (bookRenderer != null)
            {
                _baseColor = bookRenderer.color;
            }

            _startScale = transform.localScale;

            destroyVFX.SetActive(false);
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
            transform.localScale = Vector3.zero;
            transform.DOScale(_startScale, 0.5f).SetEase(Ease.OutBack).OnComplete(() => IdleAnimation());
        }

        public override void PlaySpawnAnimator() { if (animator != null) { animator.SetTrigger(spawnTrigger); } }

        private IEnumerator WaitForSpawnEndAndStartIdle()
        {
            yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsName(spawnTrigger));
            IdleAnimation();
        }

        public override void PlayIdleDOTween()
        {
            if (bookRenderer == null) return;

            Color c = bookRenderer.color;
            c.a = 0.7f;
            bookRenderer.color = c;

            bookRenderer.DOFade(1f, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public override void PlayIdleAnimator()
        {
            if (animator == null) return;
            animator.SetTrigger(idleTrigger);
        }

        public override void PlayDestroyDOTween()
        {

            Sequence seq = DOTween.Sequence();

            seq.AppendInterval(0.3f);

            seq.Append(
                transform.DOScale(Vector3.zero, 0.4f)
                    .SetEase(Ease.InBack)
            );

            seq.OnComplete(SymbolDie);
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