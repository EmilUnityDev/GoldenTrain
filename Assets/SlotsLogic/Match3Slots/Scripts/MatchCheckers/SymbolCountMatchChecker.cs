using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;

namespace SlotsLogic.Match3Slots.Scripts.MatchCheckers
{
    public class SymbolCountMatchChecker : IMatchChecker
    {
        private readonly int _targetCount;

        public SymbolCountMatchChecker(int targetCount)
        {
            _targetCount = targetCount;
        }

        public List<(int row, int col)> FindMatches(Board board)
        {
            int rows = board.Rows;
            int cols = board.Columns;

            Dictionary<string, List<(int row, int col)>> symbolGroups = new Dictionary<string, List<(int, int)>>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var symbol = board.GetSymbol(r, c);
                    if (symbol == null || symbol.isWild || symbol.isScatter) continue;
                    string symbolID = symbol.symbolID;
                    if (!symbolGroups.ContainsKey(symbolID))
                    {
                        symbolGroups[symbolID] = new List<(int, int)>();
                    }
                    symbolGroups[symbolID].Add((r, c));
                }
            }

            var matches = new List<(int row, int col)>();
            foreach (var group in symbolGroups.Values)
            {
                if (group.Count >= _targetCount)
                {
                    matches.AddRange(group);
                }
            }

            return matches;
        }
    }
}