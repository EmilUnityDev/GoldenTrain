using System.Collections;
using UnityEngine;

public class MapTrainController : MonoBehaviour
{
    [SerializeField] private RectTransform train;
    [SerializeField] private RectTransform[] pathPoints;
    [SerializeField] private int[] stopPointIndexes;
    [SerializeField] private float moveSpeed = 500f;

    [Header("Rotation")]
    [SerializeField] private bool rotateByDirection = true;
    [SerializeField] private float rotationOffset = 0f;
    [SerializeField] private float rotationSmooth = 10f;

    private const float StepsToUnlockCity = 10f;
    private Coroutine _moveRoutine;

    public void MoveToProgressWithCounter(int progress, int counter, bool animate = true)
    {
        if (train == null || pathPoints == null || pathPoints.Length == 0)
            return;

        if (stopPointIndexes == null || stopPointIndexes.Length == 0)
            return;

        int currentStopIndex = Mathf.Clamp(progress, 0, stopPointIndexes.Length - 1);
        int nextStopIndex = Mathf.Clamp(progress + 1, 0, stopPointIndexes.Length - 1);

        int fromPathIndex = Mathf.Clamp(stopPointIndexes[currentStopIndex], 0, pathPoints.Length - 1);
        int toPathIndex = Mathf.Clamp(stopPointIndexes[nextStopIndex], 0, pathPoints.Length - 1);

        float progressPercent = Mathf.Clamp01(counter / StepsToUnlockCity);

        Vector3 targetPosition = GetPositionBetweenPathIndexes(fromPathIndex, toPathIndex, progressPercent);

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        if (!animate)
        {
            Vector3 previous = train.localPosition;
            train.localPosition = targetPosition;

            if (rotateByDirection)
                RotateTrain(previous, targetPosition);

            return;
        }

        _moveRoutine = StartCoroutine(MoveToPosition(targetPosition));
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(train.localPosition, targetPosition) > 1f)
        {
            Vector3 previousPosition = train.localPosition;

            train.localPosition = Vector3.MoveTowards(
                train.localPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (rotateByDirection)
                RotateTrain(previousPosition, train.localPosition);

            yield return null;
        }

        train.localPosition = targetPosition;
    }

    private Vector3 GetPositionBetweenPathIndexes(int fromIndex, int toIndex, float percent)
    {
        if (fromIndex == toIndex)
            return pathPoints[fromIndex].localPosition;

        float pathValue = Mathf.Lerp(fromIndex, toIndex, percent);

        int index = Mathf.FloorToInt(pathValue);
        int nextIndex = Mathf.Clamp(index + 1, 0, pathPoints.Length - 1);

        float localT = pathValue - index;

        Vector3 p0 = pathPoints[Mathf.Max(index - 1, 0)].localPosition;
        Vector3 p1 = pathPoints[index].localPosition;
        Vector3 p2 = pathPoints[nextIndex].localPosition;
        Vector3 p3 = pathPoints[Mathf.Min(index + 2, pathPoints.Length - 1)].localPosition;

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    private void RotateTrain(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;

        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        train.localRotation = Quaternion.Lerp(
            train.localRotation,
            targetRotation,
            Time.deltaTime * rotationSmooth
        );
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}