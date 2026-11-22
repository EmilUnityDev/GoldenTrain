using System;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public interface ITutorialScenario
    {
        void Execute(TutorialHintsView view, Action onComplete);
        void Cleanup();
    }
}