using System.Collections.Generic;

namespace Game.Scripts.Services.Popups.TutorialHintsPopup.Scenarios
{
    public class TutorialScenarioFactory
    {
        private static readonly Dictionary<TutorialHintGroup, ITutorialScenario> ScenarioCache = new();

        public static ITutorialScenario CreateScenario(TutorialHintGroup group)
        {
            if (ScenarioCache.TryGetValue(group, out var cachedScenario))
                return cachedScenario;

            ITutorialScenario scenario = group switch
            {
                TutorialHintGroup.FirstGroup => new FirstGroupScenario(),
                TutorialHintGroup.SecondGroup => new SecondGroupScenario(),
                TutorialHintGroup.ThirdGroup => new ThirdGroupScenario(),
                TutorialHintGroup.FourthGroup => new FourthGroupScenario(),
                TutorialHintGroup.FifthGroup => new FifthGroupScenario(),
                TutorialHintGroup.SixGroup => new SixGroupScenario(),
                TutorialHintGroup.SevenGroup => new SeventhGroupScenario(),
                _ => null
            };

            if (scenario != null)
                ScenarioCache[group] = scenario;

            return scenario;
        }

        public static void ClearCache()
        {
            foreach (var scenario in ScenarioCache.Values)
                scenario?.Cleanup();
            ScenarioCache.Clear();
        }
    }
}