using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.GameRoot.Services.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.GameRoot
{
    public class GameBootstrapper : IStartable
    {
        private readonly ISceneService _sceneService;
        private readonly IUIOverlayService _overlayService;
        private readonly UIRootView _uiRoot;
        private readonly IObjectResolver _objectResolver;

        [Inject]
        public GameBootstrapper(ISceneService sceneService, IUIOverlayService overlayService, UIRootView uiRoot,
            IObjectResolver objectResolver)
        {
            _sceneService = sceneService;
            _overlayService = overlayService;
            _uiRoot = uiRoot;
            _objectResolver = objectResolver;
        }

        public void Start()
        {
            CheckAndLoadInitialScene();
            _objectResolver.InjectGameObject(_uiRoot.gameObject);
        }

        private void CheckAndLoadInitialScene()
        {
#if UNITY_EDITOR
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != Scenes.BOOT)
            {
                Debug.Log($"[GameBootstrapper] Already in scene '{sceneName}', skipping auto-load");
                return;
            }
#endif


            if (PlayerPrefs.GetInt("HasSeenGameTutorial", 0) == 0)
            {
                Debug.Log("[GameBootstrapper] Loading Tutorial scene");
                PlayerPrefs.SetInt("HasSeenGameTutorial", 1);
                _sceneService.LoadScene(Scenes.TUTORIAL, OnLoadStart, null);
            }
            else
            {
                Debug.Log("[GameBootstrapper] Loading Main Menu scene");
                _sceneService.LoadScene(Scenes.MAIN_MENU, OnLoadStart, null);
            }
        }

        private void OnLoadStart()
        {
            _uiRoot.ClearSceneUI();
            _overlayService.ShowLoadingWithTransition();
        }
    }
}