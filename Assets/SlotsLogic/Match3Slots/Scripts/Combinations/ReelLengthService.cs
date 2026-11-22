using System.Collections.Generic;
using System.Linq;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class ReelLengthService : IReelLengthService
    {
        private readonly List<int> _lengths;

        public ReelLengthService(SlotGameConfig config)
        {
            _lengths = Enumerable
                .Repeat(config.Rows, config.Columns)
                .ToList();
        }

        public ReelLengthService(IEnumerable<int> loadedLengths)
        {
            _lengths = new List<int>(loadedLengths);
        }

        public List<int> GetReelLengths()
            => new List<int>(_lengths);
    }
}