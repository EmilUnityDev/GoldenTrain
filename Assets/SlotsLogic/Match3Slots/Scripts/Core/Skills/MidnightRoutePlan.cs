using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public class MidnightRoutePlan
    {
        public List<(int r, int c)> Chain { get; }
        
        public SymbolData ClusterSymbol { get; }

        public MidnightRoutePlan(List<(int r, int c)> chain, SymbolData clusterSymbol)
        {
            Chain = chain;
            ClusterSymbol = clusterSymbol;
        }
    }
}

