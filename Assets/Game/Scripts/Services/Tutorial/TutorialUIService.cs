using System;
using UnityEngine;

namespace Game.Scripts.Services.Tutorial
{
    public class TutorialUIService : ITutorialUIService
    {
        public event Action OnHintsPopupShown;
        public event Action OnHintsPopupClosed;

        public Transform HintHand { get; private set; }

        public void SetHintHand(Transform hintHand) => HintHand = hintHand;

        public void NotifyHintsPopupShown() => OnHintsPopupShown?.Invoke();
        public void NotifyHintsPopupClosed() => OnHintsPopupClosed?.Invoke();
    }
}