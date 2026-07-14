namespace Valheim.ServersideQoL;

public readonly record struct ItemDataKey(string Name, int Quality, int Variant)
{
    public static implicit operator ItemDataKey(ItemDrop.ItemData data) => new(data);
    public ItemDataKey(ItemDrop.ItemData data) : this(data.m_shared.m_name, data.m_quality, data.m_variant) { }
}

public readonly record struct SharedItemDataKey(string Name)
{
    public static implicit operator SharedItemDataKey(ItemDrop.ItemData.SharedData data) => new(data.m_name);
    public static implicit operator SharedItemDataKey(ItemDrop.ItemData data) => new(data.m_shared.m_name);
}

public sealed class ItemDataKeyComparer : EqualityComparer<ItemDrop.ItemData>
{
    public static ItemDataKeyComparer Instance { get; } = new();
    public override bool Equals(ItemDrop.ItemData x, ItemDrop.ItemData y) => new ItemDataKey(x) == y;
    public override int GetHashCode(ItemDrop.ItemData obj) => new ItemDataKey(obj).GetHashCode();
}

public sealed class SharedItemDataKeyComparer : EqualityComparer<ItemDrop.ItemData>
{
    public static SharedItemDataKeyComparer Instance { get; } = new();
    public override bool Equals(ItemDrop.ItemData x, ItemDrop.ItemData y) => (SharedItemDataKey)x == y;
    public override int GetHashCode(ItemDrop.ItemData obj) => ((SharedItemDataKey)obj).GetHashCode();
}