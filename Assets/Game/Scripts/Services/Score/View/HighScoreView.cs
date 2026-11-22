using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Services.Score.View
{
    public class HighScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _highScoreText;
        [SerializeField] private TextMeshProUGUI _diffText;
        [SerializeField] private GameObject _newGameobject;
        private ScoreService _pointsService;

        [Inject]
        public void Construct(ScoreService scoreService)
        {
            _pointsService = scoreService;
        }

        private void Start()
        {
            if (_highScoreText == null || _diffText == null) return;
            var best = Mathf.Max(PlayerPrefs.GetInt("HighScore", 0),
                _pointsService != null ? _pointsService.Score : 0);
            var previous = PlayerPrefs.GetInt("LocalPreviousHighScore", -1);
            _highScoreText.text = best.ToString();
            _diffText.text = previous >= 0 && best > previous ? $"+{best - previous}" : "";
            if (_newGameobject != null) _newGameobject.SetActive(previous < 0);
            PlayerPrefs.SetInt("LocalPreviousHighScore", best);
            PlayerPrefs.Save();
        }
    }
}
