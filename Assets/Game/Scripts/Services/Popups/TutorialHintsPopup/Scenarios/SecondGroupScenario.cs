using System.Collections;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;
using UnityEngine;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class SecondGroupScenario : BaseTutorialScenario
    {
        protected override void OnExecute()
        {
            if (View.SpinSecondGroupButton != null)
                View.SpinSecondGroupButton.onClick.AddListener(OnSpinButtonClicked);
        }

        private void OnSpinButtonClicked()
        {
            if (View.SpinSecondGroupButton != null)
                View.SpinSecondGroupButton.onClick.RemoveListener(OnSpinButtonClicked);

            View.CloseWithAnimation();
            InvokeComplete();
        }

        public override void Cleanup()
        {
            if (View?.SpinSecondGroupButton != null)
                View.SpinSecondGroupButton.onClick.RemoveListener(OnSpinButtonClicked);
            base.Cleanup();
        }
    }
}
