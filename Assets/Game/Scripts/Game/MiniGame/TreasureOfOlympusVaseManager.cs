using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Scripts.Game.MiniGame;

public class TreasureOfOlympusVaseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image vaseImage;
    [SerializeField] private Image shineImage;
    [SerializeField] private Image shineImage2;
    [SerializeField] public Image prizeImage;

    [Header("Sprites")]
    [SerializeField] private Sprite breakedVaseSprite;
    [SerializeField] private Sprite snakeSprite;
    [SerializeField] private List<Sprite> shineSprites;
    [SerializeField] private List<Sprite> prizeSprites;

    [Header("Fade Settings")]
    [SerializeField] private float defaultVaseAlpha = 1f;

    public event Action<TreasureOfOlympusVaseManager, BonusType> Clicked;

    public int MoneyIndex { get; private set; } = -1;

    private BonusType _type;
    private Coroutine _fadeRoutine;

    private Sprite _preparedPrizeSprite;
    private Sprite _preparedShineSprite;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickInternal);
            button.onClick.AddListener(OnClickInternal);
        }

        ResetVisuals();
        SetVaseAlpha(defaultVaseAlpha);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickInternal);
    }
    public void Setup(BonusType type, int moneyIndex = -1)
    {
        _type = type;

        if (type == BonusType.Snake)
        {
            MoneyIndex = -1;
            _preparedPrizeSprite = snakeSprite;
            _preparedShineSprite = null;
        }
        else
        {
            MoneyIndex = Mathf.Clamp(moneyIndex, 0, 2);

            int pairCount = Mathf.Min(shineSprites != null ? shineSprites.Count : 0,
                                     prizeSprites != null ? prizeSprites.Count : 0);

            if (pairCount < 3)
                Debug.LogWarning($"{name}: Нужно минимум 3 пары shine/prize (индексы 0..2). Сейчас pairCount={pairCount}");

            int idx = Mathf.Clamp(MoneyIndex, 0, Mathf.Max(0, pairCount - 1));
            _preparedShineSprite = shineSprites != null && shineSprites.Count > idx ? shineSprites[idx] : null;
            _preparedPrizeSprite = prizeSprites != null && prizeSprites.Count > idx ? prizeSprites[idx] : null;
        }

        ApplyPreparedSprites();
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    public void ResetVisuals()
    {
        if (shineImage != null) shineImage.gameObject.SetActive(false);
        if (prizeImage != null) prizeImage.gameObject.SetActive(false);
        if (shineImage2 != null) shineImage2.gameObject.SetActive(false);
    }

    public void FadeVaseTo(float targetAlpha, float time)
    {
        if (vaseImage == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeImageAlpha(vaseImage, targetAlpha, time));
    }

    private void SetVaseAlpha(float a)
    {
        if (vaseImage == null) return;
        var c = vaseImage.color;
        c.a = a;
        vaseImage.color = c;
    }

    private void OnClickInternal()
    {
        Clicked?.Invoke(this, _type);
    }
    public void RevealMoney()
    {
        BreakVase();
        ShowShinePrepared();
        ShowPrizePrepared();
    }

    public void RevealSnake()
    {
        BreakVase();
        ShowShinePrepared();
        ShowPrizePrepared();
    }

    private void BreakVase()
    {
        if (vaseImage == null || breakedVaseSprite == null) return;

        vaseImage.sprite = breakedVaseSprite;
        vaseImage.SetNativeSize();
    }

    private void ApplyPreparedSprites()
    {
        if (shineImage != null) shineImage.sprite = _preparedShineSprite;
        if (prizeImage != null) prizeImage.sprite = _preparedPrizeSprite;
    }

    private void ShowShinePrepared()
    {
        if (shineImage == null || _preparedShineSprite == null) return;
        shineImage.gameObject.SetActive(true);
        shineImage2.gameObject.SetActive(true);
        shineImage.sprite = _preparedShineSprite;
    }

    private void ShowPrizePrepared()
    {
        if (prizeImage == null || _preparedPrizeSprite == null) return;
        prizeImage.gameObject.SetActive(true);
        prizeImage.sprite = _preparedPrizeSprite;
    }

    private IEnumerator FadeImageAlpha(Image img, float target, float time)
    {
        if (img == null) yield break;

        float start = img.color.a;

        if (time <= 0f)
        {
            var cc = img.color;
            cc.a = target;
            img.color = cc;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, target, t / time);

            var c = img.color;
            c.a = a;
            img.color = c;

            yield return null;
        }

        var end = img.color;
        end.a = target;
        img.color = end;

        _fadeRoutine = null;
    }
}
