using Game.Scripts.GameRoot.Services.Analytics;
using Game.Scripts.GameRoot.Services.DataStorage;
using Game.Scripts.GameRoot.Services.DataStorage.Implementations;
using Game.Scripts.GameRoot.Services.DataStorage.Interfaces;
using Game.Scripts.GameRoot.Services.DataStorage.Json;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.GameRoot.Services.Scenes;
using Game.Scripts.Services.DailyQuests;
using Game.Scripts.Services.Money;
using Game.Scripts.Utils;
using Lofelt.NiceVibrations;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.GameRoot
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIRootView _uiRootViewPrefab;
        [SerializeField] private CollectionRegistryConfig _collectionRegistryConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_uiRootViewPrefab != null)
            {
                var uiRootInstance = Instantiate(_uiRootViewPrefab);
                DontDestroyOnLoad(uiRootInstance.gameObject);

                builder.RegisterComponent(uiRootInstance);
            }
            else
            {
                Debug.LogError("[ProjectLifetimeScope] UIRootView prefab is not assigned!");
            }

            builder.Register<PopupService>(Lifetime.Singleton).As<IPopupService>();

            Debug.Log("[ProjectLifetimeScope] Registering UIOverlayService...");
            builder.Register<UIOverlayService>(Lifetime.Singleton).As<IUIOverlayService>();
            Debug.Log("[ProjectLifetimeScope] UIOverlayService registered!");

            builder.RegisterBuildCallback(container =>
            {
                var overlayService = container.Resolve<IUIOverlayService>();
                Debug.Log(
                    $"[ProjectLifetimeScope] UIOverlayService resolved and initialized: {overlayService != null}");
            });

            var coroutinesGameObject = new GameObject("[COROUTINES]");
            DontDestroyOnLoad(coroutinesGameObject);
            var coroutines = coroutinesGameObject.AddComponent<Coroutines>();
            builder.RegisterComponent(coroutines);

            builder.Register<MoneyService>(Lifetime.Singleton);
            builder.Register<DailyQuestService>(Lifetime.Singleton);

            builder.Register<PlayerPrefsDataStorage>(Lifetime.Singleton).As<IDataStorage>();
            builder.Register<JsonDataService>(Lifetime.Singleton).As<IJsonDataService>();
            builder.Register<GameDataRepository>(Lifetime.Singleton);
            builder.Register<CollectionUnlockService>(resolver =>
                new CollectionUnlockService(
                    resolver.Resolve<GameDataRepository>(),
                    _collectionRegistryConfig
                ), Lifetime.Singleton);
            builder.Register<GameSettingsStorage>(Lifetime.Singleton).As<IGameSettingsStorage>();

            builder.Register<LocalAnalyticEventService>(Lifetime.Singleton).As<IAnalyticEventService>();

            builder.Register<FeedbackService>(Lifetime.Singleton).As<IFeedbackService>();

            builder.Register<SceneContextProvider>(Lifetime.Singleton);
            builder.Register<MapProgressService>(Lifetime.Singleton);
            builder.Register<LevelProgressService>(Lifetime.Singleton);

            builder.Register<SceneService>(Lifetime.Singleton).As<ISceneService>();

            builder.RegisterBuildCallback(container =>
            {
                var settings = container.Resolve<IGameSettingsStorage>();
                HapticController.hapticsEnabled = settings.IsHapticOn;
                
                container.Resolve<DailyQuestService>();

            });

            builder.RegisterEntryPoint<GameBootstrapper>();

            Application.targetFrameRate = 165;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}