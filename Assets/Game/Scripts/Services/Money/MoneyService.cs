using System;
using Game.Scripts.GameRoot.Services.DataStorage;

namespace Game.Scripts.Services.Money
{
    public class MoneyService
    {
        private readonly GameDataRepository _repository;

        public int Money => _repository.Data.Money;
        public event Action<int> OnMoneyChanged;

        public MoneyService(GameDataRepository repository)
        {
            _repository = repository;
        }

        public void AddMoney(int amount)
        {
            if (amount == 0) return;
            _repository.Data.Money += amount;
            _repository.Save();
            OnMoneyChanged?.Invoke(_repository.Data.Money);
        }

        public void DecreaseMoney(int amount)
        {
            _repository.Data.Money -= amount;
            if (_repository.Data.Money < 0)
                _repository.Data.Money = 0;
            _repository.Save();
            OnMoneyChanged?.Invoke(_repository.Data.Money);
        }

        public int GetMoney() => _repository.Data.Money;

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (_repository.Data.Money >= amount)
            {
                _repository.Data.Money -= amount;
                _repository.Save();
                OnMoneyChanged?.Invoke(_repository.Data.Money);
                return true;
            }
            return false;
        }

        public bool HasMoneyCount(int amount)
        {
            if (amount <= 0) return true;
            return _repository.Data.Money >= amount;
        }

        public void SetMoney(int amount)
        {
            _repository.Data.Money = amount;
            _repository.Save();
            OnMoneyChanged?.Invoke(_repository.Data.Money);
        }
    }
}