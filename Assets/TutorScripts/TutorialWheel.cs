using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TutorScripts
{
    public class TutorialWheel : MonoBehaviour
    {
        public enum WheelPrizeType
        {
            SkillAndPoints,
            Skill,
            Points3k,
            Points5k,
            Points10k,
            Spins,
            Empty
        }

        [SerializeField] private float Speed;
        [SerializeField] private GameObject Wheel;
        [SerializeField] private CanvasGroup shineImage;
        [SerializeField] private Button startButton;
        [SerializeField] private List<CanvasGroup> hintCanvasGroups;
        [SerializeField] private CanvasGroup winnerCanvasGroup;
        [SerializeField] private List<CanvasGroup> wheelsCanvasGroups;
        [SerializeField] private TutorialHandManager skillHandManager;
        [SerializeField] private TutorialHandManager wheelHandManager;
        [SerializeField] private Transform skillTransform;
        [SerializeField] private Transform wheelTransform;
        [SerializeField] private CanvasGroup thirdHint;
        [SerializeField] private Animator thirdHintController;
        [SerializeField] private TutorialHandManager thirdHintManager;
        [SerializeField] private CanvasGroup titleCanvasGroup;

        private WheelPrizeType _prizeType;
        private bool isSpinning;
        private bool stopRequested;

        [Header("Fixed stop angle")] [SerializeField]
        private float targetAngle = 312f;

        [SerializeField] private float slowDownTime = 2f;
        [SerializeField] private int extraSpins = 2;

        private SkillsService _skillsService;
        private IFeedbackService _audioService;
        private Coroutine _spinRoutine;

        [Inject]
        public void Construct(SkillsService skillsService, IFeedbackService audioService)
        {
            _skillsService = skillsService;
            _audioService = audioService;
        }

        private void Start()
        {
            startButton.onClick.AddListener(OnStartStopClicked);
            wheelHandManager.HandClickAnim(wheelTransform);
            skillHandManager.HandClickAnim(skillTransform);
        }

        public void ClosePanels()
        {
            for (var i = 0; i < wheelsCanvasGroups.Count; i++)
            {
                var cg = wheelsCanvasGroups[i];
                cg.DOFade(0, 0.25f).OnComplete(() =>
                {
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                });
            }

            thirdHint.interactable = true;
            thirdHint.blocksRaycasts = true;
            thirdHintController.SetTrigger("Appear");

            thirdHint.DOFade(1, 0.25f).OnComplete(() =>
            {
                thirdHintManager.HandClickAnim(thirdHintManager.transform);
            });
        }

        private void OnStartStopClicked()
        {
            if (!isSpinning)
            {
                for (var i = 0; i < hintCanvasGroups.Count; i++) hintCanvasGroups[i].DOFade(0, 0.25f);

                titleCanvasGroup.DOFade(1, 0.25f);
                Speed = 3f;
                isSpinning = true;
                stopRequested = false;

                startButton.GetComponentInChildren<TMP_Text>().text = "STOP";

                if (_spinRoutine != null)
                    StopCoroutine(_spinRoutine);

                _spinRoutine = StartCoroutine(RotateWheel());
            }
            else
            {
                stopRequested = true;
                wheelHandManager.gameObject.SetActive(false);
                startButton.gameObject.SetActive(false);
            }
        }

        private IEnumerator RotateWheel()
        {
            isSpinning = true;
            stopRequested = false;

            while (!stopRequested)
            {
                Wheel.transform.Rotate(0, 0, -Speed);
                yield return new WaitForEndOfFrame();
            }

            yield return StartCoroutine(SlowDownToTarget());

            shineImage.DOFade(1, 0.25f);
            yield return new WaitForSeconds(1f);
            winnerCanvasGroup.interactable = true;
            winnerCanvasGroup.blocksRaycasts = true;
            winnerCanvasGroup.DOFade(1, 0.25f);

            _audioService?.PlaySfx("bonusGameWin");
            CheckYourWinnings();
        }

        private IEnumerator SlowDownToTarget()
        {
            isSpinning = false;

            var startAngle = Wheel.transform.eulerAngles.z;
            var deltaToTarget = Mathf.Repeat(startAngle - targetAngle + 360f, 360f);
            var totalRotation = deltaToTarget + 360f * extraSpins;
            var v = Speed;
            var a = -(v * v) / (2f * totalRotation);

            var travelled = 0f;

            while (travelled < totalRotation)
            {
                var step = v;
                if (travelled + step > totalRotation)
                    step = totalRotation - travelled;

                Wheel.transform.Rotate(0f, 0f, -step);
                travelled += step;
                v += a;
                if (v < 0f)
                    v = 0f;

                yield return null;
            }

            Wheel.transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        }


        private void CheckYourWinnings()
        {
            var angle = Wheel.transform.eulerAngles.z;

            _prizeType = angle switch
            {
                > 0 and < 10 => WheelPrizeType.Empty,
                > 10 and < 34 => WheelPrizeType.Points10k,
                > 34 and < 56 => WheelPrizeType.Skill,
                > 56 and < 78 => WheelPrizeType.Spins,
                > 78 and < 100 => WheelPrizeType.Empty,
                > 100 and < 122 => WheelPrizeType.Spins,
                > 122 and < 145 => WheelPrizeType.SkillAndPoints,
                > 145 and < 169 => WheelPrizeType.Points10k,
                > 169 and < 191 => WheelPrizeType.Empty,
                > 191 and < 214 => WheelPrizeType.Points5k,
                > 214 and < 237 => WheelPrizeType.Spins,
                > 237 and < 258 => WheelPrizeType.Points3k,
                > 258 and < 280 => WheelPrizeType.Empty,
                > 280 and < 302 => WheelPrizeType.Skill,
                > 302 and < 324 => WheelPrizeType.SkillAndPoints,
                > 324 and < 348 => WheelPrizeType.Skill,
                > 348 and <= 360 => WheelPrizeType.Empty,
                _ => _prizeType
            };

            StartCoroutine(ShowPrize());
        }

        public IEnumerator ShowPrize()
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
}