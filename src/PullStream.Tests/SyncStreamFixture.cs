using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PullStream.Tests
{
    public sealed class SyncStreamFixture : IStreamFixture
    {
        public Stream Create<TItem, TContext>(
            Func<Stream, TContext> contextFactory,
            Action<TContext> dispose,
            IEnumerable<TItem> sequence,
            ArrayPool<byte> pool,
            Action<TContext, TItem> write)
            => SequenceStream
                .Using(contextFactory, dispose)
                .On(sequence)
                .Pooling(pool)
                .Writing(write);
    }
}