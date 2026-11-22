using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios;
using Game.Scripts.Services.Tutorial;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using TutorScripts;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static UnityEngine.GraphicsBuffer;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup
{
    public class TutorialHintsView : MonoBehaviour, IPopup
    {
        [SerializeField] private List<CanvasGroup> _tutorialGroups = new();
        [SerializeField] private List<GameObject> _backgrounds = new();
        private GameObject _currentBackground;
        [SerializeField] private List<TutorialHandManager> _tutorialHandManagers = new();
        [SerializeField] private Image _bgImage;
        [SerializeField] private Button _spinSecondGroupButton;
        [SerializeField] private Button _nextGroupFirstButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _toSixGroupButton;
        [SerializeField] private Button _firstSkillActivateButton;
        [SerializeField] private Button _skipThirdGroupButton;
        [SerializeField] private Button _skipFourthGroupButton;
        [SerializeField] private Button _skipSeventhGroupButton;
        private const float FadeDuration = 0.3f;
        private const float ScaleDuration = 0.3f;
        private const float BgFadeOutDuration = 0.25f;

        private TutorialHintsViewModel _vm;
        private IFeedbackService _audioService;
        private MainTutorialScreensController _tutorialController;
        private CanvasGroup _currentActiveGroup;
        private ITutorialScenario _currentScenario;
        private ITutorialUIService _tutorialUIService;
        private bool _isClosing;
        private IPopupService _popupService;

        public Image BGImage => _bgImage;
        public Button SpinSecondGroupButton => _spinSecondGroupButton;
        public Button NextGroupFirstButton => _nextGroupFirstButton;
        public Button ToSixGroupButton => _toSixGroupButton;
        public Button FirstSkillActivateButton => _firstSkillActivateButton;
        public Button SkipThirdGroupButton => _skipThirdGroupButton;
        public Button SkipFourthGroupButton => _skipFourthGroupButton;
        public Button SkipSeventhGroupButton => _skipSeventhGroupButton;
        public SkillsService SkillsService { get; private set; }

        public SlotGameManager GameManager { get; private set; }

        public static event Action OnSkipThirdHint;
        public static event Action OnSkipFourthHint;
        public static event Action OnSkipSeventhHint;

        public static void FireSkipThirdHint() => OnSkipThirdHint?.Invoke();
        public static void FireSkipFourthHint() => OnSkipFourthHint?.Invoke();
        public static void FireSkipSeventhHint() => OnSkipSeventhHint?.Invoke();

        public event Action OnClose;
        public event Action OnShow;

        [Inject]
        public void Construct(
            IFeedbackService audioService,
            ITutorialUIService tutorialUIService,
            MainTutorialScreensController tutorialController,
            SlotGameManager slotGameManager,
            IPopupService popupService)
        {
            _audioService = audioService;
            _tutorialUIService = tutorialUIService;
            _tutorialController = tutorialController;
            GameManager = slotGameManager;
            SkillsService = slotGameManager?.SkillsService;
            _popupService = popupService;
        }

        public void Bind(IPopupData data)
        {
            _vm = data as TutorialHintsViewModel;
            if (_skipButton != null)
                _skipButton.onClick.AddListener(SkipTutorial);
            if (_vm != null)
                OpenGroup(_vm.GroupToShow);
        }

        public void ShowHands()
        {
            for (var i = 0; i < _tutorialHandManagers.Count; i++)
                _tutorialHandManagers[i].HandClickAnim(_tutorialHandManagers[i].gameObject.transform);
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.3f);
            ShowHands();
        }

        private void SkipTutorial()
        {
            if (_tutorialController != null)
                _tutorialController.SkipTutorial();
            else
                Debug.LogError("[TutorialHintsView] Cannot skip tutorial - MainTutorialScreensController not found!");
        }

        private void PlayClickSound()
        {
            if (_audioService == null) return;
            _audioService.PlaySfx("click1");
        }

        private void OpenGroup(TutorialHintGroup group)
        {
            var groupIndex = (int)group;

            if (groupIndex < 0 || groupIndex >= _tutorialGroups.Count)
            {
                Debug.LogError($"Invalid tutorial group index: {groupIndex}");
                return;
            }

            _currentActiveGroup = _tutorialGroups[groupIndex];
            
            _currentActiveGroup.alpha = 0f;
            _currentActiveGroup.transform.localScale = Vector3.one * 0.8f;
            _currentActiveGroup.blocksRaycasts = true;
            _currentActiveGroup.interactable = true;
            _currentActiveGroup.gameObject.SetActive(true);

            _currentActiveGroup.DOFade(1f, FadeDuration).SetEase(Ease.OutCubic);
            _currentActiveGroup.transform.DOScale(Vector3.one, ScaleDuration).SetEase(Ease.OutBack);

            if (_currentActiveGroup.TryGetComponent<Animator>(out var screenAnimator)) {

                screenAnimator.Play("Default", 0, 0f);
                screenAnimator.SetTrigger("Default");
            }

            var groupName = GetGroupName(group);
            LocalEventLogger.SendEvent($"tutorial_hint_popup_{groupName}_opened");

            _currentScenario?.Cleanup();
            _currentScenario = TutorialScenarioFactory.CreateScenario(group);
            _currentScenario?.Execute(this, null);

            OnShow?.Invoke();
            _tutorialUIService?.NotifyHintsPopupShown();


            


            // BACKGROUND
            if (groupIndex < _backgrounds.Count)
            {
                _currentBackground = _backgrounds[groupIndex];

                _currentBackground.transform.localScale = Vector3.one * 2f;
                _currentBackground.SetActive(true);

                _currentBackground.transform
                    .DOScale(Vector3.one, ScaleDuration)
                    .SetEase(Ease.OutCubic);
            }
            else
            {
                Debug.LogWarning($"No background for group index: {groupIndex}");
            }

            

        }

        public void CloseCurrentGroup()
        {
            if (_currentActiveGroup == null)
            {
                ClosePopup();
                return;
            }

            PlayClickSound();

            _currentActiveGroup.DOFade(0f, FadeDuration).SetEase(Ease.InCubic);
            _currentActiveGroup.transform.DOScale(Vector3.one * 0.8f, ScaleDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    _currentActiveGroup.blocksRaycasts = false;
                    _currentActiveGroup.interactable = false;
                    _currentActiveGroup.gameObject.SetActive(false);
                    _currentActiveGroup = null;
                    ClosePopup();
                });
        }

        public void SwitchToGroup(TutorialHintGroup newGroup)
        {
            if (_currentActiveGroup == null)
            {
                OpenGroup(newGroup);
                return;
            }

            _currentActiveGroup.DOFade(0f, FadeDuration * 0.7f).SetEase(Ease.InCubic);
            _currentActiveGroup.transform.DOScale(Vector3.one * 0.8f, ScaleDuration * 0.7f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    _currentActiveGroup.blocksRaycasts = false;
                    _currentActiveGroup.interactable = false;
                    _currentActiveGroup.gameObject.SetActive(false);
                    _currentActiveGroup = null;
                    OpenGroup(newGroup);
                });
        }

        public void CloseWithAnimation()
        {
            PlayClickSound();

            if (_tutorialController != null)
                _tutorialController.StartRoll();
            else
                Debug.LogWarning("[TutorialHintsView] Cannot start roll - MainTutorialScreensController not found!");

            if (_currentActiveGroup != null)
            {
                _currentActiveGroup.DOFade(0f, FadeDuration * 0.5f).SetEase(Ease.InQuad);
                _currentActiveGroup.transform.DOScale(Vector3.one * 0.9f, ScaleDuration * 0.5f)
                    .SetEase(Ease.InBack);
            }

            if (_bgImage != null)
            {
                _bgImage.DOFade(0f, BgFadeOutDuration).SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (_currentActiveGroup != null)
                        {
                            _currentActiveGroup.blocksRaycasts = false;
                            _currentActiveGroup.interactable = false;
                            _currentActiveGroup.gameObject.SetActive(false);
                        }

                        ClosePopup();
                    });
            }
            else
            {
                if (_currentActiveGroup != null)
                {
                    _currentActiveGroup.blocksRaycasts = false;
                    _currentActiveGroup.interactable = false;
                    _currentActiveGroup.gameObject.SetActive(false);
                }

                ClosePopup();
            }
        }

        public void ClosePopup()
        {
            if (_isClosing) return;
            _isClosing = true;

            if (this == null || gameObject == null)
                return;

            _currentScenario?.Cleanup();
            _currentScenario = null;

            try
            {
                OnClose?.Invoke();
                _tutorialUIService?.NotifyHintsPopupClosed();
            }
            catch (MissingReferenceException)
            {
            }

            if (this != null)
                Destroy(gameObject);
        }

        public void CloseAndOpenGroup(TutorialHintGroup group)
        {
            if (_isClosing) return;
            _isClosing = true;

            _currentScenario?.Cleanup();
            _currentScenario = null;

            try
            {
                OnClose?.Invoke();
                _tutorialUIService?.NotifyHintsPopupClosed();
            }
            catch (MissingReferenceException) { }

            _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                new TutorialHintsViewModel(group)
            );
        }

        private string GetGroupName(TutorialHintGroup group)
        {
            return group switch
            {
                TutorialHintGroup.FirstGroup => "first_group",
                TutorialHintGroup.SecondGroup => "second_group",
                TutorialHintGroup.ThirdGroup => "third_group",
                TutorialHintGroup.FourthGroup => "fourth_group",
                _ => "unknown_group"
            };
        }
    }
}