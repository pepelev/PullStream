using System;

namespace PullStream
{
    [Flags]
    public enum ItemKind
    {
        /// <summary>
        /// This item is first and there are more items
        /// </summary>
        First = 0x1,

        /// <summary>
        /// This item is neither the first nor the last
        /// </summary>
        Middle = 0x2,

        /// <summary>
        /// This item is last and there are previous items
        /// </summary>
        Last = 0x4,

        /// <summary>
        /// This item is the only item in sequence, there is no others
        /// </summary>
        Single = First | Last
    }
}