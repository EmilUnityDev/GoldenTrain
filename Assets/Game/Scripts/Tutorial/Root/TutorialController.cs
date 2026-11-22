using DG.Tweening;
using SlotsLogic.Match3Slots.Scripts.Core;
using UnityEngine;

namespace Game.Scripts.Tutorial.Root
{
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private SlotGameManager _slotGameManager;
        
        public void StartGame()
        {
            _slotGameManager.StartGame();
        }

        private void OnDestroy()
        {
            DOTween.KillAll();
            DOTween.Clear(true);
        }
    }
}