using SymbolScripts;

namespace SlotsLogic.Match3Slots.Scripts.Core.Skills
{
    public interface ISelectableSkill : ISkill
    {
        bool HasSelection { get; }
        int RemainingCharges { get; }
        void Select(SymbolBase target);
        void Confirm();
    }
}