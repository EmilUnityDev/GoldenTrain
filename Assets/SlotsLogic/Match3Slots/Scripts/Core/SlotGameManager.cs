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
using Game.Scripts.Services.Score;
using Game.Scripts.Utils;
using SlotsLogic.Match3Slots.Scripts.Combinations;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SlotsLogic.Match3Slots.Scripts.Generators;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using SlotsLogic.Match3Slots.Scripts.MatchCheckers;
using SpinSystem.States;
using TutorScripts;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class SlotGameManager : MonoBehaviour, ISlot
    {
        [Header("Calculation")] [SerializeField]
        private WinCalculator calculation;

        [Header("Config")] [SerializeField] private SlotGameConfig _config;

        [Header("References")] [SerializeField]
        public BoardView _boardView;

        [SerializeField] private Button _calculateButton;
        [SerializeField] private Button _updateBoardButton;

        [Header("Chance")] [Range(0, 80)] [SerializeField]
        private float Chance;

        [SerializeField] public List<SymbolData> _symbols;
        [SerializeField] public GameObject _multiplierPrefab;
        [SerializeField] public GameObject _cateringAnimationPrefab;
        [SerializeField] public GameObject _skillsBackgroundPrefab;
        [SerializeField] public GameObject _railsAnimationPrefab;
        public GameObject CollectionPopupPrefab;
        public Transform _animationCanvas;
        
        [Header("Background")] 
        [SerializeField] private SpriteRenderer _mainBGRenderer;
        [SerializeField] private Sprite _secondLevelBG;
        [SerializeField] private GameObject _secondBG;
        [SerializeField] private GameObject _thirdBG;
        
        public Board Board;
        private ISymbolGenerator _symbolGenerator;
        private IMatchChecker _matchChecker;
        private ICombinationService _combinationService;
        private IPaytableService _paytableService;
        private IReelLengthService _reelLengthService;
        private GameplayController _gameplayController;

        public SkillsService SkillsService { get; private set; }

        public SpinSystem.SpinSystem SpinSystem { get; private set; }

        public SlotLimits SlotLimits { get; private set; }

        public event Action OnSpinCompleate;
        public bool IsBusy { get; set; }
        public SlotGameConfig Config => _config;
        
        public void Initialize(IObjectResolver container)
        {
            var storage = new DataStorage();
            var dataGenerator = new DataGenerator(_config, _symbols, Chance);
            var combinationSvc = new CombinationService(dataGenerator);
            var defaultReelService = new ReelLengthService(_config);
            var defaultPayService = new PaytableService();
            var initService =
                new DataInitializationService(storage, combinationSvc, defaultReelService, defaultPayService);
            var allData = initService.Initialize(_symbols);
            _reelLengthService = new ReelLengthService(allData.reelsLength);
            _paytableService = new PaytableService(allData.paytable);
            ((PaytableService)_paytableService).MinThreshold = _config.SymbolCountThreshold;
            _combinationService = combinationSvc;
            _symbolGenerator = new ControlledSymbolGenerator(_combinationService, _reelLengthService, _symbols);
            _matchChecker = new SymbolCountMatchChecker(_config.SymbolCountThreshold);
            var maxRows = _reelLengthService.GetReelLengths().Max();
            Board = new Board(maxRows, _config.Columns);

            var scoreService = container.Resolve<ScoreService>();
            var moneyService = container.Resolve<MoneyService>();
            var sceneContext = container.Resolve<SceneContextProvider>();
            var audioService = container.Resolve<IFeedbackService>();
            var popupService = container.Resolve<IPopupService>();
            var coroutines = container.Resolve<Coroutines>();
            var uiRootView = container.Resolve<UIRootView>();
            var uiOverlayService = container.Resolve<IUIOverlayService>();
            DailyQuestService dailyQuestService = null;
            try { dailyQuestService = container.Resolve<DailyQuestService>(); } catch { /* optional */ }
            try
            {
                _gameplayController = container.Resolve<GameplayController>();
            }
            catch
            {
                /* Ignore if missing */
            }

            SlotLimits = container.Resolve<SlotLimits>();

            calculation.Construct(sceneContext);
            calculation.Initialize(_paytableService, Board, scoreService);

            MainTutorialScreensController tutorialController = null;
            try { tutorialController = container.Resolve<MainTutorialScreensController>(); } catch { /* optional */ }

            CollectionUnlockService collectionUnlockService = null;
            try { collectionUnlockService = container.Resolve<CollectionUnlockService>(); } catch { /* optional */ }

            SpinSystem = new SpinSystem.SpinSystem(
                this, _boardView, _symbolGenerator, calculation, _matchChecker,
                _paytableService, _config, SlotLimits, sceneContext, scoreService,
                popupService, coroutines, null, uiOverlayService, audioService,
                dailyQuestService, _gameplayController, tutorialController,
                collectionUnlockService, uiRootView, moneyService
            );

            var wildSymbol = _symbols[8];
            SkillsService = new SkillsService(wildSymbol, _boardView, SpinSystem, popupService, audioService,
                sceneContext, container, scoreService, null, coroutines, uiRootView);

            SpinSystem.SetSkillsService(SkillsService);
            
            if (_calculateButton != null) _calculateButton.onClick.AddListener(CalculateCombinations);
            if (_updateBoardButton != null) _updateBoardButton.onClick.AddListener(UpdateBoard);
            
            var levelService = container.Resolve<LevelProgressService>();
            if (_gameplayController != null && levelService.Level > 1)
            {
                _mainBGRenderer.sprite = _secondLevelBG;
                if (_secondBG != null) _secondBG.SetActive(false);
                if (_thirdBG != null) _thirdBG.SetActive(false);
            }
        }

        public void StartGame()
        {
            Board.InitializeBoard();
            Board.FillBoardNoMatches(_symbolGenerator, _matchChecker);
            _boardView.Setup(Board);
            for (var r = 0; r < Board.Rows; r++)
            for (var c = 0; c < Board.Columns; c++)
            {
                var sym = Board.GetSymbol(r, c);
                if (sym != null) _boardView.SpawnSymbol(r, c, sym);
            }
        }

        public bool Spin()
        {
            if (IsBusy)
            {
                Debug.Log("[SlotGameManager] Spin занят, подождите!");
                return false;
            }

            IsBusy = true;
            StartCoroutine(PerformSpin());
            return true;
        }

        private IEnumerator PerformSpin()
        {
            yield return _boardView.FlyOutAndClearBoard(_config.SpinFlyOutDuration);
            Board.FillBoard(_symbolGenerator);
            for (var r = 0; r < Board.Rows; r++)
            for (var c = 0; c < Board.Columns; c++)
            {
                var sym = Board.GetSymbol(r, c);
                if (sym != null) _boardView.SpawnSymbol(r, c, sym);
            }

            yield return new WaitForSeconds(_config.SpinExtraWait);
            CalculateCombinations();
            OnSpinCompleate?.Invoke();
            if (SlotLimits.EndGame())
                IsBusy = false;
            else
                _gameplayController?.EndGame();
        }

        public void CalculateCombinations()
        {
            var totalWin = calculation.CalculateTotalWin();
            Debug.Log($"Total win: {totalWin}");
            var matches = GetMatchPositionsIncludingWild();
            StartCoroutine(ProcessMatches(matches));
        }

        public List<(int row, int col)> GetMatchPositionsIncludingWild()
        {
            var groups = Board.GetMatchingGroups(_paytableService, _config.SymbolCountThreshold);
            return groups.SelectMany(g => g).Distinct().ToList();
        }

        public IEnumerator ProcessMatches(List<(int row, int col)> matches)
        {
            var groups = GroupMatchesBySymbol(matches);
            foreach (var group in groups)
            {
                foreach (var (r, c) in group)
                {
                    var ctrl = _boardView.GetSymbolController(r, c);
                    if (ctrl != null) ctrl.DestroyAnim();
                }

                yield return new WaitForSeconds(_config.SymbolDestructionFadeDuration);
                yield return new WaitForSeconds(0.12f);
            }

            foreach (var (r, c) in matches)
            {
                Board.SetSymbol(r, c, null);
                _boardView.ClearCell(r, c);
            }

            var columnDrops = new List<IEnumerator>();
            for (var c = 0; c < Board.Columns; c++) columnDrops.Add(DropColumn(c));
            var done = 0;
            foreach (var drop in columnDrops) StartCoroutine(Wrap(drop, () => done++));
            while (done < Board.Columns) yield return null;
            yield return new WaitForSeconds(_config.DropExtraWait);
        }

        private List<List<(int row, int col)>> GroupMatchesBySymbol(List<(int row, int col)> matches)
        {
            var groups = new List<List<(int row, int col)>>();
            var symbolGroups = new Dictionary<string, List<(int row, int col)>>();
            var wildGroups = new Dictionary<string, List<(int row, int col)>>();
            foreach (var (r, c) in matches)
            {
                var sym = Board.GetSymbol(r, c);
                if (sym == null) continue;
                if (sym.isScatter)
                {
                    if (!symbolGroups.ContainsKey("scatter")) symbolGroups["scatter"] = new List<(int row, int col)>();
                    symbolGroups["scatter"].Add((r, c));
                }
                else if (sym.isWild)
                {
                }
                else
                {
                    if (!symbolGroups.ContainsKey(sym.symbolID))
                        symbolGroups[sym.symbolID] = new List<(int row, int col)>();
                    symbolGroups[sym.symbolID].Add((r, c));
                }
            }

            var wildPositions = matches.Where(p => Board.GetSymbol(p.row, p.col)?.isWild ?? false).ToList();
            foreach (var kv in symbolGroups)
            {
                var group = kv.Value;
                var names = string.Join(", ", group.Select(p => Board.GetSymbol(p.row, p.col)?.symbolID ?? "empty"));
                Debug.Log($"Match group for {kv.Key}: {names}");
                groups.Add(group);
            }

            return groups;
        }

        private IEnumerator DropColumn(int c)
        {
            var writeRow = Board.Rows - 1;
            for (var readRow = Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = Board.GetSymbol(readRow, c);
                if (sym != null)
                {
                    if (readRow != writeRow)
                    {
                        Board.SetSymbol(writeRow, c, sym);
                        Board.SetSymbol(readRow, c, null);
                        var ctrl = _boardView.GetSymbolController(readRow, c);
                        if (ctrl != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
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
                var newSyms = _symbolGenerator.GenerateSymbolsForColumn(c).Take(holes).ToArray();
                for (var i = holes - 1; i >= 0; i--)
                {
                    var targetRow = i;
                    var ctrl = _boardView.SpawnSymbol(-1, c, newSyms[i]);
                    ctrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    var targetPos = _boardView.GetWorldPosition(targetRow, c);
                    yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                    Board.SetSymbol(targetRow, c, newSyms[i]);
                    _boardView.SetControllerAt(ctrl, targetRow, c);
                }
            }
        }

        public IEnumerator RefillBoardAfterRemoved(HashSet<(int row, int col)> removed)
        {
            var columns = removed.Select(p => p.col).Distinct().ToList();
            var routines = new List<IEnumerator>();
            foreach (var col in columns) routines.Add(RefillColumn(col));
            var total = routines.Count;
            var done = 0;
            foreach (var r in routines) StartCoroutine(Wrap(r, () => done++));
            while (done < total) yield return null;
        }

        private IEnumerator Wrap(IEnumerator r, Action cb)
        {
            yield return StartCoroutine(r);
            cb?.Invoke();
        }

        private IEnumerator RefillColumn(int c)
        {
            var writeRow = Board.Rows - 1;
            for (var readRow = Board.Rows - 1; readRow >= 0; readRow--)
            {
                var sym = Board.GetSymbol(readRow, c);
                if (sym != null)
                {
                    if (readRow != writeRow)
                    {
                        Board.SetSymbol(writeRow, c, sym);
                        Board.SetSymbol(readRow, c, null);
                        var ctrl = _boardView.GetSymbolController(readRow, c);
                        if (ctrl != null)
                        {
                            var targetPos = _boardView.GetWorldPosition(writeRow, c);
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
                var newSyms = _symbolGenerator.GenerateSymbolsForColumn(c).Take(holes).ToArray();
                for (var i = holes - 1; i >= 0; i--)
                {
                    var targetRow = i;
                    var ctrl = _boardView.SpawnSymbol(-1, c, newSyms[i]);
                    ctrl.transform.position = _boardView.GetWorldPosition(targetRow, c) + Vector3.up * 3f;
                    var targetPos = _boardView.GetWorldPosition(targetRow, c);
                    yield return ctrl.MoveToPosition(targetPos, _config.DropDuration);
                    Board.SetSymbol(targetRow, c, newSyms[i]);
                    _boardView.SetControllerAt(ctrl, targetRow, c);
                }
            }
        }

        public void UpdateBoard()
        {
            Board.FillBoardNoMatches(_symbolGenerator, _matchChecker);
            StartCoroutine(UpdateBoardCoroutine());
        }

        private IEnumerator UpdateBoardCoroutine()
        {
            yield return _boardView.FlyOutAndClearBoard(0.1f);
            for (var r = 0; r < Board.Rows; r++)
            for (var c = 0; c < Board.Columns; c++)
            {
                var sym = Board.GetSymbol(r, c);
                if (sym != null) _boardView.SpawnSymbol(r, c, sym);
            }

            Debug.Log("Board updated");
        }
    }
}
