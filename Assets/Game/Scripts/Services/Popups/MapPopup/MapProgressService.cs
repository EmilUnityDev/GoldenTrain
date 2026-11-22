using System;
using Game.Scripts.GameRoot.Services.Overlay;
using Game.Scripts.Services.Score;
using UnityEngine;
using VContainer;

public class MapProgressService
{
    private const string ProgressKey = "MapProgress";
    private const string CounterKey = "MapTravelCounter";
    private const string FirstCityRewardClaimedKey = "FirstCityRewardClaimed";

    private const int StepsToUnlockCity = 10;

    public event Action<int> OnProgressChanged;
    public event Action<int> OnCounterChanged;
    public event Action OnFirstCityReached;

    public int Progress => PlayerPrefs.GetInt(ProgressKey, 0);
    public int Counter => PlayerPrefs.GetInt(CounterKey, 0);

    private LevelProgressService _levelService;
    
    [Inject]
    public void Construct( LevelProgressService levelService )
    {
        _levelService = levelService;
    }

    public bool IsFirstCityRewardClaimed =>
        PlayerPrefs.GetInt(FirstCityRewardClaimedKey, 0) == 1;

    public void AddCounterPoint()
    {
        int counter = Counter + 1;
        _levelService.AddExperiencePercent(10);
        if (counter >= StepsToUnlockCity)
        {
            PlayerPrefs.SetInt(CounterKey, 0);
            PlayerPrefs.Save();

            UnlockNextCity();
            return;
        }

        PlayerPrefs.SetInt(CounterKey, counter);
        PlayerPrefs.Save();

        OnCounterChanged?.Invoke(counter);
    }

    private void UnlockNextCity()
    {
        int newProgress = Progress + 1;

        PlayerPrefs.SetInt(ProgressKey, newProgress);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke(newProgress);
        OnCounterChanged?.Invoke(Counter);

        OnCityReached(newProgress);
    }

    private void OnCityReached(int cityIndex)
    {
        if (cityIndex == 1)
        {
            OnFirstCityReached?.Invoke();
            return;
        }

        Debug.Log($"City reached: {cityIndex}");
    }

    public void ClaimFirstCityReward()
    {
        if (IsFirstCityRewardClaimed)
            return;

        PlayerPrefs.SetInt(FirstCityRewardClaimedKey, 1);
        PlayerPrefs.Save();

        Debug.Log("First city reward claimed");
    }

    public bool IsLocationUnlocked(int requiredProgress)
    {
        return Progress >= requiredProgress;
    }

    public void ResetProgress()
    {
        PlayerPrefs.SetInt(ProgressKey, 0);
        PlayerPrefs.SetInt(CounterKey, 0);
        PlayerPrefs.SetInt(FirstCityRewardClaimedKey, 0);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke(0);
        OnCounterChanged?.Invoke(0);
    }
}