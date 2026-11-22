using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;
using System.Globalization;

namespace Game.Scripts.Services.Money.View
{
    public class MoneyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private bool _useCommaFormatting;
        [SerializeField] private bool _instantUpdate;

        private MoneyService _moneyService;
        private Tween _countTween;
        private int _currentDisplayedMoney;

        [Inject]
        public void Construct(MoneyService moneyService)
        {
            _moneyService = moneyService;
        }

        private void Start()
        {
            if (_moneyService != null)
            {
                _moneyService.OnMoneyChanged += OnMoneyChanged;
                UpdateMoneyText(_moneyService.Money);
            }
            else
            {
                ApplyText(0);
            }
        }

        private void OnMoneyChanged(int newMoney)
        {
            UpdateMoneyText(newMoney);
        }

        private void UpdateMoneyText(int newMoney)
        {
            if (_moneyText == null) return;

            if (_instantUpdate)
            {
                ApplyText(newMoney);
                return;
            }

            _countTween?.Kill();

            var start = _currentDisplayedMoney;
            var target = newMoney;

            _countTween = DOVirtual.Int(start, target, 1f, val =>
            {
                _moneyText.text = FormatMoney(val);
            }).SetEase(Ease.OutCubic).OnComplete(() => _currentDisplayedMoney = target);
        }

        private void ApplyText(int money)
        {
            if (_moneyText == null) return;
            _moneyText.text = FormatMoney(money);
            _currentDisplayedMoney = money;
        }

        private string FormatMoney(int money)
        {
            if (_useCommaFormatting)
                return money.ToString("N0", CultureInfo.InvariantCulture);
            return money.ToString();
        }

        private void OnDestroy()
        {
            if (_moneyService != null)
                _moneyService.OnMoneyChanged -= OnMoneyChanged;

            _countTween?.Kill();
        }
    }
}
