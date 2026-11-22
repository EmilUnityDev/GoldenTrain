using System;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class SlotLimits
    {

        public int currentSpins;
        public event Action<int> OnLimitsChanged;

        public SlotLimits(int spinCount)
        {
            currentSpins = spinCount;
        }
        
        public void AddSpin(int count)
        {
            currentSpins += count;
            OnLimitsChanged?.Invoke(currentSpins);
        }

        public bool EndGame()
        {
            if (currentSpins <= 0)
                return false;
            else
            {
                return true;
            }
        }
        public void RemoveSpin()
        {
            currentSpins--;
            OnLimitsChanged?.Invoke(currentSpins);
        }
    }
}
