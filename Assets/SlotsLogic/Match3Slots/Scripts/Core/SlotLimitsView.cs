using Game.Scripts.Game.Gameplay.Root;
using TMPro;
using UnityEngine;
using VContainer;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class SlotLimitsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text slotLimitsText;
    
        private SlotLimits _slotLimits;

        [Inject]
        public void Construct(SlotLimits slotLimits)
        {
            _slotLimits = slotLimits;
        }

        private void EnsureSlotLimits()
        {
            if (_slotLimits != null) return;
            var gameManager = FindObjectOfType<SlotGameManager>();
            if (gameManager != null)
                _slotLimits = gameManager.SlotLimits;
        }

        private void Start()
        {
            EnsureSlotLimits();
            if (_slotLimits != null)
            {
                _slotLimits.OnLimitsChanged += UpdateTxt;
                UpdateTxt(_slotLimits.currentSpins);
            }
        }
    
    
        public void UpdateTxt(int limits)
        {
            if (slotLimitsText == null) return;
            slotLimitsText.text = limits.ToString();
        }

        private void OnDestroy()
        {
            if (_slotLimits != null)
            {
                _slotLimits.OnLimitsChanged -= UpdateTxt;
            }
        }
    }
}
