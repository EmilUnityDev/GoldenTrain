using System;
using System.Collections.Generic;
using System.Linq;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class DataGenerator : IDataGenerator
    {
        public SlotGameConfig Config { get; }
        public List<SymbolData> Symbols { get; }

        private readonly float _winChanceNormalized;
        private const int TotalCells = 500;

        private readonly float _wildWeight = 0.14f;
        private readonly float _scatterWeight = 0.2f;
        private readonly float _regularWeight = 1.0f;

        public DataGenerator(SlotGameConfig config, List<SymbolData> symbols, float winChance)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (symbols == null || symbols.Count == 0) throw new ArgumentException("Symbols list cannot be empty");
            Symbols = symbols;
            _winChanceNormalized = Mathf.Clamp01(winChance / 100f);
        }

        public List<string> GenerateColumn()
        {
            var column = new List<string>(TotalCells);

            var weightedPool = BuildWeightedPool();

            string prev = SampleWeighted(weightedPool);
            column.Add(prev);

            var repeatProb = _winChanceNormalized;
            var hotBias = Mathf.Lerp(0f, 0.6f, _winChanceNormalized);

            for (int i = 1; i < TotalCells; i++)
            {
                if (Random.value < repeatProb)
                {
                    column.Add(prev);
                    continue;
                }

                if (Random.value < hotBias)
                {
                    var hotPool = BuildHotPool(weightedPool, 1f + _winChanceNormalized * 1.5f);
                    var s = SampleWeighted(hotPool);
                    column.Add(s);
                    prev = s;
                    continue;
                }

                var next = SampleWeighted(weightedPool);
                column.Add(next);
                prev = next;
            }

            return column;
        }

        private List<(string id, float weight)> BuildWeightedPool()
        {
            var list = new List<(string id, float weight)>(Symbols.Count);
            foreach (var s in Symbols)
            {
                float w = _regularWeight;
                if (s.isWild) w = _wildWeight;
                else if (s.isScatter) w = _scatterWeight;
                list.Add((s.symbolID, w));
            }

            var total = list.Sum(x => x.weight);
            if (total <= 0f)
            {
                var fallback = new List<(string id, float weight)>();
                foreach (var s in Symbols) fallback.Add((s.symbolID, 1f));
                return fallback;
            }

            return list;
        }

        private List<(string id, float weight)> BuildHotPool(List<(string id, float weight)> basePool, float boost)
        {
            var pool = new List<(string id, float weight)>(basePool.Count);
            foreach (var item in basePool)
            {
                float w = item.weight;
                if (Mathf.Approximately(item.weight, _regularWeight))
                    w *= boost;
                pool.Add((item.id, w));
            }

            var total = pool.Sum(x => x.weight);
            if (total <= 0f) return basePool;
            return pool;
        }

        private string SampleWeighted(List<(string id, float weight)> pool)
        {
            var total = pool.Sum(x => x.weight);
            var r = Random.value * total;
            var acc = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += pool[i].weight;
                if (r <= acc) return pool[i].id;
            }

            return pool.Count > 0 ? pool[pool.Count - 1].id : null;
        }
    }
}
