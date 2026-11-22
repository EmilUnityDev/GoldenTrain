using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class PaytableService : IPaytableService
    {
        private readonly Dictionary<string, Dictionary<int, int>> _mapping;
        private readonly Dictionary<string, int[]> _sortedThresholds;
        public int MinThreshold { get; set; } = 3;

        public PaytableService() { }

        public PaytableService(Dictionary<string, Dictionary<string, string>> rawPaytable)
        {
            if (rawPaytable == null)
                throw new ArgumentNullException(nameof(rawPaytable));
            _mapping = rawPaytable.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    inner => int.Parse(inner.Key),
                    inner => int.Parse(inner.Value)
                ));
            _sortedThresholds = _mapping.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Keys.OrderByDescending(k => k).ToArray()
            );
        }

        public Dictionary<string, Dictionary<string, string>> GenerateDefaultPaytable(IEnumerable<SymbolData> symbols)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var unique = symbols.Where(s => !s.isWild && !s.isScatter).Select(s => s.symbolID).Distinct();
            foreach (var sym in unique)
            {
                result[sym] = new Dictionary<string, string>
                {
                    ["3"] = "10",
                    ["4"] = "15",
                    ["5"] = "20"
                };
            }
            return result;
        }

        public int GetReward(string symbol, int matchSize)
        {
            if (_mapping == null || !_mapping.ContainsKey(symbol) || matchSize < MinThreshold)
                return 0;
            foreach (var threshold in _sortedThresholds[symbol])
            {
                if (matchSize >= threshold && _mapping[symbol].TryGetValue(threshold, out var reward))
                    return reward;
            }
            return 0;
        }

        public string GetPaytableJson()
        {
            var back = _mapping.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    inner => inner.Key.ToString(),
                    inner => inner.Value.ToString()
                ));
            return JsonConvert.SerializeObject(back, Formatting.Indented);
        }

        public IEnumerable<string> GetAllSymbolIds()
        {
            return _mapping?.Keys ?? Enumerable.Empty<string>();
        }
    }
}