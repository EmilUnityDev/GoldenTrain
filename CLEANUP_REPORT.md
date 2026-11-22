# Golden Train — Integration Cleanup Report

## Scope

This copy was prepared from the full `golden train.zip`. The separately uploaded `Assets(1).zip` has the same Assets files (matching ZIP CRC and size), so there was no need to mix versions.

## Removed

- `Assets/BoomerangShared/`, corporate bootstrap prefab and references in `Boot.unity`.
- `Assets/Skillz/`, `Assets/Resources/Skillz/` and the Boot scene tournament manager.
- Corporate package installer, configuration asset and associated package metadata (including any credentials in the configuration asset).
- AppsFlyer UPM package dependency and native event calls.

## Local replacements

- Main-menu Play and tutorial-completion actions now load the Gameplay scene directly.
- `LocalMatchManager` replaces tournament score submission and returns to the MainMenu scene after storing local match statistics.
- `HighScoreView` and `AverageScoreDisplay` show PlayerPrefs-based scores instead of querying a remote tournament backend.
- `LocalAnalyticEventService` and `LocalEventLogger` replace remote attribution calls.
- `TimerComponent` reports threshold events to local logging.
- The existing match-end popup, prefab and MonoBehaviour script maintain their Unity GUIDs while using generic names.

## Validation limitations

Static checks can detect deleted SDK references and missing GUIDs but cannot prove compilation or runtime behavior. Unity Editor is not available in this execution environment.

## Unity play-test checklist

1. Open the root project with Unity 2023.2.22f1 and let the packages resolve.
2. Check the Console for missing scripts and compile errors.
3. Open `Assets/Game/Scenes/Boot.unity`. Verify Boot -> Tutorial on first run and Boot -> Main Menu on subsequent runs.
4. Finish or skip tutorial; verify it enters Gameplay without opening tournament UI.
5. In Main Menu press Play; verify Gameplay starts, spin and timer function.
6. Finish a match and press the results button; verify return to Main Menu.
7. Play a second match; verify best score and average score update locally.
8. Verify music, haptics, bonus minigames, daily quests and collection progression.
9. Restart the app; verify local save data remains and no corporate network requests occur.

## Not cleared for public GitHub

`Assets/Feel` and `Assets/Epic Toon FX` include third-party assets. Distribution rights, any purchased packages, and art/sound licensing need review. Deleting them without replacement may break visuals, so this working copy retains them for testing. The original source ZIP should never be uploaded as-is. Deleting secrets from a copy is not a substitute for revoking them at the provider.
