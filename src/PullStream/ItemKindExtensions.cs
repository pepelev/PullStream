namespace PullStream
{
    public static class ItemKindExtensions
    {
        public static bool IsFirst(this ItemKind kind) => (kind & ItemKind.First) != 0;
        public static bool IsMiddle(this ItemKind kind) => (kind & ItemKind.Middle) != 0;
        public static bool IsLast(this ItemKind kind) => (kind & ItemKind.Last) != 0;
    }
}