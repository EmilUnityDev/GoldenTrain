using Game.Scripts.GameRoot.Services.Popups;

namespace Game.Scripts.Services.Popups.MegaWinPopup
{
    public enum MegaWinType
    {
        BigWin,
        MegaWin,
        SuperWin
    }

    public class MegaWinPopupViewModel : IPopupData
    {
        public int Value { get; }

        public MegaWinPopupViewModel(int value)
        {
            Value = value;
        }
    }
}