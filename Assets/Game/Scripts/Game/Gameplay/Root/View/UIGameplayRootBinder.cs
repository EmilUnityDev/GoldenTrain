using System;
using DG.Tweening;
using Game.Scripts.Game.Gameplay.Services;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Popups.SettingsPopup;
using Game.Scripts.Services.Popups.SkillsPopup;
using Game.Scripts.Services.Score;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Game.Gameplay.Root.View
{
    public class UIGameplayRootBinder : MonoBehaviour
    {
        private IFeedbackService _audioService;
        [Header("Buttons")] 
        [Space] [SerializeField] private Button _exitButton;
        [Space] [SerializeField] private Button _settingsButton;
        [SerializeField] private TimerComponent _timer;
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
        private SkillsService _skillsService;
        private SpinSystem.SpinSystem _spinSystem;
        private IPopupService _popupService;
        private SlotGameManager _gameManager;

        [Inject]
        public void Construct(
            IFeedbackService audioService,
            IPopupService popupService,
            ScoreService scoreService,
            SlotGameManager gameManager)
        {
            _audioService = audioService;
            _popupService = popupService;
            _gameManager = gameManager;
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
                Debug.LogError("[UIGameplayRootBinder] Missing SpinSystem or SkillsService.");
                return;
            }

            if (_firstSkillEffect != null) _firstSkillEffect.SetActive(false);
            if (_secondSkillEffect != null) _secondSkillEffect.SetActive(false);
            if (_thirdSkillEffect != null) _thirdSkillEffect.SetActive(false);

            _spinButton.onClick.AddListener(() => _spinSystem.Spin());
            _spinButton.onClick.AddListener(() => LocalEventLogger.SendEvent("spin_button_clicked"));
            
            _exitButton.onClick.AddListener(OpenAbortPopup);
            _settingsButton.onClick.AddListener(OpenSettingsPopup); 
            
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
                _skipButton.onClick.AddListener(OnSkipButtonClicked);
                _spinSystem.OnStateChanged += OnSpinStateChanged;
            }
            
            if (_timer != null)
            {
                _timer.Initialize();
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
                    _skillsService.TryActivateSkill(SkillType.MidnightRoute, _gameManager));
                _thirdSkillButton.onClick.AddListener(() =>
                    LocalEventLogger.SendEvent("third_skill_used"));
            }

            UpdateButtonsInteractable(false);
        }

        private void UpdateButtonsInteractable(bool isInteractable = true) {
            if (isInteractable) {
                int charges = _skillsService.GetCharges(SkillType.СateringTrolley); 
                if (_firstSkillButton != null) _firstSkillButton.interactable = charges >= 1;
                if (_secondSkillButton != null) _secondSkillButton.interactable = charges >= 2;
                if (_thirdSkillButton != null) _thirdSkillButton.interactable = charges >= 3;
            } else {
                if (_firstSkillButton != null) _firstSkillButton.interactable = false;
                if (_secondSkillButton != null) _secondSkillButton.interactable = false;
                if (_thirdSkillButton != null) _thirdSkillButton.interactable = false;
            }
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
            UpdateSkillChargesUI(SkillType.СateringTrolley, _skillsService.Cooldowns[SkillType.СateringTrolley]);
        }

        private void UpdateSkillChargesUI(SkillType type, float fillAmount) {
            if (type == SkillType.СateringTrolley)
            {
                if (_sharedSkillFill != null) _sharedSkillFill.DOFillAmount(fillAmount, 0.3f);
            }
        }

        private void OpenAbortPopup()
        {
            _popupService.ShowPopup<AbortPopupView>();
        }
        
        private void OpenSettingsPopup()
        {
            _popupService.ShowPopup<SettingsPopupView>();
        }


        private void OnSpinStateChanged(Type stateType) {
            if (_skipButton == null) return;

            if (stateType == typeof(SpinSystem.States.IdleState)) {
                _spinButton.gameObject.SetActive(true);
                _skipButton.gameObject.SetActive(false);
                UpdateButtonsInteractable(true);
            } else {
                _spinButton.gameObject.SetActive(false);
                _skipButton.gameObject.SetActive(true);
                UpdateButtonsInteractable(false);
            }
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

        private void OnSkipButtonClicked()
        {
            if (_spinSystem == null) return;
            
            var currentState = _spinSystem.CurrentStateType;
            if (currentState == typeof(SpinSystem.States.SpinningState))
            {
                _spinSystem.SkipSpinningAnimation();
            }
            else if (currentState == typeof(SpinSystem.States.CalculationState))
            {
                _spinSystem.SkipCalculationAnimation();
            }
        }
    }
}