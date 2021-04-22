using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PullStream.Tests
{
    public interface IStreamFixture
    {
        Stream Create<TItem, TContext>(
            Func<Stream, TContext> contextFactory,
            Action<TContext> dispose,
            IEnumerable<TItem> sequence,
            ArrayPool<byte> pool,
            Action<TContext, TItem> write);
    }
}