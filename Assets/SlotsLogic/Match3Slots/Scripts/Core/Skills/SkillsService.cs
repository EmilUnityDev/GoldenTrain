using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Popups.SkillsConfirmationPopup;
using Game.Scripts.Services.Score;
using Game.Scripts.Services.Tutorial;
using Game.Scripts.Utils;
using UnityEngine;
using VContainer;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public class SkillsService
    {
        private readonly Dictionary<SkillType, float> _cooldowns;
        private readonly Dictionary<SkillType, ISkill> _skills;
        private readonly SymbolData _wildSymbolData;
        private readonly SpinSystem.SpinSystem _spinSystem;
        private readonly IPopupService _popupService;
        private readonly SceneContextProvider _sceneContext;
        private ITutorialUIService _tutorialUIService;

        private float _sharedProgress;

        private static readonly Dictionary<SkillType, int> SkillRequiredCharges = new()
        {
            [SkillType.СateringTrolley] = 1,
            [SkillType.BaggageTrolley] = 2,
            [SkillType.MidnightRoute] = 3
        };

        private static readonly Dictionary<SkillType, int> SkillChargeCost = new()
        {
            [SkillType.СateringTrolley] = 1,
            [SkillType.BaggageTrolley] = 2,
            [SkillType.MidnightRoute] = 3
        };

        public IReadOnlyDictionary<SkillType, float> Cooldowns => _cooldowns;

        public event Action<SkillType, float> OnCooldownChanged;
        public event Action<SkillType> OnSkillActivated;
        public event Action<SkillType> OnSkillCancelled;
        public event Action<SkillType> OnSkillFinished;
        public event Action OnSelectionChanged;

        public SkillType? LastUsedSkill { get; private set; }

        private SkillType? _activeSkill;
        private bool _isSkillActive;

        private static readonly HashSet<SkillType> SelectionRequiredSkills = new()
        {
            SkillType.СateringTrolley,
            SkillType.BaggageTrolley
        };

        
        private readonly Dictionary<(int r, int c), int> _multiplierTiles = new();

        public IReadOnlyDictionary<(int r, int c), int> MultiplierTiles => _multiplierTiles;

        public void RegisterMultiplierTile(int row, int col, int value)
        {
            _multiplierTiles[(row, col)] = value;
        }

        public void ClearMultiplierTiles()
        {
            _multiplierTiles.Clear();
        }

        public void RemoveMultiplierTile(int row, int col)
        {
            _multiplierTiles.Remove((row, col));
        }

        public SkillsService(SymbolData wildSymbolData, BoardView boardView, SpinSystem.SpinSystem spinSystem, IPopupService popupService, IFeedbackService audioService, SceneContextProvider sceneContext, IObjectResolver resolver, ScoreService scoreService = null, ITutorialUIService tutorialUIService = null, Coroutines coroutines = null, UIRootView uiRootView = null)
        {
            _wildSymbolData = wildSymbolData ?? throw new ArgumentNullException(nameof(wildSymbolData));
            _spinSystem = spinSystem ?? throw new ArgumentNullException(nameof(spinSystem));
            _popupService = popupService;
            _sceneContext = sceneContext;
            _tutorialUIService = tutorialUIService;
            
            _cooldowns = new Dictionary<SkillType, float>
            {
                [SkillType.СateringTrolley] = 0f,
                [SkillType.BaggageTrolley] = 0f,
                [SkillType.MidnightRoute] = 0f
            };

            _skills = new Dictionary<SkillType, ISkill>
            {
                [SkillType.СateringTrolley] = new СateringTrolleySkill(boardView, sceneContext, popupService, audioService, coroutines, uiRootView, resolver),
                [SkillType.BaggageTrolley] = new BaggageTrolleySkill(boardView, popupService, audioService, sceneContext, scoreService, coroutines, uiRootView, resolver),
                [SkillType.MidnightRoute] = new MidnightRouteSkill(boardView, popupService, audioService, sceneContext, uiRootView, coroutines, resolver)
            };
        }

        public void SetTutorialUIService(ITutorialUIService tutorialUIService)
        {
            _tutorialUIService = tutorialUIService;
        }

        public bool IsChargeAccumulationBlocked { get; private set; }

        public void SetChargeAccumulationBlocked(bool blocked)
        {
            IsChargeAccumulationBlocked = blocked;
        }

        private void SyncCooldownsFromShared()
        {
            foreach (var key in _cooldowns.Keys.ToList())
                _cooldowns[key] = _sharedProgress;

            foreach (var key in _cooldowns.Keys)
                OnCooldownChanged?.Invoke(key, _cooldowns[key]);
        }

        private int GetChargesFromProgress(float progress)
        {
            if (progress >= 1f) return 3;
            if (progress >= 0.681f) return 2;
            if (progress >= 0.362f) return 1;
            return 0;
        }

        private static float ChargesProgressValue(int charges)
        {
            return charges switch
            {
                0 => 0f,
                1 => 0.362f,
                2 => 0.681f,
                _ => 1f
            };
        }

        public void SetOneCharge()
        {
            const float oneCharge = 0.362f;
            _sharedProgress = oneCharge;
            SyncCooldownsFromShared();
        }

        public void AddProgress(int symbolCount)
        {
            if (IsChargeAccumulationBlocked) return;

            float amount = symbolCount / 36f;
            float newValue = Mathf.Min(1f, _sharedProgress + amount);
            if (Math.Abs(newValue - _sharedProgress) > 0.001f)
            {
                _sharedProgress = newValue;
                SyncCooldownsFromShared();
            }
        }

        public void AddProgress(SkillType type, float amount)
        {
            if (IsChargeAccumulationBlocked) return;

            float newValue = Mathf.Min(1f, _sharedProgress + amount);
            if (Math.Abs(newValue - _sharedProgress) > 0.001f)
            {
                _sharedProgress = newValue;
                SyncCooldownsFromShared();
            }
        }

        public void SetFullCharge()
        {
            _sharedProgress = 1f;
            SyncCooldownsFromShared();
        }

        public void RestoreCharge(SkillType type)
        {
            float restoreAmount = type switch
            {
                SkillType.СateringTrolley => 0.362f,
                SkillType.BaggageTrolley => 0.681f,
                SkillType.MidnightRoute => 1f,
                _ => 0.362f
            };
            _sharedProgress = Mathf.Min(1f, _sharedProgress + restoreAmount);
            SyncCooldownsFromShared();
        }

        public int GetCharges(SkillType type)
        {
            return GetChargesFromProgress(_sharedProgress);
        }

        public bool TryActivateSkill(SkillType type, SlotGameManager gameManager)
        {
            if (!_cooldowns.ContainsKey(type)) return false;

            int currentCharges = GetChargesFromProgress(_sharedProgress);
            int required = SkillRequiredCharges.TryGetValue(type, out var req) ? req : 1;
            if (currentCharges < required) return false;

            if (_isSkillActive) return false;
            if (_spinSystem.CurrentStateType != typeof(SpinSystem.States.IdleState)) return false;
            if (_spinSystem.IsInputLocked) return false;
            if (!_skills[type].CanActivate(this)) return false;
            
            int cost = SkillChargeCost.TryGetValue(type, out var c) ? c : 1;
            int newCharges = Mathf.Max(0, currentCharges - cost);
            _sharedProgress = ChargesProgressValue(newCharges);
            SyncCooldownsFromShared();

            OnSkillActivated?.Invoke(type);

            bool requiresSelection = SelectionRequiredSkills.Contains(type);
            if (requiresSelection)
            {
                _spinSystem.SetInputLocked(true);
            }

            _isSkillActive = true;
            _activeSkill = type;

            _skills[type].Activate(this, gameManager, currentCharges);

            if (requiresSelection && _isSkillActive)
            {
                var isTutorial = _sceneContext.IsTutorialScene();
                if (isTutorial)
                {
                    _popupService.CloseCurrentPopup();
                }

                _popupService.ShowPopup<SkillsConfirmationView, SkillsConfirmationViewModel>(
                    new SkillsConfirmationViewModel(type, this, _tutorialUIService)
                );
            }


            return true;
        }

        public void CancelActiveSkill()
        {
            Debug.Log($"[CATERING_BUG] CancelActiveSkill called. _isSkillActive={_isSkillActive}, _activeSkill={_activeSkill}");
            if (!_isSkillActive || !_activeSkill.HasValue) return;

            var type = _activeSkill.Value;
            if (_skills.TryGetValue(type, out var skill))
                skill.Cancel();

            _spinSystem.SetInputLocked(false);
            _isSkillActive = false;
            _activeSkill = null;
            OnSkillCancelled?.Invoke(type);
        }

        public void FinishActiveSkill()
        {
            Debug.Log($"[CATERING_BUG] FinishActiveSkill called. _isSkillActive={_isSkillActive}, _activeSkill={_activeSkill}");
            if (!_isSkillActive || !_activeSkill.HasValue)
            {
                Debug.Log("[CATERING_BUG] FinishActiveSkill early return — not active");
                return;
            }

            var type = _activeSkill.Value;
            LastUsedSkill = type;
            _spinSystem.SetInputLocked(false);
            _isSkillActive = false;
            _activeSkill = null;
            OnSkillFinished?.Invoke(type);

            Debug.Log("[CATERING_BUG] Calling StartCalculationAfterSkill...");
            _spinSystem.StartCalculationAfterSkill();
            Debug.Log("[CATERING_BUG] StartCalculationAfterSkill returned.");
        }

        
        public void FinishActiveSkillSilently()
        {
            if (!_isSkillActive || !_activeSkill.HasValue) return;
            var type = _activeSkill.Value;
            LastUsedSkill = type;
            _spinSystem.SetInputLocked(false);
            _isSkillActive = false;
            _activeSkill = null;
            OnSkillFinished?.Invoke(type);
        }

        public void ResetLastUsedSkill()
        {
            LastUsedSkill = null;
        }

        public void ConfirmActiveSkill()
        {
            Debug.Log($"[CATERING_BUG] ConfirmActiveSkill called. _activeSkill={_activeSkill}");
            if (!_activeSkill.HasValue) return;
            var skill = _skills[_activeSkill.Value];
            Debug.Log($"[CATERING_BUG] skill is ISelectableSkill={skill is ISelectableSkill}, HasSelection={(_activeSkill.HasValue && skill is ISelectableSkill s ? s.HasSelection.ToString() : "n/a")}");
            if (skill is ISelectableSkill selectable)
            {
                selectable.Confirm();
            }
        }

        public bool HasPendingSelection()
        {
            return _activeSkill.HasValue && _skills[_activeSkill.Value] is ISelectableSkill selectable && selectable.HasSelection;
        }

        public void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke();
        }

        internal SymbolData GetWildSymbolData()
        {
            return _wildSymbolData;
        }

        public СateringTrolleySkill GetRiskySpinSkill()
        {
            if (_skills.TryGetValue(SkillType.СateringTrolley, out var skill) && skill is СateringTrolleySkill riskySpin)
                return riskySpin;
            return null;
        }

        public BaggageTrolleySkill GetRoadNarrowsSkill()
        {
            if (_skills.TryGetValue(SkillType.BaggageTrolley, out var skill) && skill is BaggageTrolleySkill roadNarrows)
                return roadNarrows;
            return null;
        }

        public MidnightRouteSkill GetInertiaSkill()
        {
            if (_skills.TryGetValue(SkillType.MidnightRoute, out var skill) && skill is MidnightRouteSkill inertia)
                return inertia;
            return null;
        }

        public int GetRemainingCharges()
        {
            if (_activeSkill.HasValue && _skills[_activeSkill.Value] is ISelectableSkill selectable)
            {
                return selectable.RemainingCharges;
            }
            return 0;
        }
    }
}