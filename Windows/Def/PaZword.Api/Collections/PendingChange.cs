namespace PaZword.Api.Collections
{
    internal readonly struct PendingChange<T>
    {
        internal PendingChangeType Type { get; }

        internal T Item { get; }

        internal int Index { get; }

        internal int Index2 { get; }

        internal PendingChange(PendingChangeType type)
        {
            Type = type;
            Item = default;
            Index = -1;
            Index2 = -1;
        }

        internal PendingChange(PendingChangeType type, int index)
        {
            Type = type;
            Item = default;
            Index = index;
            Index2 = -1;
        }

        internal PendingChange(PendingChangeType type, T item)
        {
            Type = type;
            Item = item;
            Index = -1;
            Index2 = -1;
        }

        internal PendingChange(PendingChangeType type, T item, int index)
        {
            Type = type;
            Item = item;
            Index = index;
            Index2 = -1;
        }

        internal PendingChange(PendingChangeType type, int index, int index2)
        {
            Type = type;
            Item = default;
            Index = index;
            Index2 = index2;
        }
    }
}
