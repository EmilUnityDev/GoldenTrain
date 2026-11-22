using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Tutorial;
using Game.Scripts.Tutorial.Root.View;
using SlotsLogic.Match3Slots.Scripts.Core;
using TutorScripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Tutorial.Root
{
    public class TutorialSceneEntryPoint : IStartable
    {
        private readonly UITutorialRootBinder _uiPrefab;
        private readonly Camera _camera;
        private readonly SlotGameManager _slotGameManager;
        private readonly UIRootView _uiRootView;
        private readonly TutorialController _tutorialController;
        private readonly IObjectResolver _container;
        private readonly IPopupService _popupService;
        private readonly MainTutorialScreensController _mainTutorialScreensController;
        private readonly ITutorialUIService _tutorialUIService;

        public TutorialSceneEntryPoint(
            UITutorialRootBinder uiPrefab,
            Camera camera,
            SlotGameManager slotGameManager,
            UIRootView uiRootView,
            TutorialController tutorialController,
            IObjectResolver container,
            IPopupService popupService,
            MainTutorialScreensController mainTutorialScreensController,
            ITutorialUIService tutorialUIService)
        {
            _uiPrefab = uiPrefab;
            _camera = camera;
            _slotGameManager = slotGameManager;
            _uiRootView = uiRootView;
            _tutorialController = tutorialController;
            _container = container;
            _popupService = popupService;
            _mainTutorialScreensController = mainTutorialScreensController;
            _tutorialUIService = tutorialUIService;
        }

        public void Start()
        {
            Debug.Log("[TutorialSceneEntryPoint] Start called, initializing UI and tutorial...");

            _popupService.RegisterSceneResolver(_container);

            if (_slotGameManager != null)
            {
                _slotGameManager.Initialize(_container);
                _slotGameManager.SkillsService?.SetChargeAccumulationBlocked(true);
                _slotGameManager.SkillsService?.SetTutorialUIService(_tutorialUIService);
                Debug.Log("[TutorialSceneEntryPoint] SlotGameManager initialized, skill charge accumulation blocked!");
            }

            if (_uiPrefab == null)
            {
                Debug.LogError("[TutorialSceneEntryPoint] UI Prefab is NULL!");
                return;
            }

            var uiScene = Object.Instantiate(_uiPrefab);
            _container.InjectGameObject(uiScene.gameObject);

            if (_slotGameManager != null)
            {
                uiScene.Initialize(_slotGameManager.SkillsService, _slotGameManager.SpinSystem);
                Debug.Log("[TutorialSceneEntryPoint] UI initialized with SkillsService and SpinSystem!");
            }

            _mainTutorialScreensController?.SetUIBinder(uiScene);

            _uiRootView.AttachSceneUI(uiScene.gameObject, _camera);

            _tutorialController.StartGame();

            Debug.Log("[TutorialSceneEntryPoint] Initialization complete!");
        }
    }
}