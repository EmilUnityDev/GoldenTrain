using UnityEngine;
using UnityEngine.UI;

public class CityInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image cityImage;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        panelRoot.SetActive(false);
    }

    public void Open(Sprite sprite)
    {
        cityImage.sprite = sprite;
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        panelRoot.SetActive(false);
    }
}