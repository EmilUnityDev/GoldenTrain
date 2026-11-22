using System;
using System.Collections.Generic;
using SlotsLogic.Match3Slots.Scripts.Core;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class DataInitializationService
    {
        private readonly IDataStorage _storage;
        private readonly ICombinationService _combinationService;
        private readonly IReelLengthService _defaultReelLengthService;
        private readonly IPaytableService _defaultPaytableService;

        public DataInitializationService(
            IDataStorage storage,
            ICombinationService combinationService,
            IReelLengthService defaultReelLengthService,
            IPaytableService defaultPaytableService)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _combinationService = combinationService ?? throw new ArgumentNullException(nameof(combinationService));
            _defaultReelLengthService = defaultReelLengthService ??
                                        throw new ArgumentNullException(nameof(defaultReelLengthService));
            _defaultPaytableService =
                defaultPaytableService ?? throw new ArgumentNullException(nameof(defaultPaytableService));
        }

        public AllColumnsData Initialize(IEnumerable<SymbolData> symbols)
        {
            if (_storage.HasExistingData())
            {
                Debug.Log("Конфиг (math.json) загружен");
                var ex = _storage.Load();

                _combinationService.LoadReelSymbols(ex.reelSymbols);

                return ex;
            }

            var reelSymbols = _combinationService.GenerateReelSymbols();
            _combinationService.LoadReelSymbols(reelSymbols);

            var reelLengths = _defaultReelLengthService.GetReelLengths();

            var rawPay = _defaultPaytableService.GenerateDefaultPaytable(symbols);

            var data = new AllColumnsData
            {
                reelSymbols = new List<string>(reelSymbols),
                reelsLength = new List<int>(reelLengths),
                paytable = rawPay
            };

            _storage.Save(data);
            Debug.Log("Конфиг (math.json) создан");
            return data;
        }
    }
}