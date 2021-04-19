#if NETSTANDARD2_0

using System;

namespace PullStream
{
    internal readonly struct Span<T>
    {
        private readonly T[] content;
        private readonly int start;

        public Span(T[] content, int start, int count)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start + count > content.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.content = content;
            this.start = start;
            Length = count;
        }

        public Span(T[] content)
            : this(content, 0, content.Length)
        {
        }

        public Span<T> this[Range range]
        {
            get
            {
                var startOffset = range.Start.FromEnd
                    ? Length - range.Start.Value
                    : range.Start.Value;

                var endOffset = range.End.FromEnd
                    ? Length - range.End.Value
                    : range.End.Value;


                var newStart = start + startOffset;
                var newEnd = start + endOffset;
                return new Span<T>(content, newStart, newEnd - newStart);
            }
        }

        public int Length { get; }

        public void CopyTo(Span<byte> destination)
        {
            Array.Copy(
                content,
                start,
                destination.content,
                destination.start,
                Length
            );
        }

        public Span<T> Slice(int offset, int length) => new(content, start + offset, length);
    }

    internal static class ArrayExtensions
    {
        public static Span<T> AsSpan<T>(this T[] array) => new(array);
    }
}

namespace System
{
    internal readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }
    }

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
    }
}

#endif