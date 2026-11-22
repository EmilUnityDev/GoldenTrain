using Game.Scripts.Game.Gameplay.Root.View;
using Game.Scripts.Gameplay.LocalMatch;
using Game.Scripts.Services.Score;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Game.Gameplay.Root
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIGameplayRootBinder _sceneUIRootPrefab;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private LocalMatchManager _localMatchManager;
        [SerializeField] private SlotGameManager _slotGameManager;

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[GameplayLifetimeScope] Configure called!");
            
            builder.RegisterComponentInHierarchy<GameplayController>();

            builder.Register<ScoreService>(Lifetime.Singleton);

            builder.Register<SlotLimits>(Lifetime.Singleton).WithParameter(15);

            if (_localMatchManager != null)
            {
                builder.RegisterComponent(_localMatchManager);
            }

            if (_slotGameManager != null)
            {
                builder.RegisterComponent(_slotGameManager);
                builder.Register(container =>
                {
                    var slotManager = container.Resolve<SlotGameManager>();
                    return slotManager.SkillsService;
                }, Lifetime.Scoped);
            }
            
            builder.RegisterInstance(_sceneUIRootPrefab);
            builder.RegisterInstance(_uiCamera);
            
            builder.RegisterEntryPoint<GameplaySceneEntryPoint>();
        }
    }
}
