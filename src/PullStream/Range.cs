#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
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
}

#endif