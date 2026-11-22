using System;

[Serializable]
public readonly struct CollectionItemData : IEquatable<CollectionItemData>
{
    public readonly CollectionTag Tag;
    public readonly int ItemId;

    public CollectionItemData(CollectionTag tag, int itemId)
    {
        Tag = tag;
        ItemId = itemId;
    }

    public bool Equals(CollectionItemData other)
    {
        return Tag == other.Tag && ItemId == other.ItemId;
    }

    public override bool Equals(object obj)
    {
        return obj is CollectionItemData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Tag, ItemId);
    }
}