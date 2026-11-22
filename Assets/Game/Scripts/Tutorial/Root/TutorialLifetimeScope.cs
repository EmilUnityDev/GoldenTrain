using DG.Tweening;
using Game.Scripts.Services.Score;
using Game.Scripts.Services.Tutorial;
using Game.Scripts.Tutorial.Root.View;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using TutorScripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Tutorial.Root
{
    public class TutorialLifetimeScope : LifetimeScope
    {
        [SerializeField] private UITutorialRootBinder _sceneUITutorialRootPrefab;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private SlotGameManager _slotGameManager;
        [SerializeField] private MainTutorialScreensController _mainTutorialScreensController;
        [SerializeField] private Transform _hintHand;

        private const string HINT_HAND_TAG = "TutorialHintHand";

        protected override void Awake()
        {
            Debug.Log($"[TutorialLifetimeScope] Awake called! GameObject active: {gameObject.activeInHierarchy}, AutoRun: {autoRun}, IsRoot: ");
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[TutorialLifetimeScope] Configure called!");
            
            builder.Register<TutorialUIService>(Lifetime.Scoped).As<ITutorialUIService>();
            
            builder.RegisterComponentInHierarchy<TutorialController>();

            if (_mainTutorialScreensController != null)
            {
                Debug.Log("[TutorialLifetimeScope] Registering MainTutorialScreensController component");
                builder.RegisterComponent(_mainTutorialScreensController);
            }
            else
            {
                Debug.LogError("[TutorialLifetimeScope] MainTutorialScreensController is not assigned!");
            }

            if (_hintHand != null)
            {
                builder.RegisterBuildCallback(container =>
                {
                    var tutorialUIService = container.Resolve<ITutorialUIService>();
                    tutorialUIService.SetHintHand(_hintHand);
                    _hintHand.gameObject.SetActive(false);
                });
            }

            builder.Register<ScoreService>(Lifetime.Singleton);

            builder.Register<SlotLimits>(Lifetime.Singleton).WithParameter(3);

            if (_slotGameManager != null)
            {
                builder.RegisterComponent(_slotGameManager);
                builder.Register(container =>
                {
                    var slotManager = container.Resolve<SlotGameManager>();
                    return slotManager.SkillsService;
                }, Lifetime.Scoped);
            }
            
            builder.RegisterInstance(_sceneUITutorialRootPrefab);
            builder.RegisterInstance(_uiCamera);

            builder.RegisterEntryPoint<TutorialSceneEntryPoint>();
        }

        protected override void OnDestroy()
        {
            DOTween.KillAll();
            DOTween.Clear(true);
            base.OnDestroy();
        }
    }
}
