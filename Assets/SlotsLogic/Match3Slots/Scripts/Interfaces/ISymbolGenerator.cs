using SlotsLogic.Match3Slots.Scripts.Core;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface ISymbolGenerator
    {
        SymbolData[] GenerateSymbolsForColumn(int columnIndex);
    }
}