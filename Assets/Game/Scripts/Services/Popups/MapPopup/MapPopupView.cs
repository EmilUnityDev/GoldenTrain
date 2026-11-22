using System;
using DG.Tweening;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.Game.MainMenu.Root.View;
using Game.Scripts.GameRoot.Services.DataStorage.Interfaces;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Money;
using Game.Scripts.Services.Popups.SettingsPopup;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Popups.MapPopup
{
    public class MapPopupView : MonoBehaviour, IPopup
    {
        public event Action OnClose;

        [SerializeField] private Image _backImage;
        [SerializeField] private Button _settingsButton;
        
        [SerializeField] private MapTrainController trainController;
        [SerializeField] private CityMapPoint[] cityPoints;
        [SerializeField] private CityInfoPanel cityInfoPanel;

        private IPopupService _popupService;
        private MoneyService _moneyService;
        private CollectionUnlockService _collectionUnlockService;
        private MapProgressService _mapProgressService;
        private UIMainMenuRootBinder _mainMenuRoot;

        
        public void Bind(IPopupData data)
        {
            PopupAnimationHandler.PlaySpawnAnimation(gameObject);
            _settingsButton.onClick.AddListener(OpenSettings);
            Refresh();
        }
        
        [Inject]
        public void Construct(
            IPopupService popupService,
            CollectionUnlockService collectionUnlockService,
            MoneyService moneyService,
            MapProgressService mapProgressService,
            UIMainMenuRootBinder uiMainMenuRoot)
        {
            _popupService = popupService;
            _collectionUnlockService = collectionUnlockService;
            _moneyService = moneyService;
            _mapProgressService = mapProgressService;
            _mainMenuRoot = uiMainMenuRoot;
        }
        private void OpenSettings()
        {
            _mainMenuRoot._mainTrain.SetActive(true);
            _mainMenuRoot.UpdateActiveTrain();
            _popupService.ShowPopup<SettingsPopupView>();
        }

        private void OnEnable()
        {
            if (_mapProgressService == null)
                return;

            _mapProgressService.OnProgressChanged += HandleProgressChanged;
            _mapProgressService.OnCounterChanged += HandleCounterChanged;
            _mapProgressService.OnFirstCityReached += HandleFirstCityReached;

            Refresh();
        }

        private void OnDisable()
        {
            if (_mapProgressService == null)
                return;

            _mapProgressService.OnProgressChanged -= HandleProgressChanged;
            _mapProgressService.OnCounterChanged -= HandleCounterChanged;
            _mapProgressService.OnFirstCityReached -= HandleFirstCityReached;
        }
        private void HandleCounterChanged(int counter)
        {
            int progress = _mapProgressService.Progress;
            trainController.MoveToProgressWithCounter(progress, counter, true);
        }

        private void HandleFirstCityReached()
        {
            Debug.Log("First city reached");
        }

        private void HandleProgressChanged(int progress)
        {
            Refresh();
        }
        private void Refresh()
        {
            if (_mapProgressService == null)
                return;

            int progress = _mapProgressService.Progress;

            foreach (var city in cityPoints)
            {
                city.Init(progress, OnCityClicked);
            }

            trainController.MoveToProgressWithCounter(progress, _mapProgressService.Counter, false);
        }

        private void OnCityClicked(CityMapPoint city)
        {
            if (city.IsFirstCity)
            {
                OpenFirstCity(city);
                return;
            }
            Debug.Log($"Clicked city with progress: {city.RequiredProgress}");
        }

        private void OpenFirstCity(CityMapPoint city)
        {
            if (_mapProgressService.IsFirstCityRewardClaimed)
                return;

            cityInfoPanel.Open(city.CityPanelSprite);

            _mapProgressService.ClaimFirstCityReward();

            Debug.Log("Give first city reward");

            _collectionUnlockService.UnlockRandomCount(3);
            _moneyService.AddMoney(1000);
        }

        public void ClosePopup()
        {
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}