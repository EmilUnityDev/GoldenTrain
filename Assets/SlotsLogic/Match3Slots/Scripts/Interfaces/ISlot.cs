using System;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface ISlot
    {
        bool Spin();
        event Action OnSpinCompleate;
    }
}