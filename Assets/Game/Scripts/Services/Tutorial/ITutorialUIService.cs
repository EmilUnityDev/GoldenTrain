using System;
using UnityEngine;

namespace Game.Scripts.Services.Tutorial
{
    public interface ITutorialUIService
    {
        event Action OnHintsPopupShown;
        event Action OnHintsPopupClosed;
        
        void NotifyHintsPopupShown();
        void NotifyHintsPopupClosed();

        Transform HintHand { get; }
        void SetHintHand(Transform hintHand);
    }
}