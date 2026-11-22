using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using SpinSystem.States;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class ThirdGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.SkipThirdGroupButton != null)
                View.SkipThirdGroupButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnSkipClicked()
        {
            if (View?.SkipThirdGroupButton != null)
                View.SkipThirdGroupButton.onClick.RemoveListener(OnSkipClicked);

            TutorialHintsView.FireSkipThirdHint();
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.SkipThirdGroupButton != null)
                View.SkipThirdGroupButton.onClick.RemoveListener(OnSkipClicked);
            base.Cleanup();
        }
    }
}
