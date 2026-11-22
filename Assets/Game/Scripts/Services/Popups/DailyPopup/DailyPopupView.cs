using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.GameRoot.Services.Popups;
using Game.Scripts.Services.Analytics;
using Game.Scripts.Services.DailyQuests;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Scripts.Services.Popups.DailyPopup
{
    public class DailyPopupView : MonoBehaviour, IPopup
    {
        private DailyPopupData _data;
        public event Action OnClose;

        private IFeedbackService _audioService;
        private DailyQuestService _dailyQuestService;

        [SerializeField] private Button _closePopupButton;
        [SerializeField] private Image _backImage;

        [Header("Quests")]
        [SerializeField] private List<DailyQuestItemView> _questItems;

        [Inject]
        public void Construct(IFeedbackService audioService, DailyQuestService dailyQuestService)
        {
            _audioService = audioService;
            _dailyQuestService = dailyQuestService;
        }

        public void Bind(IPopupData data)
        {
            _data = data as DailyPopupData;

            PopupAnimationHandler.PlaySpawnAnimation(gameObject);
            LocalEventLogger.SendEvent("daily_popup_opened");

            _closePopupButton.onClick.AddListener(ClosePopup);

            BindQuests();
        }

        private void BindQuests()
        {
            foreach (var questItem in _questItems)
            {
                var quest = _dailyQuestService.GetQuest(questItem.QuestType);
                if (quest != null)
                    questItem.Bind(quest, _dailyQuestService);
            }
        }

        public void ClosePopup()
        {
            LocalEventLogger.SendEvent("daily_popup_closed");
            OnClose?.Invoke();
            if (_backImage != null)
                _backImage.DOFade(0f, 0.1f)
                    .OnComplete(() => Destroy(gameObject));
            else
                Destroy(gameObject);
        }
    }
}