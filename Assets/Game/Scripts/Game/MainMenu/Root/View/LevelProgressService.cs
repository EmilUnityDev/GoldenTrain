using System;
using UnityEngine;

public class LevelProgressService
{
    private static LevelProgressService _instance;

    public static LevelProgressService Instance
    {
        get
        {
            if (_instance == null)
                _instance = new LevelProgressService();

            return _instance;
        }
    }

    private const string LevelKey = "PlayerLevel";
    private const string ExperienceKey = "PlayerExperience";

    public static event Action<int, float> OnAnyProgressChanged;

    private int _level;
    private float _experience;

    public int Level => PlayerPrefs.GetInt(LevelKey, 1);
    public float Experience => PlayerPrefs.GetFloat(ExperienceKey, 0f);

    public void AddExperience(float amount)
    {
        if (amount <= 0)
            return;

        _level = Level;
        _experience = Experience;

        _experience += amount;

        while (_experience >= 100f)
        {
            _experience -= 100f;
            _level++;
        }

        Save();

        OnAnyProgressChanged?.Invoke(_level, _experience);
    }

    public void AddExperiencePercent(float percent)
    {
        AddExperience(percent);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(LevelKey, _level);
        PlayerPrefs.SetFloat(ExperienceKey, _experience);
        PlayerPrefs.Save();
    }
}