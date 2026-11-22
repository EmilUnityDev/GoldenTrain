using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Score;
using Game.Scripts.Utils;
using Lofelt.NiceVibrations;
using SymbolScripts;
using UnityEngine;
using VContainer;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public class BaggageTrolleySkill : ISelectableSkill
    {
        public SkillType Type => SkillType.BaggageTrolley;

        private SymbolBase _pendingTarget;
        private SkillsService _service;
        private SlotGameManager _gameManager;
        private readonly BoardView _boardView;
        private readonly IPopupService _popupService;
        private readonly SceneContextProvider _sceneContext;
        private readonly ScoreService _scoreService;
        private readonly IFeedbackService _feedbackService;
        private readonly Coroutines _coroutines;
        private readonly UIRootView _uiRootView;
        private readonly IObjectResolver _resolver;
        private int _remainingCharges;

        public int FrozenColumn { get; private set; } = -1;
        public bool HasSelection => _pendingTarget != null;
        public int RemainingCharges => _remainingCharges;

        public BaggageTrolleySkill(BoardView boardView, IPopupService popupService, IFeedbackService feedbackService, SceneContextProvider sceneContext = null, ScoreService scoreService = null, Coroutines coroutines = null, UIRootView uiRootView = null, IObjectResolver resolver = null)
        {
            _boardView = boardView;
            _popupService = popupService;
            _feedbackService = feedbackService;
            _sceneContext = sceneContext;
            _scoreService = scoreService;
            _coroutines = coroutines;
            _uiRootView = uiRootView;
            _resolver = resolver;
        }

        public void Activate(SkillsService service, SlotGameManager gameManager, int charges)
        {
            _service = service;
            _gameManager = gameManager;
            _remainingCharges = 1;
            FrozenColumn = -1;
            _pendingTarget = null;

            EnableAllColliders(true);
            DimAll(true);

            SymbolInputHandler.OnSymbolClicked += HandleSymbolClick;
        }

        public bool CanActivate(SkillsService service) => true;

        public void Cancel()
        {
            SymbolInputHandler.OnSymbolClicked -= HandleSymbolClick;
            EnableAllColliders(false);
            DimAll(false);
            ClearHighlights();
            _pendingTarget = null;
            FrozenColumn = -1;
            _service?.RestoreCharge(SkillType.BaggageTrolley);
            _popupService.CloseCurrentPopup();
        }

        public void Select(SymbolBase target)
        {
            _pendingTarget = target;

            var sameType = GetSameTypeSymbols(target.Data.symbolID);
            foreach (var sym in GetAllSymbols())
            {
                if (sym == null) continue;
                bool isSame = !sym.IsWild && !sym.IsScatter && sameType.Contains(sym);
                sym.SetDimmed(!isSame);
                if (sym.SelectedGameObject != null)
                    sym.SelectedGameObject.SetActive(isSame);
            }

            _service.NotifySelectionChanged();
        }

        public void Confirm()
        {
            if (_pendingTarget == null) return;

            string targetSymbolID = _pendingTarget.Data.symbolID;
            var sameType = GetSameTypeSymbols(targetSymbolID);

            SymbolInputHandler.OnSymbolClicked -= HandleSymbolClick;
            EnableAllColliders(false);
            DimAll(false);
            ClearHighlights();

            _popupService.CloseCurrentPopup();

            if (_coroutines != null)
                _coroutines.StartCoroutine(ConfirmRoutine(targetSymbolID, sameType));
            else
                ExecuteConfirmImmediate(targetSymbolID, sameType);
        }

        private IEnumerator ConfirmRoutine(string targetSymbolID, List<SymbolBase> sameType)
        {
            GameObject backgroundGo = null;
            if (_gameManager._skillsBackgroundPrefab != null && _uiRootView != null)
            {
                _uiRootView.UpdateCanvasOrder(0);
                backgroundGo = Object.Instantiate(_gameManager._skillsBackgroundPrefab, _uiRootView.UISceneContainer);
            }
            
            DimAll(false);
            ClearHighlights();

            int destroyedCount = 0;
            foreach (var sym in sameType)
            {
                if (sym == null) continue;
                int r = sym.Row;
                int c = sym.Col;

                var ctrl = _boardView.GetSymbolController(r, c);
                if (ctrl != null) ctrl.DestroyAnim();

                _gameManager.Board.SetSymbol(r, c, null);
                _boardView.ClearCell(r, c);
                destroyedCount++;
            }

            if (destroyedCount > 0 && _scoreService != null)
                _scoreService.AddPointsDeferred(destroyedCount * 120);

            if (destroyedCount > 0)
                _service.AddProgress(destroyedCount);

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
            Debug.Log($"[BaggageTrolley] Destroyed {destroyedCount} symbols of type '{targetSymbolID}'.");
            
            yield return new WaitForSeconds(0.4f);
            
            DimAll(false);

            if (_uiRootView != null)
                _uiRootView.UpdateCanvasOrder(10);
            if (backgroundGo != null)
                Object.Destroy(backgroundGo);

            _remainingCharges--;
            _pendingTarget = null;
            _service.NotifySelectionChanged();

            _service.FinishActiveSkill();
        }

        private void ExecuteConfirmImmediate(string targetSymbolID, List<SymbolBase> sameType)
        {
            int destroyedCount = 0;
            foreach (var sym in sameType)
            {
                if (sym == null) continue;
                var ctrl = _boardView.GetSymbolController(sym.Row, sym.Col);
                if (ctrl != null) ctrl.DestroyAnim();
                _gameManager.Board.SetSymbol(sym.Row, sym.Col, null);
                _boardView.ClearCell(sym.Row, sym.Col);
                destroyedCount++;
            }

            if (destroyedCount > 0 && _scoreService != null)
                _scoreService.AddPointsDeferred(destroyedCount * 120);
            if (destroyedCount > 0)
                _service.AddProgress(destroyedCount);

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);

            _remainingCharges--;
            _pendingTarget = null;
            _service.NotifySelectionChanged();
            _service.FinishActiveSkill();
        }

        private void HandleSymbolClick(SymbolBase symbol)
        {
            if (symbol == null) return;
            if (symbol.IsWild || symbol.IsScatter) return;
            Select(symbol);
        }

        private List<SymbolBase> GetSameTypeSymbols(string symbolID)
        {
            var result = new List<SymbolBase>();
            if (_boardView == null || _gameManager == null) return result;
            for (int r = 0; r < _gameManager.Board.Rows; r++)
            for (int c = 0; c < _gameManager.Board.Columns; c++)
            {
                var ctrl = _boardView.GetSymbolController(r, c);
                if (ctrl == null) continue;
                if (ctrl.IsWild || ctrl.IsScatter) continue;
                var data = _gameManager.Board.GetSymbol(r, c);
                if (data != null && data.symbolID == symbolID)
                    result.Add(ctrl);
            }
            return result;
        }

        private void EnableAllColliders(bool enable)
        {
            if (_boardView == null || _gameManager == null) return;
            for (int r = 0; r < _gameManager.Board.Rows; r++)
            for (int c = 0; c < _gameManager.Board.Columns; c++)
            {
                var ctrl = _boardView.GetSymbolController(r, c);
                if (ctrl == null) continue;
                var col2d = ctrl.GetComponent<Collider2D>();
                if (col2d != null) col2d.enabled = enable;
            }
        }

        private void DimAll(bool dimmed)
        {
            foreach (var sym in GetAllSymbols())
                if (sym != null) sym.SetDimmed(dimmed);
        }

        private void ClearHighlights()
        {
            foreach (var sym in GetAllSymbols())
            {
                if (sym == null || sym.SelectedGameObject == null) continue;
                sym.SelectedGameObject.SetActive(false);
            }
        }

        private IEnumerable<SymbolBase> GetAllSymbols()
        {
            if (_boardView == null) yield break;
            foreach (var sym in _boardView._symbolMap.Values)
                yield return sym;
        }
    }
}
