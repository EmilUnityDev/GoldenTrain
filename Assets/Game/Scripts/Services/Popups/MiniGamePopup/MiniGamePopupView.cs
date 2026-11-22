using System;
using System.Collections;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using TutorScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Services.Popups.MiniGamePopup
{
    public class MiniGamePopupView : MonoBehaviour, IPopup
    {
        private MiniGamePopupViewModel _vm;
        public event Action OnClose;

        [SerializeField] private Image _backImage;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _EndButton;
        [SerializeField] private GameObject _miniGamePanel;
        [SerializeField] private GameObject _finalHintPanel;

        private MainTutorialScreensController _tutorialController;


        private void EnsureServices()
        {
            if (_tutorialController == null)
                _tutorialController = FindObjectOfType<MainTutorialScreensController>();
        }

        public void Bind(IPopupData data)
        {
            EnsureServices();
            _vm = data as MiniGamePopupViewModel;

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);

            LocalEventLogger.SendEvent("mini_game_popup_opened");
            _skipButton.onClick.AddListener(EndTutorial);
            _EndButton.onClick.AddListener(StartEndTutorial);
        }

        public void StartEndTutorial()
        {
            EndTutorial();
        }

        private void EndTutorial()
        {
            EnsureServices();
            if (_tutorialController == null) return;
            _tutorialController.EndTutorial();
        }

        public void ShowFinalHint()
        {
            StartCoroutine(ShowFinalHintRoutine());
        }

        private IEnumerator ShowFinalHintRoutine()
        {
            CanvasGroup miniGroup = _miniGamePanel.GetComponent<CanvasGroup>();

            if (miniGroup == null)
                miniGroup = _miniGamePanel.AddComponent<CanvasGroup>();

            CanvasGroup finalGroup = _finalHintPanel.GetComponent<CanvasGroup>();

            if (finalGroup == null)
                finalGroup = _finalHintPanel.AddComponent<CanvasGroup>();

            miniGroup.blocksRaycasts = false;
            miniGroup.interactable = false;

            yield return miniGroup
                .DOFade(0f, 0.25f)
                .SetEase(Ease.InCubic)
                .WaitForCompletion();

            _miniGamePanel.SetActive(false);

            _finalHintPanel.SetActive(true);

            finalGroup.alpha = 0f;
            finalGroup.blocksRaycasts = true;
            finalGroup.interactable = true;

            _finalHintPanel.transform.localScale = Vector3.one * 0.8f;

            finalGroup.DOFade(1f, 0.3f);
            _finalHintPanel.transform
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack);
        }

        public void ClosePopup()
        {
            LocalEventLogger.SendEvent("mini_game_popup_closed");
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}