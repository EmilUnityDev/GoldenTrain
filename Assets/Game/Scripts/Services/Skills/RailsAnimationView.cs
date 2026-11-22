using System;
using System.Collections;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Skills
{
    public class RailsAnimationView : MonoBehaviour
    {
        [SerializeField] private RectMask2D _rectMask;
        [SerializeField] private GameObject _train;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Timing")]
        [SerializeField] private float _bottomPhase1Duration = 1.5f;
        [SerializeField] private float _rightPhaseDuration = 0.5f;
        [SerializeField] private float _bottomPhase2Duration = 0.4f;
        [SerializeField] private float _trainAnimationDuration = 2.5f;
        [SerializeField] private float _fadeDuration = 0.5f;

        private IFeedbackService _audioService;

        [Inject]
        public void Construct(IFeedbackService audioService) {
            _audioService = audioService;
        }


        public void Play(Action onTrainEnabled, Action onComplete)
        {
            StartCoroutine(PlayRoutine(onTrainEnabled, onComplete));
        }

        private IEnumerator PlayRoutine(Action onTrainEnabled, Action onComplete)
        {
            if (_train != null) _train.SetActive(false);

            float bottom = 1387.8f;
            float right = 563f;
            ApplyPadding(0f, 0f, right, bottom);

            bool rightStarted = false;
            Tween rightTween = null;
            
            Tween phase1 = DOTween.To(() => bottom, v =>
            {
                bottom = v;
                if (!rightStarted && bottom <= 598.5f)
                {
                    rightStarted = true;
                    rightTween = DOTween.To(() => right, r =>
                    {
                        right = r;
                        ApplyPadding(0f, 0f, right, bottom);
                    }, 24.4f, _rightPhaseDuration);
                }
                ApplyPadding(0f, 0f, right, bottom);
            }, 563.4f, _bottomPhase1Duration);

            yield return phase1.WaitForCompletion();

            if (rightTween != null && rightTween.IsActive())
                yield return rightTween.WaitForCompletion();
            
            yield return DOTween.To(() => bottom, v =>
            {
                bottom = v;
                ApplyPadding(0f, 0f, right, bottom);
            }, -25.7f, _bottomPhase2Duration).WaitForCompletion();
            
            if (_train != null) _train.SetActive(true);
            onTrainEnabled?.Invoke();
            _audioService?.PlaySfx("manyCoinsSkill");
            _audioService?.PlaySfx("trainSkill");
            yield return new WaitForSeconds(_trainAnimationDuration);
            
            if (_canvasGroup != null)
                yield return _canvasGroup.DOFade(0f, _fadeDuration).WaitForCompletion();

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void ApplyPadding(float left, float top, float right, float bottom)
        {
            if (_rectMask != null)
                _rectMask.padding = new Vector4(left, bottom, right, top);
        }
    }
}

