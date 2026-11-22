using System;
using Game.Scripts.Game.MainMenu.Root.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.MainMenu.Root
{
    public class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIMainMenuRootBinder _sceneUIRootPrefab;
        [SerializeField] private Camera _uiCamera;
        
        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[MainMenuLifetimeScope] Configure called!");
            
            builder.RegisterComponentInHierarchy<MainMenuController>();
            
            var binderInstance = Instantiate(_sceneUIRootPrefab);
            builder.RegisterInstance(binderInstance);
            builder.RegisterInstance(_uiCamera);
            
            builder.RegisterEntryPoint<MainMenuSceneEntryPoint>();
        }
    }
}