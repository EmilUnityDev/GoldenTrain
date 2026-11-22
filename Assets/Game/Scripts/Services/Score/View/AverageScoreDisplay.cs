using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Services.Score.View
{
    public class AverageScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _averageScoreText;
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
            if (_averageScoreText == null || _diffText == null) return;
            var count = PlayerPrefs.GetInt("LocalMatchCount", 0);
            var total = PlayerPrefs.GetFloat("LocalMatchScoreTotal", 0f);
            var current = _pointsService != null ? _pointsService.Score : 0f;
            var average = Mathf.RoundToInt((total + current) / (count + 1));
            var previous = PlayerPrefs.GetInt("LocalPreviousAverageScore", -1);
            _averageScoreText.text = average.ToString();
            _diffText.text = previous >= 0 && average > previous ? $"+{average - previous}" : "";
            if (_newGameobject != null) _newGameobject.SetActive(previous < 0);
            PlayerPrefs.SetInt("LocalPreviousAverageScore", average);
            PlayerPrefs.Save();
        }
    }
}
