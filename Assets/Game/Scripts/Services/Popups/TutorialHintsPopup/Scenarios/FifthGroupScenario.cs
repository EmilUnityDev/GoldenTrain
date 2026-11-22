namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class FifthGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.ToSixGroupButton != null)
                View.ToSixGroupButton.onClick.AddListener(OnToSixGroupClicked);
        }

        private void OnToSixGroupClicked()
        {
            if (View?.ToSixGroupButton != null)
                View.ToSixGroupButton.onClick.RemoveListener(OnToSixGroupClicked);

            View.SwitchToGroup(TutorialHintGroup.SixGroup);
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.ToSixGroupButton != null)
                View.ToSixGroupButton.onClick.RemoveListener(OnToSixGroupClicked);
            base.Cleanup();
        }
    }
}

