using DG.Tweening;
using UnityEngine;

namespace TutorScripts
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.4f;

        private CanvasGroup _currentWindow;

        public void OpenSettings(CanvasGroup canvasGroup)
        {
            _currentWindow = canvasGroup;
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.OutSine);
        }
    

        public void StartGame()
        {
            // Твоя логика старта игры
        }
    }
}