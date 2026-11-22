using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SymbolScripts
{
    [Serializable]
    public struct MultiplierSprite
    {
        public int value;
        public Sprite sprite;
    }

    public class SymbolMultiplier : SymbolBase
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private MultiplierSprite[] _multiplierSprites;
        [SerializeField] private float spawnPunchScale = 0.12f;
        [SerializeField] private float spawnPunchDuration = 0.25f;
        [SerializeField] private float destroyFadeDuration = 0.3f;
        [SerializeField] private Sprite _conductorSprite;
        
        public int MultiplierValue { get; private set; }

        private Tween _activeTween;

        public void SetValue(int value)
        {
            MultiplierValue = value;
            if (_spriteRenderer != null && _multiplierSprites != null)
            {
                foreach (var ms in _multiplierSprites)
                {
                    if (ms.value == value)
                    {
                        _spriteRenderer.sprite = ms.sprite;
                        break;
                    }
                }
            }
        }

        public void SetAsConductor()
        {
            if (_spriteRenderer != null && _conductorSprite != null)
                _spriteRenderer.sprite = _conductorSprite;
        }
        
        public override void PlaySpawnDOTween()
        {
            _activeTween?.Kill();
            transform.localScale = Vector3.zero;
            _activeTween = transform.DOScale(Vector3.one, spawnPunchDuration)
                .SetEase(Ease.OutBack);
        }

        public override void PlaySpawnAnimator() { }

        public override void PlayIdleDOTween() { }

        public override void PlayIdleAnimator() { }

        public override void PlayDestroyDOTween()
        {
            _activeTween?.Kill();
            if (_spriteRenderer != null)
            {
                _activeTween = _spriteRenderer.DOFade(0f, destroyFadeDuration)
                    .OnComplete(() => Destroy(gameObject));
            }
            else
            {
                _activeTween = transform.DOScale(Vector3.zero, destroyFadeDuration)
                    .OnComplete(() => Destroy(gameObject));
            }
        }

        public override void PlayDestroyAnimator()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }
    }
}
