using UnityEngine;

public class WagonContent
{
    public WagonContentType Type { get; private set; }
    public int Multiplier { get; private set; }
    public bool IsOpened { get; private set; }
    public Sprite WagonSprite { get; private set; }

    private WagonContent(WagonContentType type, int multiplier, Sprite wagonSprite)
    {
        Type = type;
        Multiplier = multiplier;
        WagonSprite = wagonSprite;
        IsOpened = false;
    }

    public void ForceMultiplier(int multiplier)
    {
        Type = WagonContentType.Multiplier;
        Multiplier = multiplier;
    }

    public void ForceConductor()
    {
        Type = WagonContentType.Conductor;
        Multiplier = 0;
    }
    public static WagonContent CreateMultiplier(int multiplier, Sprite wagonSprite)
    {
        return new WagonContent(WagonContentType.Multiplier, multiplier, wagonSprite);
    }

    public static WagonContent CreateConductor(Sprite wagonSprite)
    {
        return new WagonContent(WagonContentType.Conductor, 0, wagonSprite);
    }

    public void Open()
    {
        IsOpened = true;
    }
}