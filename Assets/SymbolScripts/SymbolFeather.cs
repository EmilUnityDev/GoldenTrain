using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SymbolScripts {
    public class SymbolFeather : SymbolBase {
        [Header("References")]
        [SerializeField] private GameObject featherVisual;
        [SerializeField] private GameObject destroyVFX;
        [SerializeField] private ParticleSpawner destroyParticles;

        [Header("Settings")]
        [SerializeField] private float floatAmplitude = 0.1f;
        [SerializeField] private float floatDuration = 1.2f;
        [SerializeField] private float idleLightStrength = 0.15f;
        [SerializeField] private float destroyScaleDuration = 0.7f;
        [SerializeField] private float destroyDelay = 0.2f;

        [Header("Animator Triggers")]
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string destroyTrigger = "Destroy";

        private Vector3 _startScale;
        private Vector3 _startLocalPos;

        // Список для хранения всех вложенных рендереров и их базовых цветов
        private SpriteRenderer[] _renderers;
        private List<Color> _baseColors = new List<Color>();

        private void Awake() {
            if (featherVisual != null) {
                _startLocalPos = featherVisual.transform.localPosition;

                // Находим все рендереры во вложенных объектах
                _renderers = featherVisual.GetComponentsInChildren<SpriteRenderer>();

                foreach (var sr in _renderers) {
                    _baseColors.Add(sr.color);
                }
            }

            _startScale = transform.localScale;

            if (destroyVFX != null)
                destroyVFX.SetActive(false);
            
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

            if (destroyVFX != null) destroyVFX.SetActive(true);
            if (destroyParticles != null) destroyParticles.SpawnAndAttract();
        }

        public override void PlaySpawnDOTween() {
            if (featherVisual == null) return;

            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            // Управляем прозрачностью всех вложенных спрайтов одновременно
            for (int i = 0; i < _renderers.Length; i++) {
                var sr = _renderers[i];
                var baseCol = _baseColors[i];
                sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0f);
                seq.Join(sr.DOFade(baseCol.a, 0.4f));
            }

            seq.Join(transform.DOScale(_startScale, 0.5f).SetEase(Ease.OutBack));
            seq.OnComplete(() => IdleAnimation());
        }

        public override void PlayIdleDOTween() {
            if (featherVisual == null) return;

            featherVisual.transform.localPosition = _startLocalPos;

            // Плавно двигаем весь корневой объект визуализации
            featherVisual.transform
                .DOLocalMoveY(_startLocalPos.y + floatAmplitude, floatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // Запускаем переливание цвета для каждого спрайта
            for (int i = 0; i < _renderers.Length; i++) {
                var sr = _renderers[i];
                var baseCol = _baseColors[i];

                var brighter = baseCol * (1f + idleLightStrength);
                brighter.a = baseCol.a;

                sr.DOColor(brighter, floatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public override void PlayDestroyDOTween() {
            if (featherVisual == null) return;

            float halfScale = destroyScaleDuration * 0.5f;
            Sequence seq = DOTween.Sequence();

            Vector3 visualStartScale = featherVisual.transform.localScale;
            Vector3 visualBoostScale = visualStartScale * 1.2f;

            if (destroyDelay > 0f) {
                seq.Append(featherVisual.transform.DOScale(visualBoostScale, destroyDelay).SetEase(Ease.OutQuad));
            }

            seq.Append(transform.DOScale(Vector3.zero, destroyScaleDuration).SetEase(Ease.InBack));

            // Исчезновение всех спрайтов в конце
            for (int i = 0; i < _renderers.Length; i++) {
                seq.Insert(destroyDelay + halfScale, _renderers[i].DOFade(0f, halfScale).SetEase(Ease.InQuad));
            }

            seq.OnComplete(() => Destroy(gameObject));
        }

        public override void PlaySpawnAnimator() { if (animator != null) { animator.SetTrigger(spawnTrigger);  } }
        public override void PlayIdleAnimator() { if (animator != null) animator.SetTrigger(idleTrigger); }
        public override void PlayDestroyAnimator() {
            if (animator == null) return;

            animator.SetTrigger(destroyTrigger);
        }

        public void OnDestroyAnimationComplete() {
            Destroy(gameObject);
        }

    }
}