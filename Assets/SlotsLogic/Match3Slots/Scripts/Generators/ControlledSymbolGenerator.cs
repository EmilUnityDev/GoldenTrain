using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Generators
{
    public class ControlledSymbolGenerator : ISymbolGenerator
    {
        private readonly ICombinationService _combinationService;
        private readonly IReelLengthService _reelLengthService;
        private readonly List<SymbolData> _symbols;

        public ControlledSymbolGenerator(
            ICombinationService combinationService,
            IReelLengthService reelLengthService,
            List<SymbolData> symbols)
        {
            _combinationService = combinationService ?? throw new System.ArgumentNullException(nameof(combinationService));
            _reelLengthService = reelLengthService ?? throw new System.ArgumentNullException(nameof(reelLengthService));
            _symbols = symbols ?? throw new System.ArgumentNullException(nameof(symbols));
        }

        public SymbolData[] GenerateSymbolsForColumn(int columnIndex)
        {
            int length = _reelLengthService.GetReelLengths()[columnIndex];
            var dataStr = _combinationService.GetColumnData(columnIndex);
            var list = dataStr.Split(',');
            int offset = Random.Range(0, list.Length - length);
            var outArr = new SymbolData[length];
            for (int i = 0; i < length; i++)
            {
                string id = list[offset + i];
                outArr[i] = _symbols.Find(s => s.symbolID == id);
            }
            return outArr;
        }
    }
}