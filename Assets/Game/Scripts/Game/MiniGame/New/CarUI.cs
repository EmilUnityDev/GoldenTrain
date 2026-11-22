using UnityEngine;
using UnityEngine.UI;

public class CarUI : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;

    private float _speedPxPerSec;
    private float _dir;
    private float _killX;
    private RectTransform _chicken;
    private float _hitRadiusPx;
    private bool _ignoreChickenCollision;

    public System.Action<CarUI> OnDespawn;
    public System.Action OnHitChicken;

    private void Awake()
    {
        if (rect == null) rect = (RectTransform)transform;
        if (image == null) image = GetComponentInChildren<Image>(true);
    }

    public void SetSprite(Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
    }

    public void Init(RectTransform chicken, float speedPxPerSec, float dir, float killX, float hitRadiusPx, bool ignoreChickenCollision = false)
    {
        _chicken = chicken;
        _speedPxPerSec = speedPxPerSec;
        _dir = dir;
        _killX = killX;
        _hitRadiusPx = hitRadiusPx;
        _ignoreChickenCollision = ignoreChickenCollision;
    }

    private void Update()
    {
        var p = rect.anchoredPosition;
        p.x += _dir * _speedPxPerSec * Time.deltaTime;
        rect.anchoredPosition = p;

        if (_chicken != null)
        {
            float d = Vector2.Distance(rect.anchoredPosition, _chicken.anchoredPosition);
            if (d <= _hitRadiusPx)
            {
                if (!_ignoreChickenCollision)
                {
                    OnHitChicken?.Invoke();
                    Despawn();
                    return;
                }
            }
        }

        float halfWidth = rect.rect.width * 0.5f;

        if ((_dir > 0f && rect.anchoredPosition.x - halfWidth > _killX) ||
            (_dir < 0f && rect.anchoredPosition.x + halfWidth < -_killX))
        {
            Despawn();
        }
    }

    public void Despawn()
    {
        OnDespawn?.Invoke(this);
    }
}