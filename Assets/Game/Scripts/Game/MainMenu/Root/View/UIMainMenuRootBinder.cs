using System.Collections;
using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.DataStorage;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Scenes;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Popups.CollectionsPopup.Collections;
using Game.Scripts.Services.Popups.DailyPopup;
using Game.Scripts.Services.Popups.MapPopup;
using Game.Scripts.Services.Popups.SettingsPopup;
using Game.Scripts.Services.Popups.TrainPopup;
using Game.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Game.MainMenu.Root.View
{
    public class UIMainMenuRootBinder : MonoBehaviour
    {
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _dailyQuestsButton;
        [SerializeField] private Button _trainPopupButton;
        [SerializeField] private Button _mapPopupButton;
        [SerializeField] private Button _collectionPopupButton;
        [SerializeField] private Button _homeButton;

        [SerializeField] private GameObject _blueBG;
        [SerializeField] public GameObject _mainTrain;
        [SerializeField] private GameObject _buttonsPanel;
        [SerializeField] private List<GameObject> _trainObjects;

        [Header("Background")] 
        [SerializeField] private Image _mainBGRenderer;
        [SerializeField] private Sprite _secondLevelBG;
        
        private IFeedbackService _audioService;
        private IPopupService _popupService;
        private IUIOverlayService _overlayService;
        private UIRootView _uiRoot;
        private Coroutines _coroutines;
        private GameDataRepository _repository;
        private LevelProgressService _levelProgressService;
        private ISceneService _sceneService;

        private Transform _buttonsPanelOriginalParent;
        private int _buttonsPanelOriginalSiblingIndex;

        
        private TMP_Text _trainButtonText;
        private TMP_Text _mapButtonText;
        private TMP_Text _collectionButtonText;
        private TMP_Text _homeButtonText;

        private CanvasGroup _trainButtonCG;
        private CanvasGroup _mapButtonCG;
        private CanvasGroup _collectionButtonCG;
        private CanvasGroup _homeButtonCG;

        private static readonly Color ActiveColor = new Color(1f, 0.855f, 0.267f);
        private static readonly Color DefaultColor = Color.white;

        private enum ActivePopup { None, Train, Map, Collection }
        private ActivePopup _currentActive = ActivePopup.None;

        [Inject]
        public void Construct(
            IFeedbackService audioService,
            IPopupService popupService,
            IUIOverlayService overlayService,
            UIRootView uiRoot,
            Coroutines coroutines,
            GameDataRepository repository,
            LevelProgressService levelProgressService,
            ISceneService sceneService)
        {
            _audioService = audioService;
            _popupService = popupService;
            _overlayService = overlayService;
            _uiRoot = uiRoot;
            _coroutines = coroutines;
            _repository = repository;
            _levelProgressService = levelProgressService;
            _sceneService = sceneService;
        }

        private void Start()
        {
            if (_levelProgressService.Level > 1 && _mainBGRenderer != null && _secondLevelBG != null)
            {
                _mainBGRenderer.sprite = _secondLevelBG;
            }

            _buttonsPanelOriginalParent = _buttonsPanel.transform.parent;
            _buttonsPanelOriginalSiblingIndex = _buttonsPanel.transform.GetSiblingIndex();
            _blueBG.SetActive(false);

            _trainButtonText = _trainPopupButton.GetComponentInChildren<TMP_Text>();
            _mapButtonText = _mapPopupButton.GetComponentInChildren<TMP_Text>();
            _collectionButtonText = _collectionPopupButton.GetComponentInChildren<TMP_Text>();
            _homeButtonText = _homeButton.GetComponentInChildren<TMP_Text>();

            _trainButtonCG = _trainPopupButton.GetComponentInChildren<CanvasGroup>();
            _mapButtonCG = _mapPopupButton.GetComponentInChildren<CanvasGroup>();
            _collectionButtonCG = _collectionPopupButton.GetComponentInChildren<CanvasGroup>();
            _homeButtonCG = _homeButton.GetComponentInChildren<CanvasGroup>();

            RefreshNavButtons(ActivePopup.None);

            _settingsButton?.onClick.AddListener(OpenSettings);
            _playButton?.onClick.AddListener(StartGame);
            _dailyQuestsButton?.onClick.AddListener(OpenDailyQuests);
            _trainPopupButton?.onClick.AddListener(OpenTrainPopup);
            _mapPopupButton?.onClick.AddListener(OpenMapPopup);
            _collectionPopupButton?.onClick.AddListener(OpenCollectionPopup);
            _homeButton?.onClick.AddListener(CloseCurrentPopupHome);

            UpdateActiveTrain();
        }

        public void UpdateActiveTrain()
        {
            if (_trainObjects == null || _trainObjects.Count == 0) return;
            int selected = _repository.Data.SelectedTrainIndex;
            for (int i = 0; i < _trainObjects.Count; i++)
                if (_trainObjects[i] != null)
                    _trainObjects[i].SetActive(i == selected);
        }

        private void RefreshNavButtons(ActivePopup active)
        {
            _currentActive = active;
            
            _trainButtonText.color = active == ActivePopup.Train ? ActiveColor : DefaultColor;
            _mapButtonText.color = active == ActivePopup.Map ? ActiveColor : DefaultColor;
            _collectionButtonText.color = active == ActivePopup.Collection ? ActiveColor : DefaultColor;
            _homeButtonText.color = active == ActivePopup.None ? ActiveColor : DefaultColor;
            
            SetCG(_trainButtonCG, active == ActivePopup.Train);
            SetCG(_mapButtonCG, active == ActivePopup.Map);
            SetCG(_collectionButtonCG, active == ActivePopup.Collection);
            SetCG(_homeButtonCG, active == ActivePopup.None);
            
            _trainPopupButton.interactable = active != ActivePopup.Train;
            _mapPopupButton.interactable = active != ActivePopup.Map;
            _collectionPopupButton.interactable = active != ActivePopup.Collection;
            _homeButton.interactable = active != ActivePopup.None;
        }

        private void SetCG(CanvasGroup cg, bool enabled)
        {
            if (cg == null) return;
            cg.gameObject.SetActive(enabled);
        }

        private void OnPopupClosed()
        {
            RefreshNavButtons(ActivePopup.None);
        }

        private void CloseCurrentPopupHome()
        {
            _blueBG.SetActive(false);
            _playButton.gameObject.SetActive(true);
            _dailyQuestsButton.gameObject.SetActive(true);
            _mainTrain.SetActive(true);
            UpdateActiveTrain();
            _buttonsPanel.transform.SetParent(_buttonsPanelOriginalParent, false);
            _buttonsPanel.transform.SetSiblingIndex(_buttonsPanelOriginalSiblingIndex);
            _popupService.CloseCurrentPopup();
            RefreshNavButtons(ActivePopup.None);
        }

        private void OpenDailyQuests()
        {
            _popupService.ShowPopup<DailyPopupView>();
        }

        public void StartGame()
        {
            _playButton.interactable = false;
            _settingsButton.interactable = false;
            PlayClickSound();
            _overlayService.ShowLoading();
            LocalEventLogger.SendEvent("start_game_from_main_menu");
            _coroutines.StartCoroutine(StartLocalGame());
        }

        private IEnumerator StartLocalGame()
        {
            yield return new WaitForSeconds(0.3f);
            _uiRoot.ClearSceneUI();
            _overlayService.HideAllLoading();
            _sceneService.LoadScene(Scenes.GAMEPLAY);
        }

        private void PlayClickSound() => _audioService.PlaySfx("ClickSound");

        private void OpenSettings()
        {
            _popupService.ShowPopup<SettingsPopupView>();
            _mainTrain.SetActive(true);
            UpdateActiveTrain();
        }

        private void OpenTrainPopup()
        {
            _playButton.gameObject.SetActive(false);
            _dailyQuestsButton.gameObject.SetActive(false);
            _mainTrain.SetActive(false);
            RefreshNavButtons(ActivePopup.Train);
            _popupService.ShowPopup<TrainPopupView>(onPopupClosed: () =>
            {
                _playButton.gameObject.SetActive(true);
                _dailyQuestsButton.gameObject.SetActive(true);
                if (_currentActive == ActivePopup.Train)
                    OnPopupClosed();
            });
        }

        private void OpenMapPopup()
        {
            _mainTrain.SetActive(false);
            RefreshNavButtons(ActivePopup.Map);
            _popupService.ShowPopup<MapPopupView>(onPopupClosed: () =>
            {
                _buttonsPanel.transform.SetParent(_buttonsPanelOriginalParent, false);
                _buttonsPanel.transform.SetSiblingIndex(_buttonsPanelOriginalSiblingIndex);
                if (_currentActive == ActivePopup.Map)
                    OnPopupClosed();
            });
            _buttonsPanel.transform.SetParent(_uiRoot.PopupContainer, false);
        }

        private void OpenCollectionPopup()
        {
            _mainTrain.SetActive(false);
            _blueBG.SetActive(true);
            RefreshNavButtons(ActivePopup.Collection);
            _popupService.ShowPopup<CollectionsPopupView>(onPopupClosed: () =>
            {
                _blueBG.SetActive(false);
                if (_currentActive == ActivePopup.Collection)
                    OnPopupClosed();
            });
        }
    }
}