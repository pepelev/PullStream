namespace PullStream
{
    public readonly struct Item<T>
    {
        public int Index { get; }
        public ItemKind Kind { get; }
        public T Value { get; }

        public Item(int index, ItemKind kind, T value)
        {
            Index = index;
            Kind = kind;
            Value = value;
        }

        public override string ToString() => $"{Index}-{Kind}-{Value}";

        public void Deconstruct(out int index, out ItemKind kind, out T value)
        {
            index = Index;
            kind = Kind;
            value = Value;
        }
    }
}