using UnityEngine;

namespace Tutorials
{
    public sealed class TutorialHierarchy : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }
        [field: SerializeField] public GameObject ScreenPrefab { get; private set; }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Canvas == null) Canvas = GetComponentInChildren<Canvas>(true);
        }
#endif
    }
}