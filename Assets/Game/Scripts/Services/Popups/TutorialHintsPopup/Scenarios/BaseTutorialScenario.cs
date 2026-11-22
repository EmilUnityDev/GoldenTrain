using System;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public abstract class BaseTutorialScenario : ITutorialScenario
    {
        protected TutorialHintsView View { get; private set; }
        protected Action OnComplete { get; private set; }

        public virtual void Execute(TutorialHintsView view, Action onComplete)
        {
            View = view;
            OnComplete = onComplete;
            
            OnExecute();
        }
        
        protected abstract void OnExecute();

        public virtual void Cleanup()
        {
            View = null;
            OnComplete = null;
        }

        protected void InvokeComplete()
        {
            OnComplete?.Invoke();
        }
    }
}

