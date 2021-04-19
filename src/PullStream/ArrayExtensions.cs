namespace PullStream
{
#if NETSTANDARD2_0
    internal static class ArrayExtensions
    {
        public static Span<T> AsSpan<T>(this T[] array) => new(array);
    }
#endif
}