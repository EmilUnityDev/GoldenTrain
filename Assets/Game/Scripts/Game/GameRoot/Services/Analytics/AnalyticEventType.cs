namespace Game.Scripts.GameRoot.Services.Analytics
{
    public enum AnalyticEventType
    {
        // FTUE: Video
        FTUEVideoStarted,
        FTUEVideoCompleted,
        FTUEVideoSkipped,
    
        // FTUE: Tutorial 1
        FTUETutorial1Started,
        FTUETutorial1Completed,
        FTUETutorial1Skipped,

        // FTUE: Tutorial 2 (Slideshow)
        FTUETutorial2SlideshowOpened,
        FTUETutorial2SlideshowNext,
        FTUETutorial2SlideshowPrevious,
        FTUETutorial2SlideshowClosed,
        FTUETutorial2SlideshowSkipped,

        // Main menu
        TutorialSlideshowOpened,
        TutorialSlideshowNext,
        TutorialSlideshowPrevious,
        TutorialSlideshowClosed,

        // Settings
        SettingsMusicDisabled,
        SettingsSoundsDisabled,
        SettingsHapticDisabled,

        // Navigation
        SceneSwitched,
        MatchStarted,
        MatchFinished,
        MatchAborted,
        ScoreSaved
    }
}