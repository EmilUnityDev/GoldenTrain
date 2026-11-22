using System;
using System.Collections;
using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class SymbolController : MonoBehaviour
    {
        private SymbolData _data;
        private Animator _animator;

        public int Row { get; set; }
        public int Col { get; set; }

        public void Setup(SymbolData data, float minAnimSpeed, float maxAnimSpeed)
        {
            _data = data;
            if (_animator != null)
            {
                _animator.speed = UnityEngine.Random.Range(minAnimSpeed, maxAnimSpeed);
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public IEnumerator MoveToPosition(Vector3 targetPos, float duration, Action onComplete = null)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            _animator.Play("Falling");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            transform.position = targetPos;
            _animator.Play("Fall");
            onComplete?.Invoke();
        }

        public IEnumerator FadeOutAndDestroy(float duration, Action onComplete = null)
        {
            _animator.Play("Destroy");

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
