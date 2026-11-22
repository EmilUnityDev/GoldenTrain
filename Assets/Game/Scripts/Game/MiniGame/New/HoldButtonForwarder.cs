using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButtonForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ChickenRunMinigame target;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target != null)
            target.HandleHoldDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (target != null)
            target.HandleHoldUp();
    }
}