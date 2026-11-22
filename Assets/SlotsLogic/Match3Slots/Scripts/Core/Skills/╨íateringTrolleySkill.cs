using System.Collections;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Skills;
using Game.Scripts.Utils;
using Lofelt.NiceVibrations;
using SymbolScripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public class СateringTrolleySkill : ISelectableSkill
    {
        public SkillType Type => SkillType.СateringTrolley;

        private SkillsService _service;
        private SlotGameManager _gameManager;
        private readonly BoardView _boardView;
        private readonly SceneContextProvider _sceneContext;
        private readonly IPopupService _popupService;
        private readonly IFeedbackService _feedbackService;
        private readonly Coroutines _coroutines;
        private readonly UIRootView _uiRootView;
        private readonly IObjectResolver _resolver;

        private SymbolBase _pendingTarget;
        private int _remainingCharges;

        public bool HasSelection => _pendingTarget != null;
        public int RemainingCharges => _remainingCharges;

        public float SpinMultiplier => 1f;

        public СateringTrolleySkill(BoardView boardView, SceneContextProvider sceneContext, IPopupService popupService, IFeedbackService feedbackService, Coroutines coroutines, UIRootView uiRootView, IObjectResolver resolver)
        {
            _boardView = boardView;
            _sceneContext = sceneContext;
            _popupService = popupService;
            _feedbackService = feedbackService;
            _coroutines = coroutines;
            _uiRootView = uiRootView;
            _resolver = resolver;
        }

        public void Activate(SkillsService service, SlotGameManager gameManager, int charges)
        {
            _service = service;
            _gameManager = gameManager;
            _remainingCharges = 1;
            _pendingTarget = null;

            var isTutorial = _sceneContext.IsTutorialScene();

            EnableAllColliders(true, isTutorial);
            DimAll(true);

            SymbolInputHandler.OnSymbolClicked += HandleSymbolClick;
        }

        public bool CanActivate(SkillsService service) => true;

        private void HandleSymbolClick(SymbolBase symbol)
        {
            if (symbol == null) return;

            var isTutorial = _sceneContext.IsTutorialScene();
            if (isTutorial && !(symbol.Row == 2 && symbol.Col == 1))
                return;

            Select(symbol);
        }

        private void EnableAllColliders(bool enable, bool tutorialRestrictMode = false)
        {
            if (_boardView == null || _gameManager == null) return;
            for (int r = 0; r < _gameManager.Board.Rows; r++)
            for (int c = 0; c < _gameManager.Board.Columns; c++)
            {
                var ctrl = _boardView.GetSymbolController(r, c);
                if (ctrl == null) continue;
                var col2d = ctrl.GetComponent<Collider2D>();
                if (col2d != null)
                {
                    if (tutorialRestrictMode)
                        col2d.enabled = (r == 2 && c == 1);
                    else
                        col2d.enabled = enable;
                }
            }
        }

        public void Cancel()
        {
            Debug.Log("[CATERING_BUG] Cancel called");
            SymbolInputHandler.OnSymbolClicked -= HandleSymbolClick;
            EnableAllColliders(false);
            DimAll(false);
            ClearCrossHighlight();
            _pendingTarget = null;
            _service?.RestoreCharge(SkillType.СateringTrolley);
            _popupService.CloseCurrentPopup();
            Debug.Log("[CATERING_BUG] Cancel finished");
        }

        public void Select(SymbolBase target)
        {
            _pendingTarget = target;

            var cells = GetCrossCells(target.Row, target.Col);
            foreach (var sym in GetAllSymbols())
            {
                if (sym == null) continue;
                bool inCross = false;
                foreach (var (r, c) in cells)
                    if (sym.Row == r && sym.Col == c) { inCross = true; break; }
                sym.SetDimmed(!inCross);
            }

            HighlightCross(target.Row, target.Col);
            _service.NotifySelectionChanged();
        }

        public void Confirm()
        {
            Debug.Log("[CATERING_BUG] Confirm called, _pendingTarget=" + _pendingTarget);
            if (_pendingTarget == null) return;

            int centerRow = _pendingTarget.Row;
            int centerCol = _pendingTarget.Col;

            SymbolInputHandler.OnSymbolClicked -= HandleSymbolClick;
            EnableAllColliders(false);
            ClearCrossHighlight();

            _coroutines.StartCoroutine(ConfirmRoutine(centerRow, centerCol));
        }

        private IEnumerator ConfirmRoutine(int centerRow, int centerCol)
        {
            _popupService.CloseCurrentPopup();
            
            DimAll(false);

            GameObject backgroundGo = null;
            if (_gameManager._skillsBackgroundPrefab != null && _uiRootView != null)
            {
                _uiRootView.UpdateCanvasOrder(0);
                backgroundGo = UnityEngine.Object.Instantiate(_gameManager._skillsBackgroundPrefab, _uiRootView.UISceneContainer);
            }

           
            var centerCtrl = _boardView.GetSymbolController(centerRow, centerCol);
            if (centerCtrl != null)
            {
                centerCtrl.DestroyAnim();
                _boardView.ClearCell(centerRow, centerCol);
                _gameManager.Board.SetSymbol(centerRow, centerCol, null);
            }

            var multiplierPrefab = _gameManager._multiplierPrefab;
            if (multiplierPrefab != null)
            {
                var worldPos = _boardView.GetWorldPosition(centerRow, centerCol);
                var go = Object.Instantiate(multiplierPrefab, worldPos, Quaternion.identity, _boardView.transform);
                var symMult = go.GetComponent<SymbolMultiplier>();
                if (symMult != null)
                {
                    symMult.Row = centerRow;
                    symMult.Col = centerCol;
                    symMult.Data = null;
                    symMult.SetAsConductor();
                    _boardView.SetControllerAt(symMult, centerRow, centerCol);
                    try { symMult.SymbolSpawn(); } catch { }
                }
            }

           
            if (_gameManager._cateringAnimationPrefab != null && _uiRootView != null)
            {
                var worldCenterPos = _boardView.GetWorldPosition(centerRow, centerCol);
                var animGo = Object.Instantiate(_gameManager._cateringAnimationPrefab, _gameManager._animationCanvas);
                _resolver?.InjectGameObject(animGo);
                var animView = animGo.GetComponent<CateringAnimationView>();
                if (animView != null)
                {
                    var worldCamera = Camera.main;
                    var canvas = _uiRootView.Canvas;
                    yield return _coroutines.StartCoroutine(animView.PlayAndWait(worldCenterPos, worldCamera, canvas));
                }
                else
                {
                    yield return new WaitForSeconds(5f);
                }
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }

           
            var cells = GetCrossCells(centerRow, centerCol);
            int destroyedCount = 0;

            foreach (var (r, c) in cells)
            {
                var ctrl = _boardView.GetSymbolController(r, c);
                if (ctrl != null)
                {
                    ctrl.DestroyAnim();
                    destroyedCount++;
                }
                _gameManager.Board.SetSymbol(r, c, null);
                _boardView.ClearCell(r, c);
            }

            if (destroyedCount > 0)
                _service.AddProgress(destroyedCount);

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);

            if (_uiRootView != null)
                _uiRootView.UpdateCanvasOrder(10);
            if (backgroundGo != null)
                Object.Destroy(backgroundGo);

            _remainingCharges--;
            _pendingTarget = null;
            _service.NotifySelectionChanged();

            Debug.Log("[CATERING_BUG] Calling FinishActiveSkill...");
            _service.FinishActiveSkill();
        }

        private System.Collections.Generic.List<(int row, int col)> GetCrossCells(int row, int col)
        {
            var result = new System.Collections.Generic.List<(int, int)>();
            int rows = _gameManager.Board.Rows;
            int cols = _gameManager.Board.Columns;

            var offsets = new[] { (0, 0), (-1, 0), (1, 0), (0, -1), (0, 1) };
            foreach (var (dr, dc) in offsets)
            {
                int r = row + dr;
                int c = col + dc;
                if (r >= 0 && r < rows && c >= 0 && c < cols)
                    result.Add((r, c));
            }
            return result;
        }

        private void HighlightCross(int row, int col)
        {
            var cells = GetCrossCells(row, col);
            foreach (var sym in GetAllSymbols())
            {
                if (sym == null || sym.SelectedGameObject == null) continue;
                bool inCross = false;
                foreach (var (r, c) in cells)
                    if (sym.Row == r && sym.Col == c) { inCross = true; break; }
                sym.SelectedGameObject.SetActive(inCross);
                if (inCross)
                {
                    var sr = sym.SelectedGameObject.GetComponent<UnityEngine.SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 0.898f, 0f, 106f / 255f);
                }
            }
        }

        private void ClearCrossHighlight()
        {
            foreach (var sym in GetAllSymbols())
            {
                if (sym == null || sym.SelectedGameObject == null) continue;
                sym.SelectedGameObject.SetActive(false);
            }
        }


        private void DimAll(bool dimmed)
        {
            foreach (var sym in GetAllSymbols())
                if (sym != null) sym.SetDimmed(dimmed);
        }

        private System.Collections.Generic.IEnumerable<SymbolBase> GetAllSymbols()
        {
            if (_boardView == null) yield break;
            foreach (var sym in _boardView._symbolMap.Values)
                yield return sym;
        }
    }
}
