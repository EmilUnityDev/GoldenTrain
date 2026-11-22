using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.DataStorage;
using Game.Scripts.Services.Money;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using DG.Tweening;

namespace Game.Scripts.Services.Popups.TrainPopup {
    public class PanelUnlockManager : MonoBehaviour {
        [System.Serializable]
        public class PanelData {
            public GameObject panel;
            public string TrainTitle;
            [Header("Lock UI")]
            public Sprite lockObject;
            public Sprite unlockObject;
            public Image trainImage;

            [Header("Buy UI")]
            public Button buyButton;
            public TMP_Text priceText;
            public int unlockPrice;

            [HideInInspector] public Vector3 originalLocalPosition;
            [HideInInspector] public Vector3 originalTrainImageLocalPosition;
        }

        [SerializeField] private List<PanelData> panels = new();

        [Header("Navigation")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [SerializeField] private TMP_Text _currentTrainTitleText;
        [SerializeField] private GameObject _titleRoot;

        [Header("Animation Settings")]
        [SerializeField] private float animDuration = 0.15f;
        [SerializeField] private float shakeOffset = 30f;

        private MoneyService _moneyService;
        private GameDataRepository _repository;
        private int currentPanelIndex = 0;

        [Inject]
        public void Construct(MoneyService moneyService, GameDataRepository repository) {
            _moneyService = moneyService;
            _repository = repository;
        }

        private int UnlockedIndex {
            get => _repository.Data.UnlockedTrainIndex;
            set { _repository.Data.UnlockedTrainIndex = value; }
        }

        private int AvailableToBuyIndex {
            get {
                int next = UnlockedIndex + 1;
                return next < panels.Count ? next : -1;
            }
        }

        private void Start() {
            currentPanelIndex = _repository.Data.SelectedTrainIndex;

            leftButton.onClick.AddListener(OpenPreviousPanel);
            rightButton.onClick.AddListener(OpenNextPanel);

            for (int i = 0; i < panels.Count; i++) {
                int index = i;

                
                if (panels[i].panel != null) {
                    panels[i].originalLocalPosition = panels[i].panel.transform.localPosition;
                }

                if (panels[i].trainImage != null) {
                    panels[i].originalTrainImageLocalPosition = panels[i].trainImage.rectTransform.localPosition;
                }

                if (panels[i].buyButton != null)
                    panels[i].buyButton.onClick.AddListener(() => BuyPanel(index));
                if (panels[i].priceText != null)
                    panels[i].priceText.text = panels[i].unlockPrice.ToString();
            }

            OpenPanel(currentPanelIndex);
            UpdateTitleText();

            if (_moneyService != null)
                _moneyService.OnMoneyChanged += OnMoneyChanged;
        }

        private void OnMoneyChanged(int newMoney) {
            UpdateBuyButtonInteractable();
        }

        private void UpdateBuyButtonInteractable() {
            int buyIdx = AvailableToBuyIndex;
            if (buyIdx < 0) return;

            var panelData = panels[buyIdx];
            if (panelData.buyButton != null)
                panelData.buyButton.interactable = _moneyService.Money >= panelData.unlockPrice;
        }

        private void OpenNextPanel() {
            if (currentPanelIndex < panels.Count - 1) {
                currentPanelIndex++;
                OpenPanel(currentPanelIndex);
                UpdateTitleText();
            }
        }

        private void OpenPreviousPanel() {
            if (currentPanelIndex > 0) {
                currentPanelIndex--;
                OpenPanel(currentPanelIndex);
                UpdateTitleText();
            }
        }

        private void UpdateTitleText() {
            if (_currentTrainTitleText != null && currentPanelIndex < panels.Count)
                _currentTrainTitleText.text = panels[currentPanelIndex].TrainTitle;
        }

        private void OpenPanel(int index) {
            int unlockedIdx = UnlockedIndex;
            int buyIdx = AvailableToBuyIndex;

            for (int i = 0; i < panels.Count; i++) {
                var panelData = panels[i];
                bool isCurrent = i == index;
                bool isUnlocked = i <= unlockedIdx;
                bool canBuy = buyIdx >= 0 && i == buyIdx;

                
                panelData.panel.transform.DOKill();
                panelData.panel.transform.localPosition = panelData.originalLocalPosition;

                panelData.panel.SetActive(isCurrent);

                if (panelData.lockObject != null)
                    panelData.trainImage.sprite = isUnlocked
                        ? panelData.unlockObject
                        : panelData.lockObject;

                if (panelData.buyButton != null) {
                    panelData.buyButton.gameObject.SetActive(isCurrent && canBuy);
                    if (isCurrent && canBuy)
                        panelData.buyButton.interactable = _moneyService.Money >= panelData.unlockPrice;
                }

                
                if (isCurrent && isUnlocked) {
                    AnimateTrainPanel(panelData);
                }
            }

            bool currentIsUnlocked = index <= unlockedIdx;
            if (_titleRoot != null)
                _titleRoot.SetActive(currentIsUnlocked);

            if (currentIsUnlocked) {
                _repository.Data.SelectedTrainIndex = index;
                _repository.Save();
            }

            UpdateNavigationButtons();
        }

        private void AnimateTrainPanel(PanelData panelData) {
            Transform panelTransform = panelData.panel.transform;
            float startX = panelData.originalLocalPosition.x;

            Sequence shakeSequence = DOTween.Sequence();

            shakeSequence

                .Append(panelTransform.DOLocalMoveX(startX - shakeOffset, animDuration).SetEase(Ease.OutQuad))

                .Append(panelTransform.DOLocalMoveX(startX + (shakeOffset * 0.7f), animDuration * 1.2f).SetEase(Ease.InOutQuad))

                .Append(panelTransform.DOLocalMoveX(startX - (shakeOffset * 0.3f), animDuration).SetEase(Ease.InOutQuad))

                .Append(panelTransform.DOLocalMoveX(startX, animDuration).SetEase(Ease.OutQuad))

                .SetLink(panelData.panel);
        }

        private void BuyPanel(int index) {
            if (index != AvailableToBuyIndex)
                return;

            int price = panels[index].unlockPrice;
            if (!_moneyService.TrySpend(price)) {
                Debug.Log("Not enough money");
                return;
            }

            UnlockedIndex = index;

            _repository.Data.SelectedTrainIndex = index;
            _repository.Save();

            OpenPanel(currentPanelIndex);
            UpdateBuyButtonInteractable();

            AnimateTrainImageSlideIn(panels[index]);
        }

        private void AnimateTrainImageSlideIn(PanelData panelData) {
            if (panelData.trainImage == null) return;

            var rt = panelData.trainImage.rectTransform;
            var targetPos = panelData.originalTrainImageLocalPosition;

            rt.DOKill();
            rt.localPosition = new Vector3(targetPos.x - 1200f, targetPos.y, targetPos.z);
            rt.DOLocalMoveX(targetPos.x, 0.5f).SetEase(Ease.OutCubic);
        }

        private void UpdateNavigationButtons() {
            leftButton.interactable = currentPanelIndex > 0;
            rightButton.interactable = currentPanelIndex < panels.Count - 1;
        }

        public void SaveSelectedTrain() {
            if (currentPanelIndex <= UnlockedIndex) {
                _repository.Data.SelectedTrainIndex = currentPanelIndex;
                _repository.Save();
            }
        }

        private void OnDestroy() {
            if (_moneyService != null)
                _moneyService.OnMoneyChanged -= OnMoneyChanged;

            foreach (var panelData in panels) {
                if (panelData.panel != null) {
                    panelData.panel.transform.DOKill();
                }
                if (panelData.trainImage != null) {
                    panelData.trainImage.rectTransform.DOKill();
                }
            }
        }
    }
}