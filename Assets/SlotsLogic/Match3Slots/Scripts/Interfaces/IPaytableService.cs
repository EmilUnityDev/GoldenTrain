using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface IPaytableService
    {
        int GetReward(string symbol, int matchCount);
        string GetPaytableJson();
        Dictionary<string, Dictionary<string, string>> GenerateDefaultPaytable(IEnumerable<SymbolData> symbols);
        IEnumerable<string> GetAllSymbolIds();
    }
}