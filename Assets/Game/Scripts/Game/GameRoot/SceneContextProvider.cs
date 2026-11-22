using UnityEngine.SceneManagement;

namespace Game.Scripts.Game.GameRoot
{
    public class SceneContextProvider
    {
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        public bool IsCurrentScene(string sceneName)
        {
            return SceneManager.GetActiveScene().name == sceneName;
        }

        public bool IsTutorialScene()
        {
            return IsCurrentScene(Scenes.TUTORIAL);
        }

        public bool IsGameplayScene()
        {
            return IsCurrentScene(Scenes.GAMEPLAY);
        }

        public bool IsMainMenuScene()
        {
            return IsCurrentScene(Scenes.MAIN_MENU);
        }
    }
}

