using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PullStream.Tests
{
    public static class StreamFixtureExtensions
    {
        public static Stream Create<TItem, TContext>(
            this IStreamFixture streamFixture,
            Func<Stream, TContext> contextFactory,
            IEnumerable<TItem> sequence,
            Action<TContext, TItem> write) where TContext : IDisposable
            => streamFixture.Create(
                contextFactory,
                context => context.Dispose(),
                sequence,
                ArrayPool<byte>.Shared,
                write
            );

        public static Stream Create<TItem, TContext>(
            this IStreamFixture streamFixture,
            Func<Stream, TContext> contextFactory,
            IEnumerable<TItem> sequence,
            ArrayPool<byte> pool,
            Action<TContext, TItem> write) where TContext : IDisposable
            => streamFixture.Create(
                contextFactory,
                context => context.Dispose(),
                sequence,
                pool,
                write
            );
    }
}