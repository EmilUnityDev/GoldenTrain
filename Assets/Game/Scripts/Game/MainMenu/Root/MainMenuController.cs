using System.Collections;
using Game.Scripts.GameRoot.Services.Feedback;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Game.MainMenu.Root
{
    public class MainMenuController : MonoBehaviour
    {
        private IFeedbackService _audioService;

        [Inject]
        public void Construct(IFeedbackService audioService)
        {
            _audioService = audioService;
        }

        private IEnumerator Start()
        {
            AudioListener.pause = false;
            yield return new WaitForSeconds(3f);
            AudioListener.pause = false;
            _audioService.PlayMusic("BgMusic");
        }
    }
}