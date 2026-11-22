using Game.Scripts.Game.GameRoot;
using Game.Scripts.Services.Analytics;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.GameRoot.Services.Scenes;
using Game.Scripts.Services.Tutorial;
using Lofelt.NiceVibrations;
using VContainer;
using Object = UnityEngine.Object;

namespace Game.Scripts.Services.Popups.SkillsConfirmationPopup
{
    public class SkillsConfirmationView : MonoBehaviour, IPopup
    {
        [SerializeField] private TMP_Text _bottomText;
        [SerializeField] private GameObject _topTextCatering;
        [SerializeField] private GameObject _topTextMidnight;
        [SerializeField] private GameObject _topTextBaggage;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private RectTransform _handImage;
        [SerializeField] private RectTransform _secondHandRoot;
        [SerializeField] private GameObject _tutorText;
        [SerializeField] private GameObject _raycasterBlocker;
        [SerializeField] private GameObject _hintPrefab;
        private GameObject _hint;
        
        private SkillsConfirmationViewModel _vm;
        private bool _isTutorial;
        public event Action OnClose;

        private IFeedbackService _feedbackService;
        private SkillsService _skillsService;
        private UIRootView _uiRootView;
        private ISceneService _sceneService;
        private ITutorialUIService _tutorialUIService;

        [Inject]
        public void Construct(IFeedbackService feedbackService, UIRootView uiRootView, ISceneService sceneService)
        {
            _feedbackService = feedbackService;
            _uiRootView = uiRootView;
            _sceneService = sceneService;
        }

        public void InjectTutorial(ITutorialUIService tutorialUIService)
        {
            _tutorialUIService = tutorialUIService;
        }

        public void Bind(IPopupData data)
        {
            _vm = data as SkillsConfirmationViewModel;
            if (_vm?.TutorialUIService != null)
                _tutorialUIService = _vm.TutorialUIService;
            if (_vm?.SkillsService != null)
                _skillsService = _vm.SkillsService;

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);

            var isTutorial = _sceneService != null && _sceneService.IsCurrentScene(Scenes.TUTORIAL);
            _isTutorial = isTutorial;
            if (isTutorial)
            {
                _cancelButton.gameObject.SetActive(false);
                LocalEventLogger.SendEvent("ftue_skill_confirmation_shown");
                var rt = _confirmButton.image.rectTransform;
                if (rt != null)
                    rt.anchoredPosition = new Vector2(0f, 212.6f);

                if (_handImage != null)
                    _handImage.gameObject.SetActive(false);

                var hintHand = _tutorialUIService?.HintHand;
                if (hintHand != null)
                    hintHand.gameObject.SetActive(true);

                if (_tutorText != null) _tutorText.SetActive(true);
                if (_raycasterBlocker != null) _raycasterBlocker.SetActive(false);

                _hint = Instantiate(_hintPrefab);
            }
            else
            {
                if (_handImage != null)
                    _handImage.gameObject.SetActive(false);
                if (_tutorText != null) _tutorText.SetActive(false);
            }

            if (_uiRootView != null)
                _uiRootView.UpdateCanvasOrder(0);

            if (_vm == null)
            {
                Debug.LogError("[SkillsConfirmationView] Popup data is null or wrong type.");
                return;
            }

            var bottomMessage = _vm.SkillType switch
            {
                SkillType.СateringTrolley => "The Dining Car destroys a cross-shaped area,\nclearing the selected tile and the tiles directly above,\nbelow, left, and right.",
                SkillType.MidnightRoute => "The train creates a chain of random multipliers\nacross the board that boost any winning cluster\npassing through them.",
                SkillType.BaggageTrolley => "All matching symbols on the board explode, making\nroom for new symbols and cascading wins.",
                _ => string.Empty
            };
            
            _bottomText.text = bottomMessage;
            
            ShowTopText(_vm.SkillType);

            _confirmButton.interactable = false;

            if (_skillsService == null)
            {
                Debug.LogError("[SkillsConfirmationView] SkillsService is null; cannot bind.");
                return;
            }

            _skillsService.OnSelectionChanged += UpdateConfirmButton;
            _skillsService.OnSelectionChanged += UpdateTexts;
            
            void OnCloseHandler()
            {
                _skillsService.OnSelectionChanged -= UpdateConfirmButton;
                _skillsService.OnSelectionChanged -= UpdateTexts;
            }
            OnClose += OnCloseHandler;

            UpdateConfirmButton();
            UpdateTexts();

            _confirmButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                _skillsService.ConfirmActiveSkill();
            });

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    _skillsService.CancelActiveSkill();
                    ClosePopup();
                });
            }
        }

        private void UpdateConfirmButton()
        {
            if (_skillsService == null) return;
            var hasPending = _skillsService.HasPendingSelection();
            _confirmButton.interactable = hasPending;

            if (_isTutorial && hasPending)
            {
                var hintHand = _tutorialUIService?.HintHand;
                if (hintHand != null)
                    hintHand.gameObject.SetActive(false);

                if (_handImage != null && _secondHandRoot != null)
                {
                    if (_handImage.parent != _secondHandRoot)
                    {
                        var animator = _handImage.GetComponent<HandTutorAnimator>();
                        if (animator != null) animator.StopAndReset();

                        _handImage.SetParent(_secondHandRoot, false);
                        _handImage.anchoredPosition = Vector2.zero;

                        if (animator != null)
                        {
                            animator.CaptureState();
                            animator.Play();
                        }
                    }
                    _handImage.gameObject.SetActive(true);
                    if (_hint != null)
                    {
                        Destroy(_hint);
                    }
                }
            }
        }

        private void UpdateTexts()
        {
            if (_skillsService == null) return;
            var remainingCharges = _skillsService.GetRemainingCharges();
        }

        private void PlayClickSound()
        {
            if (_feedbackService == null) return;
            _feedbackService.PlaySfx("click1");
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
        }

        private void ShowTopText(SkillType skillType)
        {
            if (_topTextCatering) _topTextCatering.SetActive(skillType == SkillType.СateringTrolley);
            if (_topTextMidnight) _topTextMidnight.SetActive(skillType == SkillType.MidnightRoute);
            if (_topTextBaggage) _topTextBaggage.SetActive(skillType == SkillType.BaggageTrolley);
        }

        public void ClosePopup()
        {
            var hintHand = _tutorialUIService?.HintHand;
            if (hintHand != null)
                hintHand.gameObject.SetActive(false);

            if (_uiRootView != null)
                _uiRootView.UpdateCanvasOrder(10);
            OnClose?.Invoke();
            Destroy(gameObject);
        }
    }
}