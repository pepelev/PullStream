using System.Diagnostics.Contracts;

namespace PullStream;

public static class ItemKindExtensions
{
    [Pure]
    public static bool IsFirst(this ItemKind kind) => (kind & ItemKind.First) != 0;

    [Pure]
    public static bool IsMiddle(this ItemKind kind) => (kind & ItemKind.Middle) != 0;

    [Pure]
    public static bool IsLast(this ItemKind kind) => (kind & ItemKind.Last) != 0;
}