using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.Feedback;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class TrafficSpawnerUI : MonoBehaviour
{
    [Header("Links")] [SerializeField] private RectTransform road;
    [SerializeField] private CarUI carPrefab;
    [SerializeField] private Sprite[] carSprites;

    [Header("Spawn")] [SerializeField] private float spawnIntervalStart = 0.9f;
    [SerializeField] private float spawnIntervalEnd = 0.25f;

    [Header("Speed")] [SerializeField] private float speedStart = 450f;
    [SerializeField] private float speedEnd = 900f;

    [Header("Bounds")] [SerializeField] private float spawnOutsidePaddingPx = 120f;
    [SerializeField] private float despawnOutsidePaddingPx = 220f;

    [Header("Road Scroll (match background)")] [SerializeField]
    private float roadScrollSpeed = 250f;

    [Header("Collision")] [SerializeField] private float hitRadiusPx = 55f;

    [Header("Pool")] [SerializeField] private int prewarm = 12;

    [Header("Forced Lose Spawn")] [SerializeField]
    private bool useForcedLoseSpawn = true;

    [SerializeField] private RectTransform forcedLoseSpawnPoint;
    [SerializeField] private int forcedCheckMinAfterSpawns = 2;
    [SerializeField] private int forcedCheckMaxAfterSpawns = 3;

    [Range(0f, 1f)] [SerializeField] private float forcedLoseSpawnChance = 0.6f;

    [SerializeField] private bool forceSpawnOnChickenLane = true;
    [SerializeField] private float fallbackForcedSpawnY;
    [SerializeField] private bool forcedSpawnFromRightToLeft = true;

    private ChickenRunMinigame _game;
    private RectTransform _chicken;
    private IFeedbackService _audioService;
    private bool _isTutorial;

    private float _spawnTimer;
    private float _halfWidth;
    private bool _bound;

    private int _spawnsSinceLastForcedCheck;
    private int _nextForcedCheckAfter;

    private readonly List<CarUI> _active = new();
    private readonly Stack<CarUI> _pool = new();

    [Inject]
    public void Construct(IFeedbackService audioService)
    {
        _audioService = audioService;
    }

    private void Awake()
    {
        if (road == null)
            road = (RectTransform)transform;

        _halfWidth = road.rect.width * 0.5f;
        ResetForcedSpawnCounter();
    }

    public void Bind(ChickenRunMinigame game, RectTransform chicken)
    {
        _game = game;
        _chicken = chicken;
        _bound = true;
        _isTutorial = SceneManager.GetActiveScene().name == "Tutorial";
        Prewarm();
    }

    private void Update()
    {
        if (!_bound || _game == null || !_game.IsRunning) return;
        if (carPrefab == null) return;
        if (carSprites == null || carSprites.Length == 0) return;

        var t01 = _game.Elapsed01;
        var interval = Mathf.Lerp(spawnIntervalStart, spawnIntervalEnd, t01);
        var speed = Mathf.Lerp(speedStart, speedEnd, t01);

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= interval)
        {
            _spawnTimer = 0f;

            if (ShouldSpawnForcedLoseCar())
                SpawnForcedLoseCar(speed);
            else
                SpawnOne(speed);
        }

        MoveActiveCarsDown(Time.deltaTime);
    }

    private bool ShouldSpawnForcedLoseCar()
    {
        if (!useForcedLoseSpawn || _chicken == null)
            return false;

        _spawnsSinceLastForcedCheck++;

        if (_spawnsSinceLastForcedCheck < _nextForcedCheckAfter)
            return false;

        var shouldForce = Random.value <= forcedLoseSpawnChance;
        ResetForcedSpawnCounter();

        return shouldForce;
    }

    private void ResetForcedSpawnCounter()
    {
        _spawnsSinceLastForcedCheck = 0;
        _nextForcedCheckAfter = Random.Range(
            Mathf.Max(1, forcedCheckMinAfterSpawns),
            Mathf.Max(forcedCheckMinAfterSpawns, forcedCheckMaxAfterSpawns) + 1
        );
    }

    private void SpawnOne(float speed)
    {
        if (!TryGetSpawnLaneY(out var y))
            return;

        var dir = Random.value > 0.5f ? 1f : -1f;

        var startX = dir > 0f
            ? -_halfWidth - spawnOutsidePaddingPx
            : _halfWidth + spawnOutsidePaddingPx;

        var killX = _halfWidth + despawnOutsidePaddingPx;

        SpawnCarInternal(startX, y, dir, speed, killX);
    }

    private void SpawnForcedLoseCar(float speed)
    {
        var dir = forcedSpawnFromRightToLeft ? -1f : 1f;

        float y;
        if (forceSpawnOnChickenLane && _chicken != null)
        {
            if (!TryGetClosestLaneYToChicken(out y))
                y = fallbackForcedSpawnY;
        }
        else
        {
            if (!TryGetSpawnLaneY(out y))
                y = fallbackForcedSpawnY;
        }

        float startX;
        if (forcedLoseSpawnPoint != null)
            startX = forcedLoseSpawnPoint.anchoredPosition.x;
        else
            startX = dir > 0f
                ? -_halfWidth - spawnOutsidePaddingPx
                : _halfWidth + spawnOutsidePaddingPx;

        var killX = _halfWidth + despawnOutsidePaddingPx;

        SpawnCarInternal(startX, y, dir, speed, killX);
    }

    private bool TryGetSpawnLaneY(out float y)
    {
        y = 0f;

        var segment = _game.GetSpawnSegment();
        if (segment == null)
            return false;

        var anchor = segment.GetComponent<RoadTrafficAnchor>();
        if (anchor == null || anchor.LanePoints == null || anchor.LanePoints.Length == 0)
            return false;

        if (_isTutorial && _chicken != null)
        {
            List<RectTransform> validPoints = new();

            for (var i = 0; i < anchor.LanePoints.Length; i++)
            {
                var point = anchor.LanePoints[i];
                if (point == null) continue;

                var pointY = GetLanePointYInRoadSpace(point);

                if (pointY > _chicken.anchoredPosition.y)
                    validPoints.Add(point);
            }

            if (validPoints.Count > 0)
            {
                var lanePoint = validPoints[Random.Range(0, validPoints.Count)];
                y = GetLanePointYInRoadSpace(lanePoint);
                return true;
            }

            RectTransform topPoint = null;
            var bestY = float.MinValue;

            for (var i = 0; i < anchor.LanePoints.Length; i++)
            {
                var point = anchor.LanePoints[i];
                if (point == null) continue;

                var pointY = GetLanePointYInRoadSpace(point);
                if (pointY > bestY)
                {
                    bestY = pointY;
                    topPoint = point;
                }
            }

            if (topPoint != null)
            {
                y = GetLanePointYInRoadSpace(topPoint);
                return true;
            }

            return false;
        }

        var randomPoint = anchor.LanePoints[Random.Range(0, anchor.LanePoints.Length)];
        if (randomPoint == null)
            return false;

        y = GetLanePointYInRoadSpace(randomPoint);
        return true;
    }

    private bool TryGetClosestLaneYToChicken(out float y)
    {
        y = 0f;

        var segment = _game.GetSpawnSegment();
        if (segment == null)
            return false;

        var anchor = segment.GetComponent<RoadTrafficAnchor>();
        if (anchor == null || anchor.LanePoints == null || anchor.LanePoints.Length == 0)
            return false;

        RectTransform bestPoint = null;
        var bestDist = float.MaxValue;

        for (var i = 0; i < anchor.LanePoints.Length; i++)
        {
            var point = anchor.LanePoints[i];
            if (point == null) continue;

            var pointY = GetLanePointYInRoadSpace(point);
            var dist = Mathf.Abs(_chicken.anchoredPosition.y - pointY);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = point;
            }
        }

        if (bestPoint == null)
            return false;

        y = GetLanePointYInRoadSpace(bestPoint);
        return true;
    }

    private float GetLanePointYInRoadSpace(RectTransform lanePoint)
    {
        var worldPos = lanePoint.position;
        var localPos = road.InverseTransformPoint(worldPos);
        return localPos.y;
    }

    private void SpawnCarInternal(float startX, float y, float dir, float speed, float killX)
    {
        var car = GetCarInstance();
        car.SetSprite(carSprites[Random.Range(0, carSprites.Length)]);

        var rt = (RectTransform)car.transform;
        rt.SetParent(road, false);
        rt.anchoredPosition = new Vector2(startX, y);

        var s = rt.localScale;
        s.x = Mathf.Abs(s.x) * (dir > 0f ? 1f : -1f);
        rt.localScale = s;

        car.OnDespawn = ReturnToPool;

        if (_isTutorial)
            car.OnHitChicken = null;
        else
            car.OnHitChicken = () => _game?.LoseByCarHit();

        car.Init(_chicken, speed, dir, killX, hitRadiusPx, _isTutorial);

        _active.Add(car);
        car.gameObject.SetActive(true);
        _audioService?.PlaySfx("carpassby");
    }

    private void MoveActiveCarsDown(float dt)
    {
        if (road == null) return;

        var dy = roadScrollSpeed * dt;
        var bottomBound = -road.rect.height * 0.5f;

        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var car = _active[i];
            if (car == null) continue;

            var rt = (RectTransform)car.transform;
            rt.anchoredPosition += new Vector2(0f, -dy);

            var halfCarHeight = rt.rect.height * 0.5f;
            var killY = bottomBound - halfCarHeight - despawnOutsidePaddingPx;

            if (rt.anchoredPosition.y < killY)
                ReturnToPool(car);
        }
    }

    public void StopAndClear()
    {
        enabled = false;
        _spawnTimer = 0f;
        ResetForcedSpawnCounter();

        for (var i = _active.Count - 1; i >= 0; i--)
            ReturnToPool(_active[i]);

        _active.Clear();
    }

    private void ReturnToPool(CarUI car)
    {
        if (car == null) return;

        car.gameObject.SetActive(false);
        car.transform.SetParent(transform, false);
        _pool.Push(car);
        _active.Remove(car);
    }

    private void Prewarm()
    {
        var need = Mathf.Max(0, prewarm - _pool.Count);
        for (var i = 0; i < need; i++)
        {
            var car = Instantiate(carPrefab, transform);
            car.gameObject.SetActive(false);
            _pool.Push(car);
        }
    }

    private CarUI GetCarInstance()
    {
        if (_pool.Count > 0)
            return _pool.Pop();

        var car = Instantiate(carPrefab, transform);
        car.gameObject.SetActive(false);
        return car;
    }
}