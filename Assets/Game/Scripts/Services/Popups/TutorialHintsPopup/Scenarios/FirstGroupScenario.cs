using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class FirstGroupScenario : BaseTutorialScenario
    {
        private const float BgFadeDuration = 0.4f;

        private void OnNextGroupButtonClicked()
        {
            if (View.NextGroupFirstButton != null)
                View.NextGroupFirstButton.onClick.RemoveListener(OnNextGroupButtonClicked);

            View.SwitchToGroup(TutorialHintGroup.SecondGroup);
        }

        protected override void OnExecute()
        {
            View.StartCoroutine(ExecuteScenario());
        }

        private IEnumerator ExecuteScenario()
        {
            var bgImage = View.BGImage;
            if (bgImage != null)
            {
                var color = bgImage.color;
                color.a = 0f;
                bgImage.color = color;
                bgImage.DOFade(221f / 255f, BgFadeDuration).SetEase(Ease.OutQuad);
            }

            if (View.NextGroupFirstButton != null)
                View.NextGroupFirstButton.onClick.AddListener(OnNextGroupButtonClicked);

            yield break;
        }

        public override void Cleanup()
        {
            if (View?.NextGroupFirstButton != null)
                View.NextGroupFirstButton.onClick.RemoveListener(OnNextGroupButtonClicked);
            base.Cleanup();
        }
    }
}
