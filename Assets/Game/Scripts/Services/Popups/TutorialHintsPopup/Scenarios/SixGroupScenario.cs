using SlotsLogic.Match3Slots.Scripts.Core.Skills;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class SixGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.FirstSkillActivateButton != null)
                View.FirstSkillActivateButton.onClick.AddListener(OnFirstSkillClicked);
        }

        private void OnFirstSkillClicked()
        {
            if (View?.FirstSkillActivateButton != null)
                View.FirstSkillActivateButton.onClick.RemoveListener(OnFirstSkillClicked);

            View.GameManager?.SkillsService?.TryActivateSkill(SkillType.СateringTrolley, View.GameManager);
            
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.FirstSkillActivateButton != null)
                View.FirstSkillActivateButton.onClick.RemoveListener(OnFirstSkillClicked);
            base.Cleanup();
        }
    }
}

