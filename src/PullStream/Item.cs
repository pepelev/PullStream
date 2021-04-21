using System;
using System.Collections.Generic;

namespace PullStream
{
    public readonly struct Item<T> : IEquatable<Item<T>>
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

        public bool Equals(Item<T> other) =>
            Index == other.Index &&
            Kind == other.Kind &&
            EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is Item<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Index;
                hashCode = (hashCode * 397) ^ (int) Kind;
                if (Value == null)
                {
                    return hashCode;
                }

                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
                return hashCode;
            }
        }
    }
}