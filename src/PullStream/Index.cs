#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System
{
    internal readonly struct Index
    {
        public int Value { get; }
        public bool FromEnd { get; }

        public Index(int value, bool fromEnd = false)
        {
            Value = value;
            FromEnd = fromEnd;
        }

        public static implicit operator Index(int index) => new(index);

        public int GetOffset(int length) => FromEnd
            ? length - Value
            : Value;
    }
}

#endif