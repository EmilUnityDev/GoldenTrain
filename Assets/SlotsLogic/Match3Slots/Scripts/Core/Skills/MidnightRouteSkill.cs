using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Skills;
using Game.Scripts.Utils;
using SymbolScripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public class MidnightRouteSkill : ISkill
    {
        public SkillType Type => SkillType.MidnightRoute;

        private const float SwapDelay = 0.37f;

        private static readonly int[] MultiplierValues = { 2, 3, 5, 10, 15, 20 };

        private static readonly List<(int r, int c)> FixedChain = new()
        {
            (0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (3, 2), (4, 2)
        };

        private readonly BoardView _boardView;
        private readonly IPopupService _popupService;
        private readonly IFeedbackService _audioService;
        private readonly SceneContextProvider _sceneContext;
        private readonly UIRootView _uiRootView;
        private readonly Coroutines _coroutines;
        private readonly IObjectResolver _resolver;

        private SkillsService _service;
        private SlotGameManager _gameManager;
        private GameObject _backgroundGo;

        public MidnightRouteSkill(BoardView boardView, IPopupService popupService, IFeedbackService audioService, SceneContextProvider sceneContext, UIRootView uiRootView = null, Coroutines coroutines = null, IObjectResolver resolver = null)
        {
            _boardView = boardView;
            _popupService = popupService;
            _audioService = audioService;
            _sceneContext = sceneContext;
            _uiRootView = uiRootView;
            _coroutines = coroutines;
            _resolver = resolver;
        }

        public void Activate(SkillsService service, SlotGameManager gameManager, int charges)
        {
            _service = service;
            _gameManager = gameManager;

            var normalSymbols = gameManager._symbols.Where(s => !s.isWild && !s.isScatter).ToList();
            var clusterSymbol = normalSymbols.Count > 0
                ? normalSymbols[Random.Range(0, normalSymbols.Count)]
                : gameManager._symbols[0];

            Debug.Log($"[MIDNIGHT_ROUTE] Activate: chain=[{string.Join(", ", FixedChain.Select(p => $"({p.r},{p.c})"))}], clusterSymbol='{clusterSymbol.symbolID}'");

            gameManager.SpinSystem.MidnightRoutePlan = new MidnightRoutePlan(new List<(int r, int c)>(FixedChain), clusterSymbol);
            gameManager.SpinSystem.BeforeCalculationHook = () => BeforeCalculationRoutine(gameManager, service, new List<(int r, int c)>(FixedChain));
            gameManager.SpinSystem.StartSpinningAfterSkill();

            // Спавним фон
            if (_gameManager._skillsBackgroundPrefab != null && _uiRootView != null)
            {
                _uiRootView.UpdateCanvasOrder(0);
                _backgroundGo = UnityEngine.Object.Instantiate(_gameManager._skillsBackgroundPrefab, _uiRootView.UISceneContainer);
            }
        }

        public bool CanActivate(SkillsService service) => true;

        public void Cancel()
        {
            CleanupBackground();
        }

        private IEnumerator BeforeCalculationRoutine(SlotGameManager gameManager, SkillsService service, List<(int r, int c)> chain)
        {
            RailsAnimationView railsView = null;
            if (gameManager._railsAnimationPrefab != null && gameManager._animationCanvas != null)
            {
                var railsGo = Object.Instantiate(gameManager._railsAnimationPrefab, gameManager._animationCanvas);
                _resolver?.InjectGameObject(railsGo);
                railsView = railsGo.GetComponent<RailsAnimationView>();
            }

            bool railsDone = false;

            if (railsView != null)
            {
                railsView.Play(
                    onTrainEnabled: () =>
                    {
                        if (_coroutines != null)
                            _coroutines.StartCoroutine(PlaceMultipliersOnChain(gameManager, service, chain));
                    },
                    onComplete: () => railsDone = true
                );

                yield return new WaitUntil(() => railsDone);
            }
            else
            {
                yield return PlaceMultipliersOnChain(gameManager, service, chain);
            }

            CleanupBackground();

            yield return new WaitForSeconds(0.5f);

            service.FinishActiveSkillSilently();
        }

        private IEnumerator PlaceMultipliersOnChain(SlotGameManager gameManager, SkillsService service, List<(int r, int c)> chain)
        {
            var boardView = gameManager._boardView;
            var multiplierPrefab = gameManager._multiplierPrefab;

            if (chain.Count == 0)
            {
                Debug.Log("[MIDNIGHT_ROUTE] Chain is empty, skipping.");
                yield break;
            }

            Debug.Log($"[MIDNIGHT_ROUTE] Placing multipliers on {chain.Count} chain cells.");

            foreach (var (r, c) in chain)
            {
                int mult = MultiplierValues[Random.Range(0, MultiplierValues.Length)];

                var existingCtrl = boardView.GetSymbolController(r, c);
                if (existingCtrl != null)
                {
                    existingCtrl.DestroyAnim();
                    boardView.ClearCell(r, c);
                    gameManager.Board.SetSymbol(r, c, null);
                }

                if (multiplierPrefab != null)
                {
                    var worldPos = boardView.GetWorldPosition(r, c);
                    var go = Object.Instantiate(multiplierPrefab, worldPos, Quaternion.identity, boardView.transform);
                    var symMult = go.GetComponent<SymbolMultiplier>();
                    if (symMult != null)
                    {
                        symMult.Row = r;
                        symMult.Col = c;
                        symMult.Data = null;
                        symMult.SetValue(mult);
                        boardView.SetControllerAt(symMult, r, c);
                        gameManager.Board.SetSymbol(r, c, null);
                        try { symMult.SymbolSpawn(); } catch { }
                    }
                }

                service.RegisterMultiplierTile(r, c, mult);
                Debug.Log($"[MIDNIGHT_ROUTE] Placed x{mult} multiplier at ({r},{c})");

                yield return new WaitForSeconds(SwapDelay);
            }

            Debug.Log("[MIDNIGHT_ROUTE] All multipliers placed.");
        }

        private void CleanupBackground()
        {
            if (_uiRootView != null)
                _uiRootView.UpdateCanvasOrder(10);
            if (_backgroundGo != null)
            {
                UnityEngine.Object.Destroy(_backgroundGo);
                _backgroundGo = null;
            }
        }
    }
}