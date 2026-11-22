using System;
using SymbolScripts;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public static class SymbolInputHandler
    {
        public static event Action<SymbolBase> OnSymbolClicked;

        public static void RegisterClick(SymbolBase symbol)
        {
            OnSymbolClicked?.Invoke(symbol);
        }
    }
}