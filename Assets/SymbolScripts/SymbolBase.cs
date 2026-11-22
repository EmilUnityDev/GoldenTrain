using System;
using System.Collections;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SymbolScripts
{
    public abstract class SymbolBase : MonoBehaviour, IPointerDownHandler
    {
        [Header("Animator")]
        [SerializeField] protected Animator animator;

        [Header("Anim Type")]
        [SerializeField] protected bool useDOTweenForSpawn = true;
        [SerializeField] protected bool useDOTweenForIdle = true;
        [SerializeField] protected bool useDOTweenForDestroy = true;

        public GameObject SelectedGameObject;
        public  SpriteRenderer blackRenderer;
        
        public int Row { get; set; }
        public int Col { get; set; }

        private SymbolData _data;
        public SymbolData Data
        {
            get => _data;
            set
            {
                _data = value;
                UpdateFlags();
            }
        }

        private void Start()
        {
            if (SelectedGameObject != null)
            {
                SelectedGameObject.SetActive(false);
            }
            
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }

        public bool IsWild => _data != null && _data.isWild;
        public bool IsScatter => _data != null && _data.isScatter;

        private void UpdateFlags()
        {
        }

        public virtual void SymbolDie()
        {
            Destroy(gameObject);
        }

        public virtual void SymbolSpawn()
        {
            if (useDOTweenForSpawn)
                PlaySpawnDOTween();
            else
                PlaySpawnAnimator();
        }

        public virtual void IdleAnimation()
        {
            if (useDOTweenForIdle)
                PlayIdleDOTween();
            else
                PlayIdleAnimator();
        }

        public virtual void DestroyAnim()
        {
            if (useDOTweenForDestroy)
                PlayDestroyDOTween();
            else
                PlayDestroyAnimator();
        }

        public abstract void PlaySpawnDOTween();
        public abstract void PlaySpawnAnimator();
        public abstract void PlayIdleDOTween();
        public abstract void PlayIdleAnimator();
        public abstract void PlayDestroyDOTween();
        public abstract void PlayDestroyAnimator();

        public virtual IEnumerator MoveToPosition(Vector3 targetPos, float duration, Action onComplete = null)
        {
            if (this == null) yield break;

            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < duration && this != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (this != null)
            {
                transform.position = targetPos;
                onComplete?.Invoke();
                IdleAnimation();
            }
        }

        public void SetDimmed(bool dimmed)
        {
            if (animator != null)
                animator.enabled = !dimmed;

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            var color = dimmed
                ? new Color(255f / 255f, 255f / 255f, 255f / 255f, 1f)
                : Color.white;
            foreach (var sr in renderers)
                sr.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this != null)
                SymbolInputHandler.RegisterClick(this);
        }
    }
}