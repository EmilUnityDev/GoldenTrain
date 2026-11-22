using System;
using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Interfaces;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class CombinationService : ICombinationService
    {
        private readonly IDataGenerator _dataGenerator;
        private List<string> _reelSymbols = new();

        public CombinationService(IDataGenerator dataGenerator)
        {
            _dataGenerator = dataGenerator ?? throw new ArgumentNullException(nameof(dataGenerator));
        }

        public List<string> GenerateReelSymbols()
        {
            var symbols = new List<string>();
            for (var col = 0; col < _dataGenerator.Config.Columns; col++)
            {
                var raw = _dataGenerator.GenerateColumn();
                symbols.Add(string.Join(",", raw));
            }

            return symbols;
        }

        public void LoadReelSymbols(IEnumerable<string> symbols)
        {
            _reelSymbols = new List<string>(symbols);
        }

        public string GetColumnData(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < _reelSymbols.Count)
                return _reelSymbols[columnIndex];
            return null;
        }
    }
}