using UnityEngine;
using TMPro;

[ExecuteAlways] 
public class TMPAnimatorBridge : MonoBehaviour {
    private TMP_Text textComponent;

    public int visibleCharacters = -1;

    void Start() {
        textComponent = GetComponent<TMP_Text>();
    }

    void Update() {
        if (textComponent != null) {
            textComponent.maxVisibleCharacters = visibleCharacters;
        }
    }
}