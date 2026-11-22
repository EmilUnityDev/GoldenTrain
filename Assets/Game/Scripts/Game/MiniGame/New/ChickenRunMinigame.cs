using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.GameRoot.Services.Feedback;
using Game.Scripts.Services.Popups.MiniGamePopup;
using Game.Scripts.Services.Score;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class ChickenRunMinigame : MonoBehaviour
{
    [Header("Links")] [SerializeField] private RectTransform chicken;
    [SerializeField] private RectTransform chickenStartPoint;
    [SerializeField] private RectTransform chickenEndPoint;

    [Header("Chicken States")] [SerializeField]
    private GameObject normalChickenObject;

    [SerializeField] private GameObject loseChickenObject;

    [Header("UI: Progress")] [SerializeField]
    private Slider progressSlider;

    [SerializeField] private TMP_Text progressPercentText;

    [Header("UI: Result Panels")] [SerializeField]
    private GameObject winPanel;

    [SerializeField] private GameObject losePanel;
    [SerializeField] private TMP_Text winMultiplierText;

    [Header("Infinite Road (spawned segments)")] [SerializeField]
    private RectTransform roadContainer;

    [SerializeField] private RectTransform initialRoadSegment;
    [SerializeField] private RectTransform roadSegmentPrefab;
    [SerializeField] private int initialSegments = 2;
    [SerializeField] private float backgroundScrollSpeed = 250f;

    [Header("Multiplier UI")] [SerializeField]
    private RectTransform multiplierContainer;

    [SerializeField] private RoadSegmentUI multiplierPrefab;

    [Header("Win")] [SerializeField] private int winPoints = 5000;

    [Header("Traffic")] [SerializeField] private TrafficSpawnerUI trafficSpawner;

    [Header("Multiplier")] [SerializeField]
    private float startMultiplier = 1f;

    [SerializeField] private float multiplierStep = 0.5f;
    [SerializeField] private float minMultiplierToWin = 2f;
    [SerializeField] private float tutorialStartMultiplier = 4f;

    [Header("Progress By Segments")] [SerializeField]
    private float progressStepPerSegment = 10f;

    [Header("Tutorial")] [SerializeField] private MiniGamePopupView tutorialView;
    [SerializeField] private CanvasGroup hintPanel;
    [SerializeField] private Button hintButton;
    [SerializeField] private GameObject skipButton;

    [Header("Run Effects")] [SerializeField]
    private GameObject runEffect1;

    [SerializeField] private GameObject runEffect2;
    [SerializeField] private Animator chickenAnimator;

    private ScoreService _scoreService;

    private readonly HashSet<RectTransform> _segmentsThatSpawnedNext = new();
    public bool IsRunning { get; private set; }

    public float Elapsed01 => Mathf.Clamp01(_currentProgressPercent / 100f);
    public float CurrentReachedMultiplier { get; private set; }

    private IFeedbackService _audioService;

    public event Action OnWin;
    public event Action OnLose;

    private bool _holding;
    private bool _finished;
    private float _elapsed;
    private float _currentProgressPercent;

    private float _segHeight;
    private readonly List<RectTransform> _segments = new();
    private readonly Stack<RectTransform> _segmentPool = new();

    private readonly Dictionary<RectTransform, RoadSegmentUI> _segmentMultiplierViews = new();
    private readonly Stack<RoadSegmentUI> _multiplierPool = new();
    private readonly HashSet<RectTransform> _passedSegments = new();

    private float _nextSegmentMultiplier;

    [Inject]
    public void Construct(IFeedbackService audioService, ScoreService scoreService)
    {
        _scoreService = scoreService;
        _audioService = audioService;
    }

    private void Awake()
    {
        if (hintButton != null)
            hintButton.onClick.AddListener(CloseHintPanel);

        if (SceneManager.GetActiveScene().name == "Tutorial")
        {
            if (hintPanel != null)
                hintPanel.gameObject.SetActive(true);
        }
        else
        {
            if (skipButton != null)
                skipButton.SetActive(false);
        }

        if (roadContainer == null)
            roadContainer = (RectTransform)transform;

        if (multiplierContainer == null)
            multiplierContainer = roadContainer;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
        }

        SetChickenNormalState();
    }

    private void OnEnable()
    {
        StartCoroutine(InitNextFrame());
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ResetGame();
    }

    private float GetStartMultiplier()
    {
        return SceneManager.GetActiveScene().name == "Tutorial"
            ? tutorialStartMultiplier
            : startMultiplier;
    }

    public void ResetGame()
    {
        _holding = false;
        IsRunning = false;
        _finished = false;
        _elapsed = 0f;
        _currentProgressPercent = 0f;

        var currentStartMultiplier = GetStartMultiplier();
        CurrentReachedMultiplier = currentStartMultiplier;
        _nextSegmentMultiplier = currentStartMultiplier;

        _passedSegments.Clear();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (winMultiplierText != null)
            winMultiplierText.text = $"x{CurrentReachedMultiplier:0}";

        if (chicken != null && chickenStartPoint != null)
        {
            var p = chicken.anchoredPosition;
            chicken.anchoredPosition = new Vector2(p.x, chickenStartPoint.anchoredPosition.y);
        }

        if (trafficSpawner != null)
            trafficSpawner.StopAndClear();

        _segmentsThatSpawnedNext.Clear();
        ResetRoad();
        UpdateProgressUI();
        SetRunObjectsActive(false);
        SetChickenNormalState();
    }

    public void HandleHoldDown()
    {
        if (_finished) return;
        if (winPanel != null && winPanel.activeSelf) return;
        if (losePanel != null && losePanel.activeSelf) return;

        if (!IsRunning)
        {
            IsRunning = true;
            _elapsed = 0f;

            if (trafficSpawner != null)
            {
                trafficSpawner.Bind(this, chicken);
                trafficSpawner.enabled = true;
            }
        }

        _holding = true;
        SetRunObjectsActive(true);
    }

    public void HandleHoldUp()
    {
        SetRunObjectsActive(false);
        if (!IsRunning || _finished) return;

        if (SceneManager.GetActiveScene().name == "Tutorial" &&
            CurrentReachedMultiplier < minMultiplierToWin)
        {
            ForceTutorialReleaseWin();
            return;
        }

        FinishByCurrentMultiplier("Button released");
    }

    private void Update()
    {
        if (!IsRunning) return;
        if (!_holding) return;
        if (_finished) return;

        var dt = Time.deltaTime;
        _elapsed += dt;

        ScrollRoadDown(dt);
        UpdateProgressUI();

        if (trafficSpawner != null && !trafficSpawner.enabled)
            trafficSpawner.enabled = true;
    }

    private void UpdateProgressUI()
    {
        var normalized = Mathf.Clamp01(_currentProgressPercent / 100f);

        if (progressSlider != null)
            progressSlider.value = normalized;

        if (progressPercentText != null)
            progressPercentText.text = $"{Mathf.RoundToInt(_currentProgressPercent)}%";
    }

    private void ResetRoad()
    {
        for (var i = _segments.Count - 1; i >= 0; i--)
        {
            if (_segments[i] == null) continue;
            if (_segments[i] == initialRoadSegment) continue;
            DespawnSegment(_segments[i]);
        }

        if (initialRoadSegment != null && _segmentMultiplierViews.ContainsKey(initialRoadSegment))
            DespawnMultiplierForSegment(initialRoadSegment);

        _segments.Clear();
        _segmentMultiplierViews.Clear();
        _passedSegments.Clear();

        if (roadContainer == null)
        {
            Debug.LogError("[MiniGame] roadContainer is null");
            return;
        }

        if (initialRoadSegment == null)
        {
            Debug.LogError("[MiniGame] initialRoadSegment is null");
            return;
        }

        initialRoadSegment.SetParent(roadContainer, false);
        initialRoadSegment.gameObject.SetActive(true);
        initialRoadSegment.anchoredPosition = Vector2.zero;

        _segHeight = initialRoadSegment.rect.height;
        if (_segHeight <= 1f)
            _segHeight = initialRoadSegment.sizeDelta.y;

        if (_segHeight <= 1f)
        {
            Debug.Log(
                $"[MiniGame] initialRoadSegment rect.height={initialRoadSegment.rect.height}, sizeDelta.y={initialRoadSegment.sizeDelta.y}");
            Debug.LogError("[MiniGame] initialRoadSegment height is invalid");
            return;
        }

        _segments.Add(initialRoadSegment);

        SetupSegmentMultiplier(initialRoadSegment, _nextSegmentMultiplier);
        _nextSegmentMultiplier += multiplierStep;

        var count = Mathf.Max(2, initialSegments);
        for (var i = 1; i < count; i++)
        {
            if (roadSegmentPrefab == null)
            {
                Debug.LogError("[MiniGame] roadSegmentPrefab is null");
                break;
            }

            var seg = SpawnSegment();
            seg.anchoredPosition = new Vector2(0f, i * _segHeight);

            SetupSegmentMultiplier(seg, _nextSegmentMultiplier);
            _nextSegmentMultiplier += multiplierStep;

            _segments.Add(seg);
        }

        _segmentsThatSpawnedNext.Clear();
    }

    private void ScrollRoadDown(float dt)
    {
        if (_segments.Count == 0) return;
        if (_segHeight <= 1f) return;

        var dy = backgroundScrollSpeed * dt;

        for (var i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            if (seg == null) continue;

            seg.anchoredPosition += new Vector2(0f, -dy);

            if (_segmentMultiplierViews.TryGetValue(seg, out var view) && view != null)
            {
                var viewRt = (RectTransform)view.transform;
                viewRt.anchoredPosition += new Vector2(0f, -dy);
            }
        }

        UpdatePassedSegments();
        CheckFinishByProgress();

        if (_finished)
            return;

        var spawnThreshold = -_segHeight;
        var despawnThreshold = -_segHeight * 1.5f;

        var bottom = GetBottomSegment();
        if (bottom == null) return;

        if (!_segmentsThatSpawnedNext.Contains(bottom) && bottom.anchoredPosition.y <= spawnThreshold)
        {
            var top = GetTopSegment();
            var newY = top != null ? top.anchoredPosition.y + _segHeight : 0f;

            var newSeg = SpawnSegment();
            newSeg.anchoredPosition = new Vector2(0f, newY);

            SetupSegmentMultiplier(newSeg, _nextSegmentMultiplier);
            _nextSegmentMultiplier += multiplierStep;

            _segments.Add(newSeg);
            _segmentsThatSpawnedNext.Add(bottom);
        }

        if (bottom.anchoredPosition.y <= despawnThreshold)
        {
            _segments.Remove(bottom);
            _passedSegments.Remove(bottom);
            _segmentsThatSpawnedNext.Remove(bottom);
            DespawnSegment(bottom);
        }
    }

    private void UpdatePassedSegments()
    {
        if (chicken == null) return;

        var chickenY = chicken.anchoredPosition.y;

        for (var i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            if (seg == null || _passedSegments.Contains(seg)) continue;

            var segmentTopY = seg.anchoredPosition.y + _segHeight * 0.5f;

            if (segmentTopY <= chickenY)
            {
                _passedSegments.Add(seg);

                var view = GetSegmentView(seg);
                if (view != null)
                {
                    CurrentReachedMultiplier = view.Multiplier;

                    _currentProgressPercent += progressStepPerSegment;
                    _currentProgressPercent = Mathf.Clamp(_currentProgressPercent, 0f, 100f);

                    Debug.Log(
                        $"[MiniGame] Reached multiplier: {CurrentReachedMultiplier:0.0}x, progress: {_currentProgressPercent}%");
                }
            }
        }
    }

    private void SetupSegmentMultiplier(RectTransform seg, float multiplier)
    {
        var view = GetOrCreateMultiplierView(seg);
        if (view == null) return;

        view.SetMultiplier(multiplier);

        var viewRt = (RectTransform)view.transform;
        viewRt.anchoredPosition = seg.anchoredPosition;
    }

    private RoadSegmentUI GetSegmentView(RectTransform seg)
    {
        if (seg == null) return null;

        if (_segmentMultiplierViews.TryGetValue(seg, out var view) && view != null)
            return view;

        return null;
    }

    private RoadSegmentUI GetOrCreateMultiplierView(RectTransform seg)
    {
        if (seg == null) return null;

        if (_segmentMultiplierViews.TryGetValue(seg, out var existing) && existing != null)
            return existing;

        if (multiplierPrefab == null)
        {
            Debug.LogError("[MiniGame] multiplierPrefab is null");
            return null;
        }

        if (multiplierContainer == null)
        {
            Debug.LogError("[MiniGame] multiplierContainer is null");
            return null;
        }

        var view = _multiplierPool.Count > 0 ? _multiplierPool.Pop() : Instantiate(multiplierPrefab);
        view.transform.SetParent(multiplierContainer, false);
        view.gameObject.SetActive(true);

        _segmentMultiplierViews[seg] = view;
        return view;
    }

    private void DespawnMultiplierForSegment(RectTransform seg)
    {
        if (seg == null) return;

        if (_segmentMultiplierViews.TryGetValue(seg, out var view) && view != null)
        {
            view.gameObject.SetActive(false);
            view.transform.SetParent(transform, false);
            _multiplierPool.Push(view);
        }

        _segmentMultiplierViews.Remove(seg);
    }

    private RectTransform GetBottomSegment()
    {
        RectTransform bottom = null;
        var minY = float.MaxValue;

        for (var i = 0; i < _segments.Count; i++)
        {
            var y = _segments[i].anchoredPosition.y;
            if (y < minY)
            {
                minY = y;
                bottom = _segments[i];
            }
        }

        return bottom;
    }

    private void SetRunObjectsActive(bool isActive)
    {
        if (runEffect1 != null) runEffect1.SetActive(isActive);
        if (runEffect2 != null) runEffect2.SetActive(isActive);

        if (chickenAnimator != null)
            chickenAnimator.SetBool("isRun", isActive);
    }

    private RectTransform GetTopSegment()
    {
        RectTransform top = null;
        var maxY = float.MinValue;

        for (var i = 0; i < _segments.Count; i++)
        {
            var y = _segments[i].anchoredPosition.y;
            if (y > maxY)
            {
                maxY = y;
                top = _segments[i];
            }
        }

        return top;
    }

    private RectTransform SpawnSegment()
    {
        var seg = _segmentPool.Count > 0 ? _segmentPool.Pop() : Instantiate(roadSegmentPrefab);
        seg.SetParent(roadContainer, false);
        seg.gameObject.SetActive(true);
        return seg;
    }

    private void DespawnSegment(RectTransform seg)
    {
        if (seg == null) return;

        DespawnMultiplierForSegment(seg);

        seg.gameObject.SetActive(false);
        seg.SetParent(transform, false);

        if (seg != initialRoadSegment)
            _segmentPool.Push(seg);
    }

    public RectTransform GetSpawnSegment()
    {
        if (_segments.Count == 0)
            return null;

        RectTransform best = null;
        var bestY = float.MinValue;

        for (var i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            if (seg == null) continue;

            if (seg.anchoredPosition.y > bestY)
            {
                bestY = seg.anchoredPosition.y;
                best = seg;
            }
        }

        return best;
    }

    public void LoseByCarHit()
    {
        if (SceneManager.GetActiveScene().name == "Tutorial")
            return;

        if (!IsRunning || _finished) return;
        FinishByCurrentMultiplier($"Hit by car at multiplier {CurrentReachedMultiplier:0.0}x");
        _audioService?.PlaySfx("carCrash");
    }

    private void ForceTutorialReleaseWin()
    {
        IsRunning = false;
        _holding = false;
        _finished = true;

        SetRunObjectsActive(false);

        if (trafficSpawner != null)
            trafficSpawner.StopAndClear();

        CurrentReachedMultiplier = Mathf.Max(CurrentReachedMultiplier, minMultiplierToWin);

        Debug.Log($"[MiniGame] Tutorial release forced WIN. Multiplier: {CurrentReachedMultiplier:0.0}x");

        Win();
    }

    private void FinishByCurrentMultiplier(string reason)
    {
        IsRunning = false;
        _holding = false;
        _finished = true;

        SetRunObjectsActive(false);

        if (trafficSpawner != null)
            trafficSpawner.StopAndClear();

        Debug.Log($"[MiniGame] Finish reason: {reason}. Current multiplier: {CurrentReachedMultiplier:0.0}x");

        if (CurrentReachedMultiplier >= minMultiplierToWin)
            Win();
        else
            Lose(reason);
    }

    private void Win()
    {
        SetChickenLoseState();

        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);

        if (winMultiplierText != null)
            winMultiplierText.text = $"x{CurrentReachedMultiplier:0}";

        OnWin?.Invoke();
        OnWinPanelShown();
    }

    private void Lose(string reason)
    {
        Debug.Log($"[MiniGame] LOSE: {reason}");

        SetChickenLoseState();

        if (losePanel != null) losePanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);

        OnLose?.Invoke();
        OnLosePanelShown();
    }

    private void SetChickenNormalState()
    {
        if (normalChickenObject != null)
            normalChickenObject.SetActive(true);

        if (loseChickenObject != null)
            loseChickenObject.SetActive(false);
    }

    private void SetChickenLoseState()
    {
        if (normalChickenObject != null)
            normalChickenObject.SetActive(false);

        if (loseChickenObject != null)
            loseChickenObject.SetActive(true);
    }

    private IEnumerator EndTutor()
    {
        yield return new WaitForSeconds(4f);
        tutorialView.StartEndTutorial();
    }

    private void OnWinPanelShown()
    {
        if (SceneManager.GetActiveScene().name == "Tutorial")
            StartCoroutine(EndTutor());
        else
            StartCoroutine(OnWinLogic());
    }

    private void OnLosePanelShown()
    {
        if (SceneManager.GetActiveScene().name == "Tutorial")
            StartCoroutine(EndTutor());
        else
            StartCoroutine(OnLoseLogic());
    }

    private IEnumerator OnWinLogic()
    {
        _scoreService.AddBonusPoints(Mathf.RoundToInt(winPoints * CurrentReachedMultiplier));
        yield return new WaitForSeconds(4f);
        tutorialView.ClosePopup();
    }

    private IEnumerator OnLoseLogic()
    {
        yield return new WaitForSeconds(4f);
        tutorialView.ClosePopup();
    }

    private void CheckFinishByProgress()
    {
        if (!IsRunning || _finished)
            return;

        if (_currentProgressPercent >= 100f)
        {
            _currentProgressPercent = 100f;
            UpdateProgressUI();
            FinishByCurrentMultiplier("Reached 100% progress");
        }
    }

    public void CloseHintPanel()
    {
        if (hintPanel == null) return;

        hintPanel.blocksRaycasts = false;
        hintPanel.interactable = false;

        hintPanel.DOFade(0f, 0.25f)
            .SetEase(Ease.InCubic);

        hintPanel.transform.DOScale(Vector3.one * 0.8f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                hintPanel.gameObject.SetActive(false);
                hintPanel.transform.localScale = Vector3.one;
            });
    }
}