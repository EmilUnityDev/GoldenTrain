using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface IMatchChecker
    {
        List<(int row, int col)> FindMatches(Board board);
    }
}