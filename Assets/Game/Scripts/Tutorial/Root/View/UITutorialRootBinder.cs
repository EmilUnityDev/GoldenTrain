using System;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Score;
using Game.Scripts.Services.Tutorial;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SpinSystem.States;
using TutorScripts;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Tutorial.Root.View
{
    public class UITutorialRootBinder : MonoBehaviour
    {
        private IFeedbackService _audioService;

        [Header("Buttons")] [Space] [SerializeField]
        private Button _skipTutorialButton;

        [Header("Skills")] [SerializeField] private Button _firstSkillButton;
        [SerializeField] private Button _secondSkillButton;
        [SerializeField] private Button _thirdSkillButton;

        [Header("Skill Charges UI")] 
        [SerializeField] private Image _sharedSkillFill;

        [Header("Skill Effects")]
        [SerializeField] private GameObject _firstSkillEffect;
        [SerializeField] private GameObject _secondSkillEffect;
        [SerializeField] private GameObject _thirdSkillEffect;

        [Header("Spin")] [SerializeField] private Button _spinButton;
        [SerializeField] private Button _skipButton;

        [SerializeField] private GameObject _handHint;
        
        private SkillsService _skillsService;
        private SpinSystem.SpinSystem _spinSystem;
        private IPopupService _popupService;
        private SlotGameManager _gameManager;
        private MainTutorialScreensController _tutorialController;
        private ITutorialUIService _tutorialUIService;

        [Inject]
        public void Construct(
            IFeedbackService audioService,
            IPopupService popupService,
            ScoreService scoreService,
            SlotGameManager gameManager,
            MainTutorialScreensController tutorialController,
            ITutorialUIService tutorialUIService)
        {
            _audioService = audioService;
            _popupService = popupService;
            _gameManager = gameManager;
            _tutorialController = tutorialController;
            _tutorialUIService = tutorialUIService;
        }

        public void Initialize(SkillsService skillsService, SpinSystem.SpinSystem spinSystem)
        {
            _skillsService = skillsService;
            _spinSystem = spinSystem;

            if (_skillsService != null)
            {
                _skillsService.OnCooldownChanged += UpdateSkillChargesUI;
                _skillsService.OnSkillActivated += OnSkillActivated;
                _skillsService.OnSkillFinished += OnSkillFinished;
                _skillsService.OnSkillCancelled += OnSkillFinished;
                InitializeSkillChargesUI();
                UpdateButtonsInteractable();
                Debug.Log("[UITutorialRootBinder] Initialized with SkillsService and SpinSystem!");
            }
        }

        private void EnsureServices()
        {
            if (_gameManager != null)
            {
                if (_skillsService == null)
                    _skillsService = _gameManager.SkillsService;
                if (_spinSystem == null)
                    _spinSystem = _gameManager.SpinSystem;
            }
        }

        private void Start()
        {
            EnsureServices();

            if (_spinSystem == null || _skillsService == null)
            {
                Debug.LogError("[UITutorialRootBinder] Cannot initialize - SpinSystem or SkillsService is null!");
                return;
            }

            if (_handHint != null)
                _handHint.SetActive(false);

            if (_firstSkillEffect != null) _firstSkillEffect.SetActive(false);
            if (_secondSkillEffect != null) _secondSkillEffect.SetActive(false);
            if (_thirdSkillEffect != null) _thirdSkillEffect.SetActive(false);

            _spinButton.onClick.AddListener(() => _spinSystem.Spin());
            _spinButton.onClick.AddListener(() => LocalEventLogger.SendEvent("spin_button_clicked"));
            _spinButton.onClick.AddListener(OnSpinButtonClicked);
            _skipTutorialButton.onClick.AddListener(SkipTutorial);

            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
                _skipButton.onClick.AddListener(OnSkipButtonClicked);
                _spinSystem.OnStateChanged += OnSpinStateChanged;
            }

            if (_firstSkillButton != null)
            {
                _firstSkillButton.onClick.AddListener(() =>
                    _skillsService.TryActivateSkill(SkillType.СateringTrolley, _gameManager));
                _firstSkillButton.onClick.AddListener(() =>
                    LocalEventLogger.SendEvent("first_skill_used"));
            }

            if (_secondSkillButton != null)
            {
                _secondSkillButton.onClick.AddListener(() =>
                    _skillsService.TryActivateSkill(SkillType.BaggageTrolley, _gameManager));
                _secondSkillButton.onClick.AddListener(() =>
                    LocalEventLogger.SendEvent("second_skill_used"));
            }

            if (_thirdSkillButton != null)
            {
                _thirdSkillButton.onClick.AddListener(() =>
                {
                    EnsureServices();
                    if (_skillsService != null)
                        _skillsService.TryActivateSkill(SkillType.MidnightRoute, _gameManager);
                });
                _thirdSkillButton.onClick.AddListener(() =>
                    LocalEventLogger.SendEvent("third_skill_used"));
            }

            UpdateButtonsInteractable(false);
        }

        public void UpdateButtonsInteractable(bool isInteractable = true)
        {
            if (isInteractable && _skillsService != null)
            {
                int charges = _skillsService.GetCharges(SkillType.СateringTrolley);
                if (_firstSkillButton != null) _firstSkillButton.interactable = charges >= 1;
                if (_secondSkillButton != null) _secondSkillButton.interactable = charges >= 2;
                if (_thirdSkillButton != null) _thirdSkillButton.interactable = charges >= 3;
            }
            else
            {
                if (_firstSkillButton != null) _firstSkillButton.interactable = false;
                if (_secondSkillButton != null) _secondSkillButton.interactable = false;
                if (_thirdSkillButton != null) _thirdSkillButton.interactable = false;
            }

            AudioListener.volume = 1;
        }

        private void OnDestroy()
        {
            if (_skillsService != null)
            {
                _skillsService.OnCooldownChanged -= UpdateSkillChargesUI;
                _skillsService.OnSkillActivated -= OnSkillActivated;
                _skillsService.OnSkillFinished -= OnSkillFinished;
                _skillsService.OnSkillCancelled -= OnSkillFinished;
            }

            if (_spinSystem != null)
                _spinSystem.OnStateChanged -= OnSpinStateChanged;
        }

        private void InitializeSkillChargesUI()
        {
            if (_skillsService == null) return;

            if (_skillsService.Cooldowns.TryGetValue(SkillType.СateringTrolley, out var cooldown))
            {
                UpdateSkillChargesUI(SkillType.СateringTrolley, cooldown);
            }
        }

        private void UpdateSkillChargesUI(SkillType type, float fillAmount)
        {
            if (type == SkillType.СateringTrolley)
            {
                if (_sharedSkillFill != null) _sharedSkillFill.DOFillAmount(fillAmount, 0.3f);
            }
        }

        private void SkipTutorial()
        {
            _tutorialController.SkipTutorial();
        }

        private void OnSpinButtonClicked()
        {
            if (_handHint != null)
                _handHint.SetActive(false);
        }

        private bool _afterSkillCalcPending;

        private void OnSpinStateChanged(Type stateType)
        {
            if (stateType == typeof(CalculationState) && _spinSystem.IsCalculationAfterSkill)
                _afterSkillCalcPending = true;

            if (stateType == typeof(IdleState))
            {
                if (_afterSkillCalcPending)
                {
                    _afterSkillCalcPending = false;
                    if (_handHint != null)
                    {
                        _handHint.SetActive(true);
                    }
                }

                if (_skipButton != null)
                {
                    _spinButton.gameObject.SetActive(true);
                    _skipButton.gameObject.SetActive(false);
                }
                UpdateButtonsInteractable(true);
            }
            else
            {
                if (_skipButton != null)
                {
                    if (stateType == typeof(SpinningState) || stateType == typeof(CalculationState))
                    {
                        _spinButton.gameObject.SetActive(false);
                        _skipButton.gameObject.SetActive(true);
                    }
                }
                UpdateButtonsInteractable(false);
            }
        }

        private void OnSkipButtonClicked()
        {
            if (_spinSystem == null) return;

            var currentState = _spinSystem.CurrentStateType;
            if (currentState == typeof(SpinningState))
                _spinSystem.SkipSpinningAnimation();
            else if (currentState == typeof(CalculationState)) _spinSystem.SkipCalculationAnimation();
        }

        private void OnSkillActivated(SkillType type)
        {
            switch (type)
            {
                case SkillType.СateringTrolley:
                    if (_firstSkillEffect != null) _firstSkillEffect.SetActive(true);
                    break;
                case SkillType.BaggageTrolley:
                    if (_secondSkillEffect != null) _secondSkillEffect.SetActive(true);
                    break;
                case SkillType.MidnightRoute:
                    if (_thirdSkillEffect != null) _thirdSkillEffect.SetActive(true);
                    break;
            }
        }

        private void OnSkillFinished(SkillType type)
        {
            switch (type)
            {
                case SkillType.СateringTrolley:
                    if (_firstSkillEffect != null) _firstSkillEffect.SetActive(false);
                    break;
                case SkillType.BaggageTrolley:
                    if (_secondSkillEffect != null) _secondSkillEffect.SetActive(false);
                    break;
                case SkillType.MidnightRoute:
                    if (_thirdSkillEffect != null) _thirdSkillEffect.SetActive(false);
                    break;
            }
        }
    }
}