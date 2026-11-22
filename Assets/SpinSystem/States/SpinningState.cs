using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Utils;
using Lofelt.NiceVibrations;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using SpinSystem.Interfaces;
using SymbolScripts;
using UnityEngine;

namespace SpinSystem.States
{
    public class SpinningState : ISpinState
    {
        public event Action OnCompleted;
        private readonly SlotGameManager _gameManager;
        private readonly BoardView _boardView;
        private readonly ISymbolGenerator _generator;
        private readonly SlotGameConfig _config;
        private readonly Coroutines _coroutines;
        private readonly IFeedbackService _audioService;
        private readonly IPopupService _popupService;
        private readonly SceneContextProvider _sceneContext;
        private SkillsService _skillsService;
        private SpinSystem _spinSystem;

        private Coroutine _spinCoroutine;
        private bool _skipAnimation;

        public int TutorialSpinCount { get; private set; }

        public void SetSkillsService(SkillsService skillsService)
        {
            _skillsService = skillsService;
        }

        public void SetSpinSystem(SpinSystem spinSystem)
        {
            _spinSystem = spinSystem;
        }

        public SpinningState(
            SlotGameManager gm,
            BoardView view,
            ISymbolGenerator gen,
            SlotGameConfig cfg,
            Coroutines coroutines,
            IFeedbackService audioService,
            IPopupService popupService,
            SceneContextProvider sceneContext)
        {
            _gameManager = gm;
            _boardView = view;
            _generator = gen;
            _config = cfg;
            _coroutines = coroutines;
            _audioService = audioService;
            _popupService = popupService;
            _sceneContext = sceneContext;
        }

        public void Enter()
        {
            _skipAnimation = false;
            _audioService?.PlaySfx("roll");
            HapticPatterns.PlayConstant(0.1f, 1f, 2f);

            var isTutorial = _sceneContext.IsTutorialScene();
            var isAfterSkill = _spinSystem != null && _spinSystem.IsSpinAfterSkill;
            if (isTutorial && !isAfterSkill)
            {
                TutorialSpinCount++;
                Debug.Log($"[SpinningState] Tutorial spin count: {TutorialSpinCount}");
            }

            if (_spinCoroutine != null)
            {
                _coroutines.StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }

            _spinCoroutine = _coroutines.StartCoroutine(SpinRoutine());
        }

        public void SkipAnimation()
        {
            if (!_skipAnimation && _spinCoroutine != null)
            {
                _skipAnimation = true;
                _boardView.InterruptFlyOut();
            }
        }

        private IEnumerator SpinRoutine()
        {
            var frozenColumn = -1;
            var frozenCellRow = -1;
            var frozenCellCol = -1;
            
            SymbolData[] frozenColumnData = null;
            SymbolBase[] frozenColumnCtrls = null;
            SymbolData frozenCellData = null;
            SymbolBase frozenCellCtrl = null;
            HashSet<GameObject> preserveObjects = null;

            if (frozenColumn >= 0)
            {
                var rows0 = _gameManager.Board.Rows;
                frozenColumnData = new SymbolData[rows0];
                frozenColumnCtrls = new SymbolBase[rows0];
                preserveObjects = new HashSet<GameObject>();
                for (var r = 0; r < rows0; r++)
                {
                    frozenColumnData[r] = _gameManager.Board.GetSymbol(r, frozenColumn);
                    frozenColumnCtrls[r] = _boardView.GetSymbolController(r, frozenColumn);
                    if (frozenColumnCtrls[r] != null)
                        preserveObjects.Add(frozenColumnCtrls[r].gameObject);
                }
            }

            if (frozenCellRow >= 0 && frozenCellCol >= 0)
            {
                frozenCellData = _gameManager.Board.GetSymbol(frozenCellRow, frozenCellCol);
                frozenCellCtrl = _boardView.GetSymbolController(frozenCellRow, frozenCellCol);
                if (frozenCellCtrl != null)
                {
                    if (preserveObjects == null) preserveObjects = new HashSet<GameObject>();
                    preserveObjects.Add(frozenCellCtrl.gameObject);
                }
            }

            if (!_skipAnimation)
                yield return _boardView.FlyOutAndClearBoard(0.05f, preserveObjects);
            else
                _boardView.ClearBoard(preserveObjects);

            yield return null;
            _gameManager.Board.InitializeBoard();

            if (preserveObjects != null)
                _boardView.SetupExceptObjects(_gameManager.Board, preserveObjects);
            else
                _boardView.Setup(_gameManager.Board);

            if (frozenColumn >= 0 && frozenColumnData != null)
            {
                var rows0 = _gameManager.Board.Rows;
                for (var r = 0; r < rows0; r++)
                    if (frozenColumnData[r] != null && frozenColumnCtrls[r] != null)
                    {
                        _gameManager.Board.SetSymbol(r, frozenColumn, frozenColumnData[r]);
                        frozenColumnCtrls[r].transform.position = _boardView.GetWorldPosition(r, frozenColumn);
                        _boardView.SetControllerAt(frozenColumnCtrls[r], r, frozenColumn);
                    }
            }

            if (frozenCellRow >= 0 && frozenCellData != null && frozenCellCtrl != null)
            {
                _gameManager.Board.SetSymbol(frozenCellRow, frozenCellCol, frozenCellData);
                frozenCellCtrl.transform.position = _boardView.GetWorldPosition(frozenCellRow, frozenCellCol);
                _boardView.SetControllerAt(frozenCellCtrl, frozenCellRow, frozenCellCol);
            }

            var cols = _gameManager.Board.Columns;
            var rows = _gameManager.Board.Rows;

            var speedMultiplier = _skipAnimation ? 0.4f : 1f;
            var colStagger = Mathf.Clamp(_config.SpinColumnStaggerDelay * 0.8f * speedMultiplier, 0.01f, 0.35f);
            var interRowDelayBase = Mathf.Clamp(_config.SpinStepDelay * 1.1f * speedMultiplier, 0.01f, 0.20f);
            var minDurBase = Mathf.Clamp(_config.DropDuration * 0.9f * speedMultiplier, 0.02f, 0.30f);
            var maxDurBase = Mathf.Clamp(_config.DropDuration * 1.9f * speedMultiplier, 0.04f, 0.7f);

            var isTutorial = _sceneContext.IsTutorialScene();
            Dictionary<int, SymbolData[]> tutorialColumns = null;

            var midnightPlan = _spinSystem?.MidnightRoutePlan;
            if (midnightPlan != null)
            {
                tutorialColumns = _boardView.BuildMidnightRouteSpin(
                    _gameManager._symbols, cols, rows,
                    midnightPlan.Chain, midnightPlan.ClusterSymbol);
                _spinSystem.MidnightRoutePlan = null;
            }
            else if (isTutorial && TutorialSpinCount == 1)
                tutorialColumns = _boardView.BuildSpecificFirstTutorialLayout(_gameManager._symbols);
            else if (isTutorial && TutorialSpinCount == 2)
                tutorialColumns = _boardView.BuildTutorialScatterSpin(_gameManager._symbols, cols, rows);

            var globalSpawned = 0;
            var globalCompleted = 0;
            Action onSpawned = () => Interlocked.Increment(ref globalSpawned);
            Action onCompleted = () => Interlocked.Increment(ref globalCompleted);

            for (var c = 0; c < cols; c++)
            {
                var col = c;
                if (col == frozenColumn) continue;

                var skipRow = col == frozenCellCol ? frozenCellRow : -1;
                var columnSymbols = tutorialColumns != null ? tutorialColumns[col] : null;
                _coroutines.StartCoroutine(RunColumnFall(col, rows, minDurBase, maxDurBase, interRowDelayBase,
                    onSpawned, onCompleted, columnSymbols, skipRow));
                yield return new WaitForSeconds(colStagger);
            }

            yield return new WaitUntil(() => globalSpawned > 0 && globalCompleted >= globalSpawned);
            yield return new WaitForSeconds(0.01f);

            for (var c = 0; c < cols; c++)
            for (var r = 0; r < rows; r++)
            {
                var ctrl = _boardView.GetSymbolController(r, c);
                var sym = _gameManager.Board.GetSymbol(r, c);
                if (ctrl == null && sym != null)
                {
                    var spawn = _boardView.SpawnSymbol(r, c, sym);
                    if (spawn != null)
                    {
                        spawn.transform.position = _boardView.GetWorldPosition(r, c);
                        _boardView.SetControllerAt(spawn, r, c);
                    }
                }
                else if (ctrl != null && sym == null)
                {
                    _gameManager.Board.SetSymbol(r, c, ctrl.Data);
                }
            }

            OnCompleted?.Invoke();
            _spinCoroutine = null;
        }

        private IEnumerator RunColumnFall(int col, int rows, float minDurBase, float maxDurBase,
            float interRowDelayBase, Action onSpawned, Action onCompleted, SymbolData[] overrideColumn,
            int skipRow = -1)
        {
            SymbolData[] symbols;
            if (overrideColumn != null && overrideColumn.Length == rows)
                symbols = overrideColumn;
            else
                symbols = _generator.GenerateSymbolsForColumn(col).Take(rows).ToArray();

            var spawnedInColumn = 0;
            var completedInColumn = 0;

            for (var r = rows - 1; r >= 0; r--)
            {
                if (r == skipRow)
                {
                    yield return new WaitForSeconds(interRowDelayBase);
                    continue;
                }

                var sym = r < symbols.Length ? symbols[r] : null;
                if (sym == null)
                {
                    yield return new WaitForSeconds(interRowDelayBase);
                    continue;
                }

                var ctrl = _boardView.SpawnSymbol(-1, col, sym);
                if (ctrl == null)
                {
                    yield return new WaitForSeconds(interRowDelayBase);
                    continue;
                }

                spawnedInColumn++;
                onSpawned?.Invoke();

                var spawnPos = _boardView.GetWorldPosition(-1, col) + Vector3.up * (_config.CellSizeY * 0.5f);
                ctrl.transform.position = spawnPos;

                var tNorm = rows > 1 ? (float)(rows - 1 - r) / (rows - 1) : 0f;
                var duration = Mathf.Lerp(minDurBase, maxDurBase, tNorm);

                _coroutines.StartCoroutine(MoveAndPlace(ctrl, _boardView.GetWorldPosition(r, col), duration, r, col,
                    () =>
                    {
                        Interlocked.Increment(ref completedInColumn);
                        onCompleted?.Invoke();
                    }));

                yield return new WaitForSeconds(interRowDelayBase);
            }

            yield return new WaitUntil(() => completedInColumn >= spawnedInColumn);

            for (var r = 0; r < rows; r++)
            {
                if (r == skipRow) continue;
                var ctrl = _boardView.GetSymbolController(r, col);
                if (ctrl != null)
                {
                    _gameManager.Board.SetSymbol(r, col, ctrl.Data);
                    _boardView.SetControllerAt(ctrl, r, col);
                }
            }
        }

        private IEnumerator MoveAndPlace(SymbolBase ctrl, Vector3 target, float duration, int row, int col,
            Action onDone)
        {
            if (ctrl == null)
            {
                onDone?.Invoke();
                yield break;
            }

            try
            {
                yield return ctrl.MoveToPosition(target, duration);
            }
            finally
            {
                if (ctrl != null)
                {
                    try
                    {
                        ctrl.transform.position = target;
                    }
                    catch
                    {
                    }

                    try
                    {
                        _boardView.SetControllerAt(ctrl, row, col);
                    }
                    catch
                    {
                    }

                    try
                    {
                        _gameManager.Board.SetSymbol(row, col, ctrl.Data);
                    }
                    catch
                    {
                    }
                }

                try
                {
                    onDone?.Invoke();
                }
                catch
                {
                }
            }
        }

        public void IncrementTutorialSpinCount()
        {
            TutorialSpinCount++;
        }

        public void IncreaseTutorialSpinCount()
        {
            TutorialSpinCount++;
        }

        public void Exit()
        {
            if (_spinCoroutine != null)
            {
                _coroutines.StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
        }
    }
}