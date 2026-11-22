using System;
using Game.Scripts.GameRoot.Services.DataStorage.Interfaces;

namespace Game.Scripts.GameRoot.Services.DataStorage.Implementations
{
    /// <summary>
    /// Saves and loads game settings.
    /// Settings are stored locally through IDataStorage.
    /// </summary>
    public class GameSettingsStorage : IGameSettingsStorage
    {
        private readonly GameDataRepository _repository;

        public bool IsSoundOn { get; private set; }
        public bool IsMusicOn { get; private set; }
        public bool IsHapticOn { get; private set; }

        public event Action<bool> OnSoundStateChanged;
        public event Action<bool> OnMusicStateChanged;
        public event Action<bool> OnHapticStateChanged;

        public GameSettingsStorage(GameDataRepository repository)
        {
            _repository = repository;

            IsSoundOn = _repository.Data.IsSoundOn;
            IsMusicOn = _repository.Data.IsMusicOn;
            IsHapticOn = _repository.Data.IsHapticOn;

        }

        public void SetSoundState(bool isOn)
        {
            if (IsSoundOn == isOn) return;

            IsSoundOn = isOn;
            _repository.Data.IsSoundOn = isOn;
            _repository.Save();
            OnSoundStateChanged?.Invoke(isOn);
        }

        public void SetMusicState(bool isOn)
        {
            if (IsMusicOn == isOn) return;

            IsMusicOn = isOn;
            _repository.Data.IsMusicOn = isOn;
            _repository.Save();
            OnMusicStateChanged?.Invoke(isOn);
        }

        public void SetHapticState(bool isOn)
        {
            if (IsHapticOn == isOn) return;

            IsHapticOn = isOn;
            _repository.Data.IsHapticOn = isOn;
            _repository.Save();
            OnHapticStateChanged?.Invoke(isOn);
        }

        private void ChangeGameSettingsForJourney()
        {
            SetSoundState(false);
            SetMusicState(false);
            SetHapticState(false);
        }
    }
}