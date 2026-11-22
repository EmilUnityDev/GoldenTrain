using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface IDataGenerator
    {
        SlotGameConfig Config { get; }
        List<SymbolData> Symbols { get; }
        List<string> GenerateColumn();
    }
}