using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Tutorial;
using SlotsLogic.Match3Slots.Scripts.Core.Skills;

namespace Game.Scripts.Services.Popups.SkillsConfirmationPopup
{
    public class SkillsConfirmationViewModel : IPopupData
    {
        public SkillType SkillType { get; }
        public ITutorialUIService TutorialUIService { get; }
        public SkillsService SkillsService { get; }

        public SkillsConfirmationViewModel(SkillType skillType, SkillsService skillsService, ITutorialUIService tutorialUIService = null)
        {
            SkillType = skillType;
            SkillsService = skillsService;
            TutorialUIService = tutorialUIService;
        }
    }
}