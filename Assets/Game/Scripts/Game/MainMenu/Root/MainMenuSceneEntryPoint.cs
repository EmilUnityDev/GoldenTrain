using Game.Scripts.Game.MainMenu.Root.View;
using Game.Scripts.GameRoot.Services.Popups;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.MainMenu.Root
{
    public class MainMenuSceneEntryPoint : IStartable
    {
        private readonly UIMainMenuRootBinder _uiPrefab;
        private readonly Camera _camera;
        private readonly UIRootView _uiRootView;
        private readonly IObjectResolver _container;
        private readonly IPopupService _popupService;

        [Inject]
        public MainMenuSceneEntryPoint(
            UIMainMenuRootBinder uiPrefab,
            Camera camera,
            UIRootView uiRootView,
            IObjectResolver container,
            IPopupService popupService)
        {
            Debug.Log("[MainMenuSceneEntryPoint] Constructor called!");
            Debug.Log($"[MainMenuSceneEntryPoint] uiPrefab is null: {uiPrefab == null}");
            Debug.Log($"[MainMenuSceneEntryPoint] camera is null: {camera == null}");
            Debug.Log($"[MainMenuSceneEntryPoint] uiNavigation is null: {uiRootView == null}");
            Debug.Log($"[MainMenuSceneEntryPoint] container is null: {container == null}");
            
            _uiPrefab = uiPrefab;
            _camera = camera;
            _uiRootView = uiRootView;
            _container = container;
            _popupService = popupService;
        }

        public void Start()
        {
            Debug.Log("[MainMenuSceneEntryPoint] Start called, initializing UI...");
            
            _popupService.RegisterSceneResolver(_container);
            
            if (_uiPrefab == null)
            {
                Debug.LogError("[MainMenuSceneEntryPoint] UI Prefab is NULL!");
                return;
            }

            _container.InjectGameObject(_uiPrefab.gameObject);
            _uiRootView.AttachSceneUI(_uiPrefab.gameObject, _camera);
            
            Debug.Log("[MainMenuSceneEntryPoint] UI initialized successfully!");
        }
    }
}
