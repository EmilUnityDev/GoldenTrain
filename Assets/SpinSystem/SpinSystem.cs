using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Game.Gameplay.Root;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.DailyQuests;
using Game.Scripts.Services.Money;
using Game.Scripts.Services.Score;
using Game.Scripts.Utils;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using SpinSystem.Interfaces;
using SpinSystem.States;
using TutorScripts;
using UnityEngine;

namespace SpinSystem
{
    public class SpinSystem
    {
        private readonly SlotLimits _slotLimits;
        private readonly SceneContextProvider _sceneContext;
        private readonly GameplayController _gameplayController;
        private Coroutines _coroutines;
        private DailyQuestService _dailyQuestService;
        
        public Func<IEnumerator> BeforeCalculationHook { get; set; }
        public MidnightRoutePlan MidnightRoutePlan { get; set; }

        private readonly List<Type> _order = new()
        {
            typeof(IdleState),
            typeof(SpinningState),
            typeof(StoppingState),
            typeof(CalculationState),
            typeof(WinState)
        };

        public bool IsCalculationAfterSkill { get; set; }
        public bool IsSpinAfterSkill { get; private set; }

        private ISpinState _current;
        private readonly Dictionary<Type, ISpinState> _states = new();

        public event Action<Type> OnStateChanged;
        public bool IsSpinning { get; private set; }

        public bool IsInputLocked { get; private set; }

        public Type CurrentStateType => _current?.GetType();

        public SkillsService SkillsService { get; private set; }

        public void SetSkillsService(SkillsService skillsService)
        {
            SkillsService = skillsService;
            if (_states.TryGetValue(typeof(CalculationState), out var calcStateObj) &&
                calcStateObj is CalculationState calcState) calcState.SetSkillsService(skillsService);
            if (_states.TryGetValue(typeof(SpinningState), out var spinStateObj) &&
                spinStateObj is SpinningState spinState) spinState.SetSkillsService(skillsService);
        }

        public SpinSystem(
            SlotGameManager gameManager,
            BoardView boardView,
            ISymbolGenerator symbolGenerator,
            WinCalculator winCalc,
            IMatchChecker matchChecker,
            IPaytableService paytable,
            SlotGameConfig config,
            SlotLimits slotLimits,
            SceneContextProvider sceneContext,
            ScoreService scoreService,
            IPopupService popupService,
            Coroutines coroutines,
            SkillsService skillsService,
            IUIOverlayService uiOverlayService,
            IFeedbackService audioService,
            DailyQuestService dailyQuestService = null,
            GameplayController gameplayController = null,
            MainTutorialScreensController tutorialController = null,
            CollectionUnlockService collectionUnlockService = null,
            UIRootView uiRootView = null,
            MoneyService moneyService = null)
        {
            _slotLimits = slotLimits;
            _sceneContext = sceneContext;
            _gameplayController = gameplayController;
            _dailyQuestService = dailyQuestService;

            _states[typeof(IdleState)] = new IdleState();
            var spinningState = new SpinningState(gameManager, boardView, symbolGenerator, config, coroutines,
                audioService, popupService, sceneContext);
            spinningState.SetSpinSystem(this);
            _states[typeof(SpinningState)] = spinningState;
            _states[typeof(StoppingState)] = new StoppingState(config.StoppingDuration, coroutines);
            _states[typeof(CalculationState)] = new CalculationState(
                this, gameManager, boardView, winCalc, matchChecker, paytable,
                symbolGenerator, config, spinningState, scoreService, popupService,
                coroutines, sceneContext, null, gameplayController, uiOverlayService,
                audioService, dailyQuestService, tutorialController, 200,
                collectionUnlockService, uiRootView, moneyService);

            _coroutines = coroutines;
            var calcState = (CalculationState)_states[typeof(CalculationState)];
            _states[typeof(SpinningState)].OnCompleted += () =>
            {
                IsSpinAfterSkill = false;
                ChangeState<StoppingState>();
            };
            _states[typeof(StoppingState)].OnCompleted += () =>
            {
                if (BeforeCalculationHook != null)
                {
                    var hook = BeforeCalculationHook;
                    BeforeCalculationHook = null;
                    _coroutines.StartCoroutine(RunHookThenCalculate(hook));
                }
                else
                {
                    ChangeState<CalculationState>();
                }
            };
            calcState.OnCompleted += () =>
            {
                if (calcState.TotalWin > 0)
                {
                    var winState = new WinState(calcState.TotalWin, coroutines);
                    winState.OnCompleted += () => ChangeState<IdleState>();
                    _states[typeof(WinState)] = winState;
                    ChangeState<WinState>();
                }
                else
                {
                    if (_slotLimits.EndGame())
                    {
                        ChangeState<IdleState>();
                    }
                    else
                    {
                        if (!_sceneContext.IsTutorialScene() && _gameplayController != null)
                            _gameplayController.EndGame();
                        else
                            ChangeState<IdleState>();
                    }
                }
            };

            ChangeState<IdleState>();
        }

        public void Spin()
        {
            if (IsSpinning) return;
            if (IsInputLocked) return;
            if (!_slotLimits.EndGame())
                return;
            _slotLimits.RemoveSpin();
            IsSpinning = true;
            IsSpinAfterSkill = false;
            if (!_sceneContext.IsTutorialScene())
                _dailyQuestService?.AddProgress(DailyQuestType.MakeSpins);
            ChangeState<SpinningState>();
        }

        public void SetInputLocked(bool locked)
        {
            IsInputLocked = locked;
        }

        public void StartCalculation()
        {
            if (_current?.GetType() == typeof(CalculationState)) return;
            IsSpinning = true;
            IsCalculationAfterSkill = false;
            ChangeState(typeof(CalculationState));
        }

        public void StartCalculationAfterSkill()
        {
            Debug.Log($"[CATERING_BUG] StartCalculationAfterSkill called. CurrentState={_current?.GetType().Name}");
            if (_current?.GetType() == typeof(CalculationState))
            {
                Debug.Log("[CATERING_BUG] Already in CalculationState, returning.");
                return;
            }
            IsSpinning = true;
            IsCalculationAfterSkill = true;
            Debug.Log("[CATERING_BUG] Calling ChangeState<CalculationState>...");
            ChangeState<CalculationState>();
            Debug.Log("[CATERING_BUG] ChangeState<CalculationState> returned.");
        }

        public void StartSpinningAfterSkill()
        {
            if (_current?.GetType() == typeof(SpinningState)) return;
            IsSpinning = true;
            IsCalculationAfterSkill = false;
            IsSpinAfterSkill = true;
            if (_states.TryGetValue(typeof(SpinningState), out var spinStateObj) &&
                spinStateObj is SpinningState spinState && _sceneContext.IsTutorialScene())
            {
                spinState.IncrementTutorialSpinCount();
                Debug.Log(
                    $"[SpinSystem] StartSpinningAfterSkill — TutorialSpinCount incremented to {spinState.TutorialSpinCount}");
            }

            ChangeState<SpinningState>();
        }

        public void SkipSpinningAnimation()
        {
            if (_current is SpinningState spinningState) spinningState.SkipAnimation();
        }

        public void SkipCalculationAnimation()
        {
            if (_current is CalculationState calculationState) calculationState.SkipAnimation();
        }

        private void ChangeState<T>() where T : ISpinState
        {
            ChangeState(typeof(T));
        }

        private IEnumerator RunHookThenCalculate(Func<IEnumerator> hook)
        {
            yield return hook();
            IsCalculationAfterSkill = true; 
            ChangeState<CalculationState>();
        }

        private void ChangeState(Type type)
        {
            if (_current?.GetType() == type) return;

            _current?.Exit();
            _current = _states[type];
            if (type == typeof(WinState))
                if (!_slotLimits.EndGame())
                    if (!_sceneContext.IsTutorialScene() && _gameplayController != null)
                        _gameplayController.EndGame();

            if (type == typeof(IdleState)) IsSpinning = false;

            OnStateChanged?.Invoke(type);

            _current.Enter();
        }
    }
}