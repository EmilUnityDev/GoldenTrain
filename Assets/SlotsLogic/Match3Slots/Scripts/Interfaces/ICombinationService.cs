using System.Collections.Generic;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface ICombinationService
    {
        string GetColumnData(int columnIndex);
        List<string> GenerateReelSymbols();
        void LoadReelSymbols(IEnumerable<string> symbols);
    }
}