using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Scripts.Game.GameRoot;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.Score;
using Game.Scripts.Tutorial.Root;
using SlotsLogic.Match3Slots.Scripts.Combinations;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;
using VContainer;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class WinCalculator : MonoBehaviour, IWinCalculator
    {
        private IPaytableService _paytableService;
        private Board _board;
        private int _manualWin;
        private ScoreService _scoreService;
        private SceneContextProvider _sceneContextProvider;

        private bool _wasOpened = false;
        private int _scattersCountForMiniGame = 3;
        
        public int WheelSpinsCount { get; private set; }

        [Inject]
        public void Construct(SceneContextProvider sceneContextProvider)
        {
            _sceneContextProvider = sceneContextProvider;
        }

        public void Initialize(IPaytableService paytableService, Board board, ScoreService scoreService)
        {
            _paytableService = paytableService ?? throw new ArgumentNullException(nameof(paytableService));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            Reset();
        }

        public void Reset()
        {
            WheelSpinsCount = 0;
            _manualWin = 0;
            
            if (_sceneContextProvider != null && _sceneContextProvider.IsTutorialScene())
            {
                _scattersCountForMiniGame = 4;
            }
        }

        public int CalculateTotalWin()
        {
            var threshold = ((PaytableService)_paytableService).MinThreshold;
            var effectiveCounts = _board.GetEffectiveSymbolCounts(_paytableService, threshold);
            var totalWin = _manualWin;

            foreach (var kv in effectiveCounts.Where(kvp => kvp.Value >= threshold))
            {
                var reward = _paytableService.GetReward(kv.Key, kv.Value);
                var multiplier = GetMultiplier(kv.Value);
                var comboWin = (int)(reward * multiplier);
                totalWin += comboWin;
                Debug.Log($"{kv.Key}: count={kv.Value}, reward={reward}, multiplier={multiplier}, comboWin={comboWin}");
                if (kv.Value >= 10) Debug.Log("Book Burst mini-explosion!");
            }

            var scatterCount = _board.CountScatters();
            if (scatterCount >= _scattersCountForMiniGame)
            {
                Debug.Log($"scatter count: {scatterCount}");
                WheelSpinsCount += 1;
                Debug.Log($"Added {1} wheel spins for {scatterCount} scatters. Total: {WheelSpinsCount}");
            }

            _manualWin = 0;

            if (totalWin > 0 && _scoreService != null)
            {
                _scoreService.AddPointsDeferred(totalWin);
                Debug.Log(
                    $"[WinCalculator] Deferred {totalWin} points. Will be committed at end of spin. CurrentSpinScore: {_scoreService.CurrentSpinScore}");
            }

            return totalWin;
        }
        
        public int CalculateWinForSelectedSymbols(List<(int row, int col)> positions)
        {
            if (positions == null || positions.Count == 0) return 0;

            var symbolCounts = positions
                .Select(p => _board.GetSymbol(p.row, p.col))
                .Where(sym => sym != null && !sym.isWild && !sym.isScatter)
                .GroupBy(sym => sym.symbolID)
                .ToDictionary(g => g.Key, g => g.Count());

            if (symbolCounts.Count == 0)
            {
                return 0;
            }
            
            var treatedCount = positions.Count;
            var threshold = ((PaytableService)_paytableService).MinThreshold;

            var bestId = symbolCounts.Keys
                .OrderByDescending(id => _paytableService.GetReward(id, Mathf.Max(1, treatedCount)))
                .First();

            var reward = _paytableService.GetReward(bestId, treatedCount);

            if (reward == 0)
            {
                var rewardAtThreshold = _paytableService.GetReward(bestId, threshold);
                if (rewardAtThreshold > 0)
                {
                    reward = Mathf.RoundToInt(rewardAtThreshold * (treatedCount / (float)threshold));
                }
            }

            var multiplier = GetMultiplier(treatedCount);

            var finalWin = Mathf.RoundToInt(reward * multiplier);

            Debug.Log(
                $"[WinCalculator] CalculateWinForSelectedSymbols -> bestId={bestId}, treatedCount={treatedCount}, reward={reward}, multiplier={multiplier}, finalWin={finalWin}");

            return finalWin;
        }
        
        private float GetMultiplier(int size)
        {
            return size switch
            {
                3 => 1f,
                4 => 2f,
                5 => 1f,
                6 => 1f,
                7 => 2f,
                8 => 2f,
                9 => 3f,
                10 => 3f,
                _ => 3f
            };
        }
    }
}