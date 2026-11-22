using TMPro;
using UnityEngine;

public class RoadSegmentUI : MonoBehaviour
{
    [SerializeField] private TMP_Text multiplierText;

    public float Multiplier { get; private set; }

    public void SetMultiplier(float value)
    {
        Multiplier = value;

        if (multiplierText != null)
            multiplierText.text = $"x {value:0.0}";
    }
}