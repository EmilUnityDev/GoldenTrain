using System;
using System.Collections.Generic;
using System.Linq;
using SlotsLogic.Match3Slots.Scripts.Combinations;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    [Serializable]
    public class Board
    {
        private SymbolData[,] _grid;

        public int Rows { get; }

        public int Columns { get; }

        public Board(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public void InitializeBoard()
        {
            _grid = new SymbolData[Rows, Columns];
        }

        public SymbolData GetSymbol(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Columns)
                return null;
            return _grid[row, col];
        }

        public void SetSymbol(int row, int col, SymbolData symbol)
        {
            if (row >= 0 && row < Rows && col >= 0 && col < Columns)
            {
                _grid[row, col] = symbol;
            }
        }

        public void FillBoard(ISymbolGenerator generator)
        {
            for (var c = 0; c < Columns; c++)
            {
                var symbols = generator.GenerateSymbolsForColumn(c);
                var len = symbols.Length;

                for (var r = 0; r < Rows; r++) _grid[r, c] = r < len ? symbols[r] : null;
            }
        }

        public void FillBoardNoMatches(ISymbolGenerator generator, IMatchChecker checker)
        {
            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                FillBoard(generator);

                if (!checker.FindMatches(this).Any())
                    return;
            }

            FillBoard(generator);
        }

        public Dictionary<string, int> GetSymbolCounts()
        {
            var counts = new Dictionary<string, int>();
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && !sym.isWild && !sym.isScatter)
                {
                    if (!counts.ContainsKey(sym.symbolID))
                        counts[sym.symbolID] = 0;
                    counts[sym.symbolID]++;
                }
            }

            return counts;
        }

        public int CountWilds()
        {
            var count = 0;
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && sym.isWild) count++;
            }

            return count;
        }

        public List<(int row, int col)> GetWildPositions()
        {
            var positions = new List<(int, int)>();
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && sym.isWild) positions.Add((r, c));
            }

            return positions;
        }

        public List<(int row, int col)> GetPositionsForSymbol(string id)
        {
            var positions = new List<(int, int)>();
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && sym.symbolID == id) positions.Add((r, c));
            }

            return positions;
        }

        public int CountScatters()
        {
            var count = 0;
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && sym.isScatter) count++;
            }

            return count;
        }

        public List<(int row, int col)> GetScatterPositions()
        {
            var positions = new List<(int, int)>();
            for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
            {
                var sym = GetSymbol(r, c);
                if (sym != null && sym.isScatter) positions.Add((r, c));
            }

            return positions;
        }

        public List<List<(int row, int col)>> GetMatchingGroups(IPaytableService paytable, int threshold)
        {
            var (effective, allocated) = ComputeAllocatedWilds(paytable, threshold);
            var groups = new List<List<(int row, int col)>>();
            foreach (var sym in effective.Keys.Where(s => effective[s] >= threshold))
            {
                var group = GetPositionsForSymbol(sym);
                if (allocated.TryGetValue(sym, out var wps)) group.AddRange(wps);
                groups.Add(group);
            }

            if (CountScatters() >= 3) groups.Add(GetScatterPositions());
            return groups;
        }

        private (Dictionary<string, int> effective, Dictionary<string, List<(int row, int col)>> allocated)
            ComputeAllocatedWilds(IPaytableService paytable, int threshold)
        {
            var baseCounts = GetSymbolCounts();
            var wildPositions = GetWildPositions();
            var wildCount = wildPositions.Count;
            var allocated = new Dictionary<string, List<(int row, int col)>>();
            var effective = new Dictionary<string, int>(baseCounts);

            if (wildCount == 0)
            {
                return (effective, allocated);
            }

            string bestSymbolId = null;
            int maxReward = -1;

            var symbolIdsToTest = baseCounts.Keys.ToList();
            
            var allSymbolsPaytable = paytable.GetAllSymbolIds();
            foreach (var s in allSymbolsPaytable)
            {
                if (!symbolIdsToTest.Contains(s))
                {
                    symbolIdsToTest.Add(s);
                }
            }

            foreach (var symbolId in symbolIdsToTest)
            {
                var currentCount = baseCounts.GetValueOrDefault(symbolId, 0);
                var potentialCount = currentCount + wildCount;
                var potentialReward = paytable.GetReward(symbolId, potentialCount);

                if (potentialReward > maxReward)
                {
                    maxReward = potentialReward;
                    bestSymbolId = symbolId;
                }
            }

            if (bestSymbolId != null)
            {
                if (!effective.ContainsKey(bestSymbolId))
                {
                    effective[bestSymbolId] = 0;
                }
                effective[bestSymbolId] += wildCount;
                allocated[bestSymbolId] = wildPositions;
            }

            return (effective, allocated);
        }
        
        public Dictionary<string, int> GetEffectiveSymbolCounts(IPaytableService paytable, int threshold)
        {
            var (effective, _) = ComputeAllocatedWilds(paytable, threshold);
            return effective;
        }
    }
}