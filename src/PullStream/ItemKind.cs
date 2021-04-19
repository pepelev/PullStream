using System;

namespace PullStream
{
    [Flags]
    public enum ItemKind
    {
        First = 0x1,
        Middle = 0x2,
        Last = 0x4,
        Single = First | Last
    }
}