namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class SeventhGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.SkipSeventhGroupButton != null)
                View.SkipSeventhGroupButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnSkipClicked()
        {
            if (View?.SkipSeventhGroupButton != null)
                View.SkipSeventhGroupButton.onClick.RemoveListener(OnSkipClicked);

            TutorialHintsView.FireSkipSeventhHint();
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.SkipSeventhGroupButton != null)
                View.SkipSeventhGroupButton.onClick.RemoveListener(OnSkipClicked);
            base.Cleanup();
        }
    }
}