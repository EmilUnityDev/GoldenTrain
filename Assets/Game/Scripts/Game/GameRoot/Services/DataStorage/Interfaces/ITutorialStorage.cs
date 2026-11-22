namespace Game.Scripts.GameRoot.Services.DataStorage.Interfaces
{
    public interface ITutorialStorage
    {
        bool IsTutorialCompleted { get; }
        bool IsSlideshowShown { get; }
        void MarkTutorialCompleted();
        void MarkSlideshowShown();
    }
}

