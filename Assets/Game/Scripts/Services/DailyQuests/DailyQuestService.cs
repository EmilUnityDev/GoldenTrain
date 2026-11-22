using System;
using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.DataStorage;
using Game.Scripts.Services.Money;
using UnityEngine;

namespace Game.Scripts.Services.DailyQuests
{
    public class DailyQuestService
    {
        private readonly GameDataRepository _repository;
        private readonly MoneyService _moneyService;

        public event Action OnQuestsUpdated;

        public IReadOnlyList<DailyQuestProgress> Quests => _repository.Data.DailyQuests;

        public DailyQuestService(GameDataRepository repository, MoneyService moneyService)
        {
            _repository = repository;
            _moneyService = moneyService;
            CheckAndResetIfNewDay();
        }

        private void CheckAndResetIfNewDay()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_repository.Data.DailyQuestDate != today)
            {
                _repository.Data.DailyQuestDate = today;
                _repository.Data.DailyQuests = CreateDefaultQuests();
                _repository.Save();
                Debug.Log("[DailyQuestService] New day detected. Quests reset.");
            }
            else if (_repository.Data.DailyQuests == null || _repository.Data.DailyQuests.Count == 0)
            {
                _repository.Data.DailyQuests = CreateDefaultQuests();
                _repository.Save();
            }
        }

        private List<DailyQuestProgress> CreateDefaultQuests()
        {
            return new List<DailyQuestProgress>
            {
                new DailyQuestProgress(DailyQuestType.CollectClusters,  10,  100),
                new DailyQuestProgress(DailyQuestType.LaunchCascades,   15,  100),
                new DailyQuestProgress(DailyQuestType.CollectLowPay,    20,  100),
                new DailyQuestProgress(DailyQuestType.CollectMidPay,    10,  100),
                new DailyQuestProgress(DailyQuestType.LaunchBonusGame,   1, 100),
                new DailyQuestProgress(DailyQuestType.MakeSpins,        30,  100),
                new DailyQuestProgress(DailyQuestType.WinCoins,       50, 100),
            };
        }

        public void AddProgress(DailyQuestType type, int amount = 1)
        {
            var quest = GetQuest(type);
            if (quest == null || quest.State != DailyQuestState.InProgress)
                return;

            quest.CurrentProgress = Mathf.Min(quest.CurrentProgress + amount, quest.RequiredProgress);

            if (quest.CurrentProgress >= quest.RequiredProgress)
                quest.State = DailyQuestState.Completed;

            _repository.Save();
            OnQuestsUpdated?.Invoke();
        }

        public bool TryClaimReward(DailyQuestType type, out int rewardCoins)
        {
            rewardCoins = 0;
            var quest = GetQuest(type);
            if (quest == null || quest.State != DailyQuestState.Completed)
                return false;

            rewardCoins = quest.RewardCoins;
            quest.State = DailyQuestState.Claimed;
            _moneyService.AddMoney(rewardCoins);
            _repository.Save();
            OnQuestsUpdated?.Invoke();
            return true;
        }

        public DailyQuestProgress GetQuest(DailyQuestType type)
        {
            foreach (var q in _repository.Data.DailyQuests)
                if (q.Type == type) return q;
            return null;
        }

        public DailyQuestState GetState(DailyQuestType type)
        {
            return GetQuest(type)?.State ?? DailyQuestState.InProgress;
        }

        public int GetProgress(DailyQuestType type)
        {
            return GetQuest(type)?.CurrentProgress ?? 0;
        }

        public int GetRequired(DailyQuestType type)
        {
            return GetQuest(type)?.RequiredProgress ?? 0;
        }
    }
}
