using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Game.Gameplay.Root;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.DailyQuests;
using Game.Scripts.Services.Money;
using Game.Scripts.Services.Popups.CollectionsPopup;
using Game.Scripts.Services.Popups.MegaWinPopup;
using Game.Scripts.Services.Popups.MiniGamePopup;
using Game.Scripts.Services.Popups.TutorialHintsPopup;
using Game.Scripts.Services.Score;
using Game.Scripts.Utils;
using Lofelt.NiceVibrations;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using SpinSystem.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;
using TutorScripts;
using Object = UnityEngine.Object;

namespace SpinSystem.States
{
    public class CalculationState : ISpinState
    {
        public event Action OnCompleted;
        public WinCalculator WinCalculator { get; }
        public int TotalWin { get; private set; }

        private readonly SlotGameManager _gameManager;
        private readonly BoardView _boardView;
        private readonly IMatchChecker _matchChecker;
        private readonly IPaytableService _paytable;
        private readonly ISymbolGenerator _symbolGenerator;
        private readonly SlotGameConfig _config;
        private readonly SpinningState _spinState;
        private readonly int _maxCascades;
        private readonly ScoreService _scoreService;
        private readonly SpinSystem _spinSystem;
        private readonly IPopupService _popupService;
        private readonly Coroutines _coroutines;
        private readonly SceneContextProvider _sceneContext;
        private SkillsService _skillsService;
        private readonly GameplayController _gameplayController;
        private readonly IUIOverlayService _uiOverlayService;
        private readonly IFeedbackService _audioService;
        private readonly DailyQuestService _dailyQuestService;
        private readonly CollectionUnlockService _collectionUnlockService;
        private readonly UIRootView _uiRootView;
        private readonly SlotGameManager _slotGameManager;
        private readonly MoneyService _moneyService;

        private static float _lastMegaWinTime = -999f;
        private const float MegaWinCooldown = 14f;

        private Coroutine _processCoroutine;
        private bool _skipAnimation;
        private Coroutine _autoCloseCoroutine;
        private Dictionary<int, SymbolData[]> _tutorialFirstSpinRefillOverride;
        private SkillType? _pendingSkillForThisSpin;
        private int _cascadeCounter;

        private bool _skipThirdHint;
        private bool _skipFourthHint;
        private bool _skipSeventhHint;
        private Coroutine _autoCloseThirdCoroutine;
        private Coroutine _autoCloseFourthCoroutine;

        private MainTutorialScreensController _tutorialController;

        public void SetTutorialController(MainTutorialScreensController controller)
        {
            _tutorialController = controller;
        }

        private bool IsTutorialOver => _tutorialController != null && _tutorialController.IsGameOver;

        public void SetSkillsService(SkillsService skillsService) => _skillsService = skillsService;

        public CalculationState(
            SpinSystem spinSystem, SlotGameManager gameManager, BoardView boardView,
            WinCalculator winCalc, IMatchChecker matchChecker, IPaytableService paytable,
            ISymbolGenerator symbolGenerator, SlotGameConfig config, SpinningState spinState,
            ScoreService scoreService, IPopupService popupService, Coroutines coroutines,
            SceneContextProvider sceneContext, SkillsService skillsService,
            GameplayController gameplayController, IUIOverlayService uiOverlayService,
            IFeedbackService audioService, DailyQuestService dailyQuestService = null,
            MainTutorialScreensController tutorialController = null, int maxCascades = 200,
            CollectionUnlockService collectionUnlockService = null, UIRootView uiRootView = null,
            MoneyService moneyService = null)
        {
            _gameManager = gameManager;
            _boardView = boardView;
            WinCalculator = winCalc;
            _matchChecker = matchChecker;
            _paytable = paytable;
            _symbolGenerator = symbolGenerator;
            _config = config;
            _maxCascades = Mathf.Max(1, maxCascades);
            _scoreService = scoreService;
            _popupService = popupService;
            _coroutines = coroutines;
            _sceneContext = sceneContext;
            _skillsService = skillsService;
            _gameplayController = gameplayController;
            _uiOverlayService = uiOverlayService;
            _audioService = audioService;
            _spinSystem = spinSystem;
            _spinState = spinState;
            _dailyQuestService = dailyQuestService;
            _tutorialController = tutorialController;
            _collectionUnlockService = collectionUnlockService;
            _uiRootView = uiRootView;
            _slotGameManager = gameManager;
            _moneyService = moneyService;
        }

        public void Enter()
        {
            if (_tutorialController != null)
            {
                Debug.Log($"is game over : {_tutorialController.IsGameOver}");
            }
            else
            {
                Debug.Log($"is game over : null tutorial controller");
            }
            
            WinCalculator.Reset();
            TotalWin = 0;
            _skipAnimation = false;
            _cascadeCounter = 0;

            if (!_spinSystem.IsCalculationAfterSkill)
            {
                if (_skillsService != null)
                {
                    _pendingSkillForThisSpin = _skillsService.LastUsedSkill;
                    _skillsService.ResetLastUsedSkill();
                    _skillsService.ClearMultiplierTiles();
                }
                else
                {
                    _pendingSkillForThisSpin = null;
                }
            }
            else
            {
                _pendingSkillForThisSpin = null;
            }

            TutorialHintsView.OnSkipThirdHint += SkipThirdHint;
            TutorialHintsView.OnSkipFourthHint += SkipFourthHint;
            TutorialHintsView.OnSkipSeventhHint += SkipSeventhHint;

            _processCoroutine = _coroutines.StartCoroutine(ProcessNext());
        }

        public void SkipAnimation() => _skipAnimation = true;

        private IEnumerator ProcessNext()
        {
            var isAfterSkill = _spinSystem.IsCalculationAfterSkill;
            var isTutorial = _sceneContext.IsTutorialScene();

            if (isTutorial && _spinState.TutorialSpinCount == 1 && !isAfterSkill)
            {
                yield return ProcessTutorialFirstSpin();
                yield break;
            }

            if (isTutorial && isAfterSkill)
            {
                yield return ProcessTutorialAfterSkillSpin();
                yield break;
            }

            if (isTutorial && _spinState.TutorialSpinCount == 2 && !isAfterSkill)
            {
                yield return ProcessTutorialScatterSpin();
                yield break;
            }

            var initialGroups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);
            if (initialGroups.Count == 0)
            {
                if (isAfterSkill)
                {
                    yield return DropAndRefill();
                }
                else
                {
                    _scoreService.ResetCurrentSpinScore();
                    Debug.Log("No initial matches, resetting current spin score.");
                    yield return HandleEndState();
                    yield break;
                }
            }

            for (var cascadeIndex = 0; cascadeIndex < _maxCascades; cascadeIndex++)
            {
                if (!isTutorial && _gameplayController != null && _gameplayController.IsGameOver())
                {
                    Debug.Log("[CalculationState] Game over detected during cascade loop — stopping.");
                    yield break;
                }

                var groups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);
                if (groups.Count == 0) break;

                _cascadeCounter++;

                var cascadeWin = 0;

                if (!isTutorial)
                {
                    if (_gameplayController != null && !_gameplayController.IsGameOver())
                    {
                        cascadeWin = WinCalculator.CalculateTotalWin();
                        if (cascadeIndex == 0 && !isAfterSkill)
                            _scoreService.SetCurrentSpinScore(cascadeWin);
                    }
                }
                else
                {
                    cascadeWin = WinCalculator.CalculateTotalWin();
                    if (cascadeIndex == 0 && !isAfterSkill)
                        _scoreService.SetCurrentSpinScore(cascadeWin);
                }

                TotalWin += cascadeWin;
                Debug.Log($"Cascade #{cascadeIndex + 1} win: {cascadeWin} AND play sound");

                if (!isTutorial && _dailyQuestService != null)
                {
                    _dailyQuestService.AddProgress(DailyQuestType.CollectClusters, groups.Count);
                    if (cascadeIndex > 0)
                        _dailyQuestService.AddProgress(DailyQuestType.LaunchCascades);
                }

                yield return ProcessMatches(groups, cascadeIndex);
                yield return DropAndRefill();
            }

            var remainingGroups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);
            if (remainingGroups.Count > 0)
                Debug.LogWarning($"Max cascades ({_maxCascades}) reached but board still has matches ({remainingGroups.Count}). Stopping additional cascades to avoid infinite loop.");

            yield return HandleEndState();
        }

        private IEnumerator HandleEndState()
        {
            Debug.Log($"Total win: {TotalWin}");

            _scoreService.CommitDeferredPoints();

            var isTutorial = _sceneContext.IsTutorialScene();
            var isAfterSkill = _spinSystem.IsCalculationAfterSkill;
            var shouldShowMegaWin = TotalWin >= 4000 && Time.time - _lastMegaWinTime >= MegaWinCooldown && !isTutorial;
            var shouldShowWheel = WinCalculator.WheelSpinsCount > 0 && (!isTutorial || _spinState.TutorialSpinCount == 5);

            if (isTutorial)
            {
                var tutorialSpin = _spinState.TutorialSpinCount;
                var showTutorialMegaWin = (isTutorial && isAfterSkill) ||
                                          tutorialSpin == 2 ||
                                          (tutorialSpin == 3 && TotalWin >= 4000) ||
                                          (tutorialSpin == 4 && TotalWin >= 4000);
                if (showTutorialMegaWin)
                {
                    var megaWinClosed = false;
                    _popupService.ShowPopup<MegaWinPopupView, MegaWinPopupViewModel>(
                        new MegaWinPopupViewModel(TotalWin > 0 ? TotalWin : 9999),
                        () => megaWinClosed = true);
                    yield return new WaitUntil(() => megaWinClosed);
                }
            }
            else if (shouldShowMegaWin)
            {
                if (_gameplayController != null && !_gameplayController.IsGameOver())
                {
                    _lastMegaWinTime = Time.time;
                    var megaWinClosed = false;
                    _popupService.ShowPopup<MegaWinPopupView, MegaWinPopupViewModel>(
                        new MegaWinPopupViewModel(TotalWin),
                        () => megaWinClosed = true);
                    yield return new WaitUntil(() => megaWinClosed);
                }
            }

            if (shouldShowWheel)
            {
                if (!isTutorial)
                {
                    if (_gameplayController != null && !_gameplayController.IsGameOver())
                    {
                        var closed = false;
                        _uiOverlayService.ShowTransition();
                        yield return new WaitForSeconds(1.3f);
                        _dailyQuestService?.AddProgress(DailyQuestType.LaunchBonusGame);
                        _popupService.ShowPopup<MiniGamePopupView, MiniGamePopupViewModel>(
                            new MiniGamePopupViewModel(),
                            () => closed = true);
                        yield return new WaitUntil(() => closed);
                    }
                }
                else
                {
                    _uiOverlayService.ShowTransition();
                    yield return new WaitForSeconds(1.3f);
                    _popupService.ShowPopup<MiniGamePopupView>();
                }
            }

            if (!isTutorial && _collectionUnlockService != null && _uiRootView != null && _slotGameManager?.CollectionPopupPrefab != null)
            {
                if (Random.Range(0, 10) == 0)
                {
                    int count = Random.Range(1, 5);
                    _collectionUnlockService.UnlockRandomCount(count);

                    var popup = Object.Instantiate(_slotGameManager.CollectionPopupPrefab);
                    popup.transform.SetParent(_uiRootView.UISceneContainer, false);

                    var cardView = popup.GetComponent<CollectionCardPopupView>();
                    if (cardView != null)
                        cardView.SetCount(count);
                }
            }

            if (!isTutorial && Random.Range(0, 10) == 0)
            {
                int coinsReward = Random.Range(10, 41);
                _moneyService?.AddMoney(coinsReward);
                _dailyQuestService?.AddProgress(DailyQuestType.WinCoins, coinsReward);
                Debug.Log($"[CalculationState] Random coins reward: {coinsReward}");
            }

            WinCalculator.Reset();
            OnCompleted?.Invoke();
        }

        private IEnumerator ProcessMatches(List<List<(int row, int col)>> groups, int cascadeIndex)
        {
            foreach (var group in groups)
            {
                var symId = group.Select(p => _gameManager.Board.GetSymbol(p.row, p.col))
                    .FirstOrDefault(s => s != null && !s.isWild && !s.isScatter)?.symbolID ?? "scatter";
                var names = string.Join(", ", group.Select(p => _gameManager.Board.GetSymbol(p.row, p.col)?.symbolID ?? "empty"));
                Debug.Log($"Match group for {symId}: {names}");
                _audioService?.PlaySfx("merge" + Random.Range(1, 4));
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
                var shouldSkip = _skipAnimation;
                if (_skipAnimation) _skipAnimation = false;

                foreach (var (r, c) in group)
                {
                    var ctrl = _boardView.GetSymbolController(r, c);
                    if (ctrl != null) ctrl.DestroyAnim();
                }

                var multipliersBetween = new List<(int r, int c)>();
                if (_skillsService != null && _skillsService.MultiplierTiles.Count > 0)
                {
                    var groupSet = new HashSet<(int, int)>(group);
                    int[] dr = { -1, 1, 0, 0 };
                    int[] dc = { 0, 0, -1, 1 };

                    Debug.Log($"[MIDNIGHT_ROUTE] Checking {_skillsService.MultiplierTiles.Count} multiplier(s) against group '{symId}' ({group.Count} cells): [{string.Join(", ", group.Select(p => $"({p.row},{p.col})"))}]");

                    foreach (var kv in _skillsService.MultiplierTiles)
                    {
                        var (mr, mc) = kv.Key;
                        bool adjacent = false;
                        for (int d = 0; d < 4; d++)
                        {
                            if (groupSet.Contains((mr + dr[d], mc + dc[d])))
                            {
                                adjacent = true;
                                break;
                            }
                        }
                        Debug.Log($"[MIDNIGHT_ROUTE] Multiplier x{kv.Value} at ({mr},{mc}) — adjacent to group '{symId}': {adjacent}");
                        if (adjacent) multipliersBetween.Add((mr, mc));
                    }
                }

                int totalMultiplier = 0;
                foreach (var tile in multipliersBetween)
                {
                    int tileValue = _skillsService.MultiplierTiles[tile];
                    totalMultiplier += tileValue;
                    var mCtrl = _boardView.GetSymbolController(tile.r, tile.c);
                    if (mCtrl != null) mCtrl.DestroyAnim();
                    _skillsService.RemoveMultiplierTile(tile.r, tile.c);
                    _gameManager.Board.SetSymbol(tile.r, tile.c, null);
                    _boardView.ClearCell(tile.r, tile.c);
                }

                if (totalMultiplier > 0)
                {
                    var paytableWin = WinCalculator.CalculateTotalWin();
                    float bonusPercent = Mathf.Clamp(totalMultiplier * 0.2f, 0f, 1f);
                    int bonus = Mathf.RoundToInt(paytableWin * bonusPercent);
                    if (bonus > 0)
                    {
                        _scoreService.AddPointsDeferred(bonus);
                        Debug.Log($"[MIDNIGHT_ROUTE] Total multiplier x{totalMultiplier} ({bonusPercent * 100:0}% bonus) applied. Base win={paytableWin}, bonus={bonus}.");
                    }
                }

                var symbolIndex = _gameManager._symbols.FindIndex(s => s.symbolID == symId);
                if (symbolIndex != -1 && _skillsService != null)
                    _skillsService.AddProgress(group.Count);

                if (!_sceneContext.IsTutorialScene() && _dailyQuestService != null)
                {
                    var symIdLower = symId.ToLower();
                    if (symIdLower.Contains("lowpay"))
                        _dailyQuestService.AddProgress(DailyQuestType.CollectLowPay, group.Count);
                    else if (symIdLower.Contains("midpay"))
                        _dailyQuestService.AddProgress(DailyQuestType.CollectMidPay, group.Count);
                }

                if (shouldSkip)
                {
                    yield return new WaitForSeconds(_config.SymbolDestructionFadeDuration * 0.3f);
                    yield return new WaitForSeconds(0.12f * 0.3f);
                }
                else
                {
                    yield return new WaitForSeconds(_config.SymbolDestructionFadeDuration);
                    yield return new WaitForSeconds(0.12f);
                }
            }

            foreach (var group in groups)
            foreach (var (r, c) in group)
            {
                _gameManager.Board.SetSymbol(r, c, null);
                _boardView.ClearCell(r, c);
            }
        }

        private IEnumerator DropAndRefill()
        {
            var isTutorial = _sceneContext.IsTutorialScene();
            var useSpecialTutorialRefill = isTutorial && _spinState.TutorialSpinCount == 1;

            var remaining = _gameManager.Board.Columns;
            for (var c = 0; c < _gameManager.Board.Columns; c++)
            {
                var col = c;
                if (useSpecialTutorialRefill)
                    _coroutines.StartCoroutine(DropColumnTutorial(col, () => { remaining--; }));
                else if (!isTutorial && _cascadeCounter >= 3)
                    _coroutines.StartCoroutine(DropColumnAntiMatch(col, () => { remaining--; }));
                else
                    _coroutines.StartCoroutine(DropColumn(col, () => { remaining--; }));
            }

            while (remaining > 0) yield return null;
            yield return new WaitForSeconds(_config.DropExtraWait);
        }

        private IEnumerator DropAndRefillWithOverride()
        {
            var remaining = _gameManager.Board.Columns;
            for (var c = 0; c < _gameManager.Board.Columns; c++)
            {
                var col = c;
                _tutorialFirstSpinRefillOverride.TryGetValue(col, out var overrideSymbols);
                _coroutines.StartCoroutine(DropColumnWithNewSymbols(col, overrideSymbols, () => remaining--));
            }

            while (remaining > 0) yield return null;
            yield return new WaitForSeconds(_config.DropExtraWait);
        }

        private IEnumerator DropAndRefillWithMatchGuarantee(Dictionary<int, SymbolData[]> overrideByColumn)
        {
            var remaining = _gameManager.Board.Columns;
            for (var c = 0; c < _gameManager.Board.Columns; c++)
            {
                var col = c;
                overrideByColumn.TryGetValue(col, out var overrideSymbols);
                _coroutines.StartCoroutine(DropColumnWithNewSymbols(col, overrideSymbols, () => remaining--));
            }

            while (remaining > 0) yield return null;
            yield return new WaitForSeconds(_config.DropExtraWait);
        }

        private Dictionary<int, SymbolData[]> BuildMatchGuaranteeRefill()
        {
            var rows = _gameManager.Board.Rows;
            var cols = _gameManager.Board.Columns;
            var normalSymbols = _gameManager._symbols.Where(s => !s.isScatter && !s.isWild).ToList();
            if (normalSymbols.Count == 0) return new Dictionary<int, SymbolData[]>();

            var threshold = _config.SymbolCountThreshold;

            var symbolCounts = normalSymbols.ToDictionary(s => s, s => 0);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var sym = _gameManager.Board.GetSymbol(r, c);
                    if (sym != null && symbolCounts.ContainsKey(sym))
                        symbolCounts[sym]++;
                }

            var emptyCellsByColumn = new Dictionary<int, int>();
            for (int c = 0; c < cols; c++)
            {
                int emptyCount = 0;
                for (int r = 0; r < rows; r++)
                    if (_gameManager.Board.GetSymbol(r, c) == null && _boardView.GetSymbolController(r, c) == null)
                        emptyCount++;
                if (emptyCount > 0)
                    emptyCellsByColumn[c] = emptyCount;
            }

            int totalEmpty = emptyCellsByColumn.Values.Sum();
            if (totalEmpty == 0) return new Dictionary<int, SymbolData[]>();

            var symbolsToFill = new List<SymbolData>();

           
            foreach (var sym in normalSymbols.OrderByDescending(s => symbolCounts[s]))
            {
                if (symbolsToFill.Count >= totalEmpty) break;
                int current = symbolCounts[sym];
                int canAdd = threshold - current;
                if (canAdd <= 0) continue; 
                for (int i = 0; i < canAdd && symbolsToFill.Count < totalEmpty; i++)
                    symbolsToFill.Add(sym);
            }

           
            while (symbolsToFill.Count < totalEmpty)
            {
                var sym = normalSymbols
                    .Where(s => symbolCounts[s] + symbolsToFill.Count(x => x == s) < threshold)
                    .OrderBy(s => symbolCounts[s])
                    .FirstOrDefault();
                if (sym == null) break;
                symbolsToFill.Add(sym);
            }

            symbolsToFill = symbolsToFill.OrderBy(_ => Random.value).ToList();

            var result = new Dictionary<int, SymbolData[]>();
            int fillIdx = 0;
            foreach (var kvp in emptyCellsByColumn)
            {
                var colSymbols = new SymbolData[kvp.Value];
                for (int i = 0; i < kvp.Value && fillIdx < symbolsToFill.Count; i++, fillIdx++)
                    colSymbols[i] = symbolsToFill[fillIdx];
                result[kvp.Key] = colSymbols;
            }

            return result;
        }

        private IEnumerator DropColumn(int c, Action onComplete)
        {
            if (_boardView == null || _gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var writeRow = _gameManager.Board.Rows - 1;
            for (var readRow = _gameManager.Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = _gameManager.Board.GetSymbol(readRow, c);
                var ctrl = _boardView.GetSymbolController(readRow, c);
                bool isOccupied = sym != null || ctrl != null;

                if (isOccupied)
                {
                    if (readRow != writeRow)
                    {
                        _gameManager.Board.SetSymbol(writeRow, c, sym);
                        _gameManager.Board.SetSymbol(readRow, c, null);

                        if (ctrl != null && _boardView != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
                            _boardView.ClearCell(readRow, c);
                            yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                            if (_boardView != null) _boardView.SetControllerAt(ctrl, writeRow, c);
                        }

                        if (_skillsService != null && sym == null && ctrl != null)
                        {
                            if (_skillsService.MultiplierTiles.TryGetValue((readRow, c), out var multVal))
                            {
                                _skillsService.RemoveMultiplierTile(readRow, c);
                                _skillsService.RegisterMultiplierTile(writeRow, c, multVal);
                                ctrl.Row = writeRow;
                                ctrl.Col = c;
                            }
                        }
                    }

                    writeRow--;
                }
            }

            var holes = writeRow + 1;
            if (holes > 0 && _boardView != null)
            {
                var antiMatchSymbols = GenerateAntiMatchSymbols(c, holes);

                for (var i = holes - 1; i >= 0; i--)
                {
                    if (_boardView == null) break;
                    var targetRow = i;
                    var newCtrl = _boardView.SpawnSymbol(-1, c, antiMatchSymbols[i]);
                    if (newCtrl == null || _boardView == null) continue;

                    newCtrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    var targetPos = _boardView.GetWorldPosition(targetRow, c);
                    yield return newCtrl.MoveToPosition(targetPos, _config.DropDuration);

                    if (_gameManager != null && _gameManager.Board != null)
                        _gameManager.Board.SetSymbol(targetRow, c, antiMatchSymbols[i]);
                    if (_boardView != null) _boardView.SetControllerAt(newCtrl, targetRow, c);
                }
            }

            onComplete?.Invoke();
        }

        private IEnumerator DropColumnTutorial(int c, Action onComplete)
        {
            if (_boardView == null || _gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var writeRow = _gameManager.Board.Rows - 1;
            for (var readRow = _gameManager.Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = _gameManager.Board.GetSymbol(readRow, c);
                if (sym != null)
                {
                    if (readRow != writeRow)
                    {
                        _gameManager.Board.SetSymbol(writeRow, c, sym);
                        _gameManager.Board.SetSymbol(readRow, c, null);
                        var ctrl = _boardView.GetSymbolController(readRow, c);
                        if (ctrl != null && _boardView != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
                            yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                            if (_boardView != null) _boardView.SetControllerAt(ctrl, writeRow, c);
                        }
                    }

                    writeRow--;
                }
            }

            var holes = writeRow + 1;
            if (holes > 0 && _boardView != null)
            {
                var tutorialRefill = _boardView.BuildSecondTutorialRefillNoMatches(
                    _gameManager._symbols, _gameManager.Board.Columns, _gameManager.Board.Rows, _gameManager.Board);

                var newSyms = tutorialRefill.ContainsKey(c) ? tutorialRefill[c] : new SymbolData[0];
                var filteredSyms = newSyms.Where(s => s != null && !s.isScatter && !s.isWild).ToList();
                var symbolsToSpawn = Mathf.Min(holes, filteredSyms.Count);

                for (var i = symbolsToSpawn - 1; i >= 0; i--)
                {
                    if (_boardView == null) break;
                    var targetRow = holes - symbolsToSpawn + i;
                    var ctrl = _boardView.SpawnSymbol(-1, c, filteredSyms[i]);
                    if (ctrl == null || _boardView == null) continue;

                    ctrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    var targetPos = _boardView.GetWorldPosition(targetRow, c);
                    yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);

                    if (_gameManager != null && _gameManager.Board != null)
                        _gameManager.Board.SetSymbol(targetRow, c, filteredSyms[i]);
                    if (_boardView != null) _boardView.SetControllerAt(ctrl, targetRow, c);
                }
            }

            onComplete?.Invoke();
        }

        private IEnumerator DropColumnAntiMatch(int c, Action onComplete)
        {
            if (_boardView == null || _gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var writeRow = _gameManager.Board.Rows - 1;
            for (var readRow = _gameManager.Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = _gameManager.Board.GetSymbol(readRow, c);
                var ctrl = _boardView.GetSymbolController(readRow, c);
                bool isOccupied = sym != null || ctrl != null;

                if (isOccupied)
                {
                    if (readRow != writeRow)
                    {
                        _gameManager.Board.SetSymbol(writeRow, c, sym);
                        _gameManager.Board.SetSymbol(readRow, c, null);

                        if (ctrl != null && _boardView != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
                            _boardView.ClearCell(readRow, c);
                            yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                            if (_boardView != null) _boardView.SetControllerAt(ctrl, writeRow, c);
                        }
                    }

                    writeRow--;
                }
            }

            var holes = writeRow + 1;
            if (holes > 0 && _boardView != null)
            {
                var antiMatchSymbols = GenerateAntiMatchSymbols(c, holes);

                for (var i = holes - 1; i >= 0; i--)
                {
                    if (_boardView == null) break;
                    var targetRow = i;
                    var newCtrl = _boardView.SpawnSymbol(-1, c, antiMatchSymbols[i]);
                    if (newCtrl == null || _boardView == null) continue;

                    newCtrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    var targetPos = _boardView.GetWorldPosition(targetRow, c);
                    yield return newCtrl.MoveToPosition(targetPos, _config.DropDuration);

                    if (_gameManager != null && _gameManager.Board != null)
                        _gameManager.Board.SetSymbol(targetRow, c, antiMatchSymbols[i]);
                    if (_boardView != null) _boardView.SetControllerAt(newCtrl, targetRow, c);
                }
            }

            onComplete?.Invoke();
        }

        private IEnumerator DropColumnWithNewSymbols(int c, SymbolData[] overrideNewSymbols, Action onComplete)
        {
            if (_boardView == null || _gameManager == null) { onComplete?.Invoke(); yield break; }

            var writeRow = _gameManager.Board.Rows - 1;
            for (var readRow = _gameManager.Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = _gameManager.Board.GetSymbol(readRow, c);
                var ctrl = _boardView.GetSymbolController(readRow, c);
                bool isOccupied = sym != null || ctrl != null;

                if (isOccupied)
                {
                    if (readRow != writeRow)
                    {
                        _gameManager.Board.SetSymbol(writeRow, c, sym);
                        _gameManager.Board.SetSymbol(readRow, c, null);
                        if (ctrl != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
                            _boardView.ClearCell(readRow, c);
                            yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                            _boardView.SetControllerAt(ctrl, writeRow, c);
                        }
                    }
                    writeRow--;
                }
            }

            var holes = writeRow + 1;
            if (holes > 0)
            {
                SymbolData[] newSyms;
                if (overrideNewSymbols != null && overrideNewSymbols.Length >= holes)
                    newSyms = overrideNewSymbols.Take(holes).ToArray();
                else if (overrideNewSymbols != null && overrideNewSymbols.Length > 0)
                    newSyms = overrideNewSymbols.Concat(_symbolGenerator.GenerateSymbolsForColumn(c)).Take(holes).ToArray();
                else
                    newSyms = _symbolGenerator.GenerateSymbolsForColumn(c).Take(holes).ToArray();

                for (var i = holes - 1; i >= 0; i--)
                {
                    if (_boardView == null) break;
                    var targetRow = i;
                    var symToSpawn = i < newSyms.Length ? newSyms[i] : newSyms[0];
                    var newCtrl = _boardView.SpawnSymbol(-1, c, symToSpawn);
                    if (newCtrl == null) continue;

                    newCtrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    yield return newCtrl.MoveToPosition(_boardView.GetWorldPosition(targetRow, c), _config.DropDuration);

                    _gameManager.Board.SetSymbol(targetRow, c, symToSpawn);
                    _boardView.SetControllerAt(newCtrl, targetRow, c);
                }
            }

            onComplete?.Invoke();
        }

        private SymbolData[] GenerateAntiMatchSymbols(int col, int count)
        {
            var result = new SymbolData[count];
            var allSymbols = _gameManager._symbols.Where(s => !s.isScatter && !s.isWild).ToList();
            if (allSymbols.Count == 0)
                return _symbolGenerator.GenerateSymbolsForColumn(col).Take(count).ToArray();

            var threshold = _config.SymbolCountThreshold;
            int extraCascades = _cascadeCounter - 3;
            float antiMatchChance = Mathf.Clamp01(0.85f + extraCascades * 0.05f);

            for (int i = 0; i < count; i++)
            {
                if (Random.value < antiMatchChance)
                {
                    var symbolCounts = new Dictionary<SymbolData, int>();
                    foreach (var s in allSymbols)
                        symbolCounts[s] = 0;

                    for (int r = 0; r < _gameManager.Board.Rows; r++)
                        for (int c = 0; c < _gameManager.Board.Columns; c++)
                        {
                            var sym = _gameManager.Board.GetSymbol(r, c);
                            if (sym != null && symbolCounts.ContainsKey(sym))
                                symbolCounts[sym]++;
                        }

                    var safe = allSymbols
                        .Where(s => symbolCounts[s] < threshold - 2)
                        .OrderBy(s => symbolCounts[s])
                        .ToList();

                    if (safe.Count > 0)
                        result[i] = safe[Random.Range(0, Mathf.Min(3, safe.Count))];
                    else
                    {
                        var least = allSymbols.OrderBy(s => symbolCounts[s]).First();
                        result[i] = least;
                    }
                }
                else
                {
                    var generated = _symbolGenerator.GenerateSymbolsForColumn(col);
                    result[i] = generated.Length > 0 ? generated[0] : allSymbols[Random.Range(0, allSymbols.Count)];
                }
            }

            return result;
        }

        private IEnumerator ProcessTutorialFirstSpin()
        {
            var groups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);

            if (groups.Count == 0 || IsTutorialOver)
            {
                _scoreService.ResetCurrentSpinScore();
                _scoreService.CommitDeferredPoints();
                WinCalculator.Reset();
                OnCompleted?.Invoke();
                yield break;
            }

            if (_boardView.FrameFirstMatchGroup != null)
                _boardView.FrameFirstMatchGroup.SetActive(true);

            var cascadeWin = WinCalculator.CalculateTotalWin();
            _scoreService.SetCurrentSpinScore(cascadeWin);
            TotalWin += cascadeWin;

            yield return new WaitForSeconds(1.0f);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            if (_boardView.FrameFirstMatchGroup != null)
                _boardView.FrameFirstMatchGroup.SetActive(false);

            yield return ProcessMatches(groups, 0);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            _skipThirdHint = false;
            _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                new TutorialHintsViewModel(TutorialHintGroup.ThirdGroup));
            _autoCloseThirdCoroutine = _coroutines.StartCoroutine(AutoCloseCurrentPopupAfter(6f));

            yield return WaitOrSkip(6f, () => _skipThirdHint);

            if (_skipThirdHint && _autoCloseThirdCoroutine != null)
            {
                _coroutines.StopCoroutine(_autoCloseThirdCoroutine);
                _autoCloseThirdCoroutine = null;
                _popupService.CloseCurrentPopup();
            }

            _tutorialFirstSpinRefillOverride = new Dictionary<int, SymbolData[]> 
            {
                [0] = new[] { _gameManager._symbols[1], _gameManager._symbols[1] },
                [1] = new[] { _gameManager._symbols[5] },
                [3] = new[] { _gameManager._symbols[4], _gameManager._symbols[4] },
            };
            yield return DropAndRefillWithOverride();
            _tutorialFirstSpinRefillOverride = null;
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            if (_boardView.FrameSecondMatchGroup != null)
                _boardView.FrameSecondMatchGroup.SetActive(true);

            var skipPreFourthWait = _skipThirdHint;
            _skipFourthHint = false;
            yield return WaitOrSkip(6.2f, () => _skipFourthHint || skipPreFourthWait);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            groups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);
            if (groups.Count > 0)
            {
                _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                    new TutorialHintsViewModel(TutorialHintGroup.FourthGroup));
                _autoCloseFourthCoroutine = _coroutines.StartCoroutine(AutoCloseCurrentPopupAfter(6f));

                yield return WaitOrSkip(6f, () => _skipFourthHint);

                if (_skipFourthHint && _autoCloseFourthCoroutine != null)
                {
                    _coroutines.StopCoroutine(_autoCloseFourthCoroutine);
                    _autoCloseFourthCoroutine = null;
                    _popupService.CloseCurrentPopup();
                }

                cascadeWin = WinCalculator.CalculateTotalWin();
                TotalWin += cascadeWin;

                if (_boardView.FrameSecondMatchGroup != null)
                    _boardView.FrameSecondMatchGroup.SetActive(false);

                yield return ProcessMatches(groups, 1);
                if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }
                yield return DropAndRefill();
                if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }
            }

            _autoCloseCoroutine = null;
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            _scoreService.CommitDeferredPoints();
            WinCalculator.Reset();

            _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                new TutorialHintsViewModel(TutorialHintGroup.FifthGroup));
            _skillsService.SetOneCharge();

            OnCompleted?.Invoke();
        }

        private IEnumerator WaitForCoroutine(Coroutine coroutine)
        {
            yield return coroutine;
        }

        private IEnumerator AutoCloseCurrentPopupAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _popupService.CloseCurrentPopup();
            _autoCloseCoroutine = null;
        }

        private IEnumerator ProcessTutorialAfterSkillSpin()
        {
            _spinSystem.IsCalculationAfterSkill = false;
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            var refillOverride = BuildMatchGuaranteeRefill();
            yield return DropAndRefillWithMatchGuarantee(refillOverride);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            int cascadesDone = 0;
            for (var cascadeIndex = 0; cascadeIndex < _maxCascades; cascadeIndex++)
            {
                if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

                var groups = _gameManager.Board.GetMatchingGroups(_paytable, _config.SymbolCountThreshold);
                if (groups.Count == 0) break;

                var cascadeWin = WinCalculator.CalculateTotalWin();
                if (cascadeIndex == 0)
                    _scoreService.SetCurrentSpinScore(cascadeWin);
                TotalWin += cascadeWin;

                yield return ProcessMatches(groups, cascadeIndex);
                if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }
                cascadesDone++;

                if (cascadesDone < 2)
                {
                    var nextRefill = BuildMatchGuaranteeRefill();
                    yield return DropAndRefillWithMatchGuarantee(nextRefill);
                }
                else
                {
                    yield return DropAndRefill();
                    break;
                }
            }

            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            _scoreService.CommitDeferredPoints();

            var megaWinClosed = false;
            _popupService.ShowPopup<MegaWinPopupView, MegaWinPopupViewModel>(
                new MegaWinPopupViewModel(TotalWin > 0 ? TotalWin : 9999),
                () => megaWinClosed = true);
            yield return new WaitUntil(() => megaWinClosed || IsTutorialOver);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            WinCalculator.Reset();
            OnCompleted?.Invoke();
        }

        private IEnumerator ProcessTutorialScatterSpin()
        {
            _scoreService.ResetCurrentSpinScore();
            _scoreService.CommitDeferredPoints();
            WinCalculator.Reset();

            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            _skipSeventhHint = false;
            _popupService.ShowPopup<TutorialHintsView, TutorialHintsViewModel>(
                new TutorialHintsViewModel(TutorialHintGroup.SevenGroup));

            yield return WaitOrSkip(5f, () => _skipSeventhHint);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            if (_skipSeventhHint)
                _popupService.CloseCurrentPopup();

            _uiOverlayService.ShowTransition();
            yield return new WaitForSeconds(1.4f);
            if (IsTutorialOver) { OnCompleted?.Invoke(); yield break; }

            _popupService.ShowPopup<MiniGamePopupView>();

            OnCompleted?.Invoke();
        }

        private IEnumerator WaitOrSkip(float duration, Func<bool> skipFlag)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (skipFlag()) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public void SkipThirdHint() => _skipThirdHint = true;
        public void SkipFourthHint() => _skipFourthHint = true;
        public void SkipSeventhHint() => _skipSeventhHint = true;

        public void Exit()
        {
            TutorialHintsView.OnSkipThirdHint -= SkipThirdHint;
            TutorialHintsView.OnSkipFourthHint -= SkipFourthHint;
            TutorialHintsView.OnSkipSeventhHint -= SkipSeventhHint;

            if (_processCoroutine != null)
            {
                _coroutines.StopCoroutine(_processCoroutine);
                _processCoroutine = null;
            }
        }
    }
}
