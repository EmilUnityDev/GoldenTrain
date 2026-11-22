using System;
using System.Collections.Generic;
using UnityEngine;

public class LuxuryWagonsBonusGame
{
    public const int TotalWagons = 6;
    public const int MaxOpenedWagons = 4;

    public int CurrentWagonIndex { get; private set; }
    public int OpenedWagonsCount { get; private set; }
    public int TotalMultiplier { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFinished { get; private set; }

    public IReadOnlyList<WagonContent> Wagons => _wagons;

    public event Action<int> OnWagonChanged;
    public event Action<int, WagonContent> OnWagonOpened;
    public event Action<int> OnMultiplierChanged;
    public event Action<int> OnBonusFinished;

    private readonly List<WagonContent> _wagons = new();
    private readonly System.Random _random = new();
    public int BaseReward { get; private set; }

    private bool _isTutorial;
    private int _tutorialClickCount;

    public void StartBonus(int baseReward, IReadOnlyList<Sprite> wagonSprites)
    {
        BaseReward = baseReward;

        _wagons.Clear();

        CurrentWagonIndex = 0;
        OpenedWagonsCount = 0;
        TotalMultiplier = 0;
        IsActive = true;
        IsFinished = false;
        _isTutorial = false;
        _tutorialClickCount = 0;
        GenerateWagons(wagonSprites);

        OnWagonChanged?.Invoke(CurrentWagonIndex);
        OnMultiplierChanged?.Invoke(TotalMultiplier);
    }

    public void NextWagon()
    {
        if (!CanChangeWagon())
            return;

        CurrentWagonIndex++;

        if (CurrentWagonIndex >= TotalWagons)
            CurrentWagonIndex = 0;

        OnWagonChanged?.Invoke(CurrentWagonIndex);
    }

    public void PreviousWagon()
    {
        if (!CanChangeWagon())
            return;

        CurrentWagonIndex--;

        if (CurrentWagonIndex < 0)
            CurrentWagonIndex = TotalWagons - 1;

        OnWagonChanged?.Invoke(CurrentWagonIndex);
    }

    public void OpenCurrentWagon()
    {
        if (!IsActive || IsFinished)
            return;

        WagonContent wagon = _wagons[CurrentWagonIndex];

        if (wagon.IsOpened)
            return;

        if (_isTutorial)
        {
            _tutorialClickCount++;

            if (_tutorialClickCount == 1)
            {
                wagon.ForceMultiplier(3);
            }
            else if (_tutorialClickCount == 2)
            {
                wagon.ForceMultiplier(5);
            }
            else if (_tutorialClickCount == 3)
            {
                wagon.ForceConductor();
            }
        }

        wagon.Open();
        OpenedWagonsCount++;

        OnWagonOpened?.Invoke(CurrentWagonIndex, wagon);

        if (wagon.Type == WagonContentType.Conductor)
        {
            FinishBonus();
            return;
        }

        TotalMultiplier += wagon.Multiplier;

        OnMultiplierChanged?.Invoke(TotalMultiplier);

        if (OpenedWagonsCount >= MaxOpenedWagons)
        {
            FinishBonus();
        }
    }

    public void SetupTutorial(IReadOnlyList<Sprite> wagonSprites)
    {
        _wagons.Clear();
        _isTutorial = true;
        _tutorialClickCount = 0;
        List<Sprite> randomizedSprites = GetRandomizedSprites(wagonSprites);

        _wagons.Add(WagonContent.CreateMultiplier(3, randomizedSprites[0]));
        _wagons.Add(WagonContent.CreateMultiplier(5, randomizedSprites[1]));
        _wagons.Add(WagonContent.CreateConductor(randomizedSprites[2]));
        _wagons.Add(WagonContent.CreateConductor(randomizedSprites[3]));
        _wagons.Add(WagonContent.CreateConductor(randomizedSprites[4]));
        _wagons.Add(WagonContent.CreateConductor(randomizedSprites[5]));

        CurrentWagonIndex = 0;
        OpenedWagonsCount = 0;
        TotalMultiplier = 0;
        IsActive = true;
        IsFinished = false;

        OnWagonChanged?.Invoke(CurrentWagonIndex);
        OnMultiplierChanged?.Invoke(TotalMultiplier);
    }
    
    public int GetReward()
    {
        return BaseReward * TotalMultiplier;
    }
    public void FinishByTimer()
    {
        if (!IsActive || IsFinished)
            return;

        FinishBonus();
    }

    private void FinishBonus()
    {
        IsActive = false;
        IsFinished = true;

        OnBonusFinished?.Invoke(TotalMultiplier);
    }

    private bool CanChangeWagon()
    {
        return IsActive && !IsFinished;
    }

    private void GenerateWagons(IReadOnlyList<Sprite> wagonSprites)
    {
        int conductorIndex = _random.Next(0, TotalWagons);
        List<Sprite> randomizedSprites = GetRandomizedSprites(wagonSprites);

        for (int i = 0; i < TotalWagons; i++)
        {
            Sprite sprite = randomizedSprites[i];

            if (i == conductorIndex)
            {
                _wagons.Add(WagonContent.CreateConductor(sprite));
            }
            else
            {
                int multiplier = GetRandomMultiplier();
                _wagons.Add(WagonContent.CreateMultiplier(multiplier, sprite));
            }
        }
    }
    
    private List<Sprite> GetRandomizedSprites(IReadOnlyList<Sprite> wagonSprites)
    {
        List<Sprite> result = new();

        if (wagonSprites == null || wagonSprites.Count == 0)
            return result;

        for (int i = 0; i < wagonSprites.Count; i++)
            result.Add(wagonSprites[i]);

        for (int i = result.Count - 1; i > 0; i--)
        {
            int randomIndex = _random.Next(0, i + 1);
            (result[i], result[randomIndex]) = (result[randomIndex], result[i]);
        }

        while (result.Count < TotalWagons)
            result.Add(result[_random.Next(0, result.Count)]);

        return result;
    }

    private int GetRandomMultiplier()
    {
        return _random.Next(2, 10);
    }
}