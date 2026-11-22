using UnityEngine;
using UnityEngine.SceneManagement;

public class RoadTrafficAnchor : MonoBehaviour
{
    [Header("Default")]
    [SerializeField] private RectTransform[] lanePoints;

    [Header("Tutorial")]
    [SerializeField] private RectTransform[] tutorialLanePoints;

    public RectTransform[] LanePoints => IsTutorial() && tutorialLanePoints != null && tutorialLanePoints.Length > 0
        ? tutorialLanePoints
        : lanePoints;

    private bool IsTutorial()
    {
        return SceneManager.GetActiveScene().name == "Tutorial";
    }

    private RectTransform[] GetActiveLanePoints()
    {
        if (IsTutorial() && tutorialLanePoints != null && tutorialLanePoints.Length > 0)
            return tutorialLanePoints;

        return lanePoints;
    }

    public float GetClosestLaneY(float targetY)
    {
        RectTransform[] activeLanePoints = GetActiveLanePoints();

        if (activeLanePoints == null || activeLanePoints.Length == 0)
            return ((RectTransform)transform).anchoredPosition.y;

        float bestY = activeLanePoints[0].anchoredPosition.y;
        float bestDist = Mathf.Abs(targetY - bestY);

        for (int i = 1; i < activeLanePoints.Length; i++)
        {
            float y = activeLanePoints[i].anchoredPosition.y;
            float dist = Mathf.Abs(targetY - y);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestY = y;
            }
        }

        return bestY;
    }

    public float GetRandomLaneY()
    {
        RectTransform[] activeLanePoints = GetActiveLanePoints();

        if (activeLanePoints == null || activeLanePoints.Length == 0)
            return ((RectTransform)transform).anchoredPosition.y;

        return activeLanePoints[Random.Range(0, activeLanePoints.Length)].anchoredPosition.y;
    }
}