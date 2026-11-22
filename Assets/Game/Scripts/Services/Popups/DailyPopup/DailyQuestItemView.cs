using System;
using DG.Tweening;
using Game.Scripts.Services.DailyQuests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Services.Popups.DailyPopup
{
    public class DailyQuestItemView : MonoBehaviour
    {
        [Header("Quest Type")]
        [SerializeField] private DailyQuestType _questType;
        
        public DailyQuestType QuestType => _questType;

        [Header("Progress")]
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Image _progressFillImage;

        [Header("Button")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private Sprite _inProgressSprite;
        [SerializeField] private Sprite _completedSprite;
        [SerializeField] private Sprite _claimedSprite;

        [Header("Button State Texts")]
        [SerializeField] private TMP_Text _inProgressText;
        [SerializeField] private TMP_Text _completedText;
        [SerializeField] private TMP_Text _claimedText;

        [Header("Reward")]
        [SerializeField] private TMP_Text _rewardText;

        private DailyQuestProgress _quest;
        private DailyQuestService _service;

        public event Action OnClaimed;

        public void Bind(DailyQuestProgress quest, DailyQuestService service)
        {
            _quest = quest;
            _service = service;
            
            _claimButton.onClick.AddListener(OnClaimClicked);

            RefreshFull();
        }

        private void OnClaimClicked()
        {
            if (_quest.State != DailyQuestState.Completed) return;

            if (_service.TryClaimReward(_quest.Type, out _))
            {
                RefreshButtonOnly();
                OnClaimed?.Invoke();
            }
        }

        private void RefreshFull()
        {
            _progressText.text = $"{_quest.CurrentProgress}/{_quest.RequiredProgress}";

            _progressFillImage.fillAmount = 0f;
            _progressFillImage.DOFillAmount(_quest.NormalizedProgress, 0.6f).SetEase(Ease.OutCubic);

            bool showReward = _quest.State != DailyQuestState.Claimed;
            _rewardText.gameObject.SetActive(showReward);
            if (showReward)
                _rewardText.text = $"{_quest.RewardCoins}";

            ApplyButtonState(_quest.State);
        }

        private void RefreshButtonOnly()
        {
            bool showReward = _quest.State != DailyQuestState.Claimed;
            _rewardText.gameObject.SetActive(showReward);

            ApplyButtonState(_quest.State);
        }

        private void ApplyButtonState(DailyQuestState state)
        {
            _claimButton.image.sprite = state switch
            {
                DailyQuestState.InProgress => _inProgressSprite,
                DailyQuestState.Completed  => _completedSprite,
                DailyQuestState.Claimed    => _claimedSprite,
                _ => _inProgressSprite
            };

            _inProgressText.gameObject.SetActive(state == DailyQuestState.InProgress);
            _completedText.gameObject.SetActive(state == DailyQuestState.Completed);
            _claimedText.gameObject.SetActive(state == DailyQuestState.Claimed);

            _claimButton.interactable = state == DailyQuestState.Completed;
        }
    }
}
