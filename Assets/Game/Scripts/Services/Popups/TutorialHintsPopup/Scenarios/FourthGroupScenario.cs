using SlotsLogic.Match3Slots.Scripts.Core.Skills;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class FourthGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.SkipFourthGroupButton != null)
                View.SkipFourthGroupButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnSkipClicked()
        {
            if (View?.SkipFourthGroupButton != null)
                View.SkipFourthGroupButton.onClick.RemoveListener(OnSkipClicked);

            TutorialHintsView.FireSkipFourthHint();
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.SkipFourthGroupButton != null)
                View.SkipFourthGroupButton.onClick.RemoveListener(OnSkipClicked);
            base.Cleanup();
        }
    }
}
