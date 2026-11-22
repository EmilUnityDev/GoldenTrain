using System;
using System.Collections.Generic;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    [Serializable]
    public class ColumnData
    {
        public int column;
        public string symbols;
    
        private List<string> _symbolsList;
        public List<string> SymbolsList
        {
            get
            {
                if(_symbolsList == null) ParseSymbols();
                return _symbolsList;
            }
        }

        public void ParseSymbols()
        {
            _symbolsList = new List<string>(symbols.Split(','));
        }
    }
}