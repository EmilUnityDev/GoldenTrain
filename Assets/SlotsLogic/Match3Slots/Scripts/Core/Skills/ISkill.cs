namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public interface ISkill
    {
        SkillType Type { get; }
        void Activate(SkillsService service, SlotGameManager gameManager, int charges);
        bool CanActivate(SkillsService service);
        void Cancel();
    }
}