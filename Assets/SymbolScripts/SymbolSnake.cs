using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts {
    public class SymbolSnake : SymbolBase {
        [Header("References")]
        [SerializeField] private GameObject snakeVisual;
        [SerializeField] private GameObject destroyVFX;
        [SerializeField] private ParticleSpawner destroyParticles;

        [Header("Settings")]
        [SerializeField] private float spawnScaleDuration = 0.5f;
        [SerializeField] private float idleWaveDuration = 1.2f;
        [SerializeField] private float destroyFadeDuration = 0.4f;

        [Header("Animator Triggers")]
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        private Vector3 _startScale;
        private Tween _idleTween;

        // Массивы для управления множественными рендерерами
        private SpriteRenderer[] _renderers;
        private List<Color> _baseColors = new List<Color>();

        private void Awake() {
            if (snakeVisual != null) {
                // Собираем все рендереры во вложенных объектах
                _renderers = snakeVisual.GetComponentsInChildren<SpriteRenderer>();

                foreach (var sr in _renderers) {
                    _baseColors.Add(sr.color);
                }
            }

            _startScale = transform.localScale;

            if (destroyVFX != null)
                destroyVFX.SetActive(false);
        }

        private void OnDisable() {
            _idleTween?.Kill();
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

            if (destroyVFX != null)
                destroyVFX.SetActive(true);
            if (destroyParticles != null) destroyParticles.SpawnAndAttract();
        }

        public override void PlaySpawnDOTween() {
            if (snakeVisual == null) return;

            transform.localScale = Vector3.zero;

            // Анимация масштаба всего символа
            transform.DOScale(_startScale, spawnScaleDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => IdleAnimation());

            // Анимация проявления (Fade In) для всех частей змеи
            for (int i = 0; i < _renderers.Length; i++) {
                var sr = _renderers[i];
                var baseCol = _baseColors[i];

                // Устанавливаем в 0 альфу, сохраняя RGB
                sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0f);
                sr.DOFade(baseCol.a, spawnScaleDuration * 0.8f);
            }
        }

        public override void PlaySpawnAnimator() {
            if (animator == null) return;
            animator.SetTrigger(spawnTrigger);
            
        }

        public override void PlayIdleDOTween() {
            _idleTween?.Kill();

            transform.localScale = _startScale;
            var up = new Vector3(_startScale.x, _startScale.y * 1.05f, _startScale.z);

            // "Дыхание" или волнообразное масштабирование
            _idleTween = transform
                .DOScale(up, idleWaveDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public override void PlayIdleAnimator() {
            if (animator == null) return;
            animator.SetTrigger(idleTrigger);
        }

        public override void PlayDestroyDOTween() {
            _idleTween?.Kill();

            Sequence seq = DOTween.Sequence();

            // Скейлим весь объект в ноль
            seq.Append(transform.DOScale(Vector3.zero, destroyFadeDuration).SetEase(Ease.InBack));

            // Параллельно запускаем Fade Out для всех вложенных спрайтов
            if (_renderers != null) {
                for (int i = 0; i < _renderers.Length; i++) {
                    seq.Join(_renderers[i].DOFade(0f, destroyFadeDuration));
                }
            }

            seq.OnComplete(() => Destroy(gameObject));
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