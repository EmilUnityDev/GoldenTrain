using System;
using System.Collections.Generic;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    [Serializable]
    public class AllColumnsData
    {
        public List<int> reelsLength = new();
        public List<string> reelSymbols = new();
        public Dictionary<string, Dictionary<string, string>> paytable = new();
    }
}