using Game.Scripts.GameRoot.Services.DataStorage.Interfaces;

namespace Game.Scripts.GameRoot.Services.DataStorage.Implementations
{
    public sealed class TutorialStorage : ITutorialStorage
    {
        private readonly GameDataRepository _repository;

        public bool IsTutorialCompleted => _repository.Data.IsTutorialCompleted;
        public bool IsSlideshowShown => _repository.Data.IsSlideshowShown;

        public TutorialStorage(GameDataRepository repository)
        {
            _repository = repository;
        }

        public void MarkTutorialCompleted()
        {
            if (_repository.Data.IsTutorialCompleted) return;

            _repository.Data.IsTutorialCompleted = true;
            _repository.Save();
        }

        public void MarkSlideshowShown()
        {
            if (_repository.Data.IsSlideshowShown) return;

            _repository.Data.IsSlideshowShown = true;
            _repository.Save();
        }
    }
}
