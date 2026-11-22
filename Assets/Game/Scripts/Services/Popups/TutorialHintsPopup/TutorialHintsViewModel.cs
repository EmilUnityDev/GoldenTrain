using Game.Scripts.GameRoot.Services.Popups;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup
{
    public class TutorialHintsViewModel : IPopupData
    {
        public TutorialHintGroup GroupToShow { get; }

        public TutorialHintsViewModel(TutorialHintGroup groupToShow)
        {
            GroupToShow = groupToShow;
        }
    }
}