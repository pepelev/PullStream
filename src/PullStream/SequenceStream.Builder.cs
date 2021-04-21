using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PullStream
{
    public static class SequenceStream
    {
        public sealed class Builder<TContext>
        {
            internal readonly Func<Stream, TContext> factory;
            internal readonly Action<TContext> dispose;

            internal Builder(Func<Stream, TContext> factory, Action<TContext> dispose)
            {
                this.factory = factory;
                this.dispose = dispose;
            }

            public Builder<TItem, TContext> On<TItem>(IEnumerable<TItem> sequence) =>
                new(this, sequence, ArrayPool<byte>.Shared);

            public AsyncBuilder<TItem, TContext> On<TItem>(IAsyncEnumerable<TItem> sequence) => new(
                this,
                sequence,
                ArrayPool<byte>.Shared
            );
        }

        public sealed class Builder<TItem, TContext>
        {
            private readonly Builder<TContext> builder;
            private readonly IEnumerable<TItem> sequence;
            private readonly ArrayPool<byte> pool;

            internal Builder(Builder<TContext> builder, IEnumerable<TItem> sequence, ArrayPool<byte> pool)
            {
                this.builder = builder;
                this.sequence = sequence;
                this.pool = pool;
            }

            public Builder<(ItemKind Kind, TItem Item), TContext> WithItemKind() => new(
                builder,
                sequence.WithItemKind(),
                pool
            );

            public Builder<Item<TItem>, TContext> AsItems() => new(
                builder,
                sequence.AsItems(),
                pool
            );

            public Builder<(int Index, TItem Item), TContext> Indexed() => new(
                builder,
                sequence.Indexed(),
                pool
            );

            public Builder<TItem, TContext> Pooling(ArrayPool<byte> newPool) => new(
                builder,
                sequence,
                newPool
            );

            public Stream Writing(Action<TContext, TItem> write) => new SequenceStream<TItem, TContext>(
                builder.factory,
                builder.dispose,
                write,
                pool,
                sequence.GetEnumerator()
            );
        }

        public sealed class AsyncBuilder<TItem, TContext>
        {
            private readonly Builder<TContext> builder;
            private readonly IAsyncEnumerable<TItem> sequence;
            private readonly ArrayPool<byte> pool;

            internal AsyncBuilder(Builder<TContext> builder, IAsyncEnumerable<TItem> sequence, ArrayPool<byte> pool)
            {
                this.builder = builder;
                this.sequence = sequence;
                this.pool = pool;
            }

            public AsyncBuilder<(ItemKind Kind, TItem Item), TContext> WithItemKind() => new(
                builder,
                sequence.WithItemKind(),
                pool
            );

            public AsyncBuilder<Item<TItem>, TContext> AsItems() => new(
                builder,
                sequence.AsItems(),
                pool
            );

            public AsyncBuilder<(int Index, TItem Item), TContext> Indexed() => new(
                builder,
                sequence.Indexed(),
                pool
            );

            public AsyncBuilder<TItem, TContext> Pooling(ArrayPool<byte> newPool) => new(
                builder,
                sequence,
                newPool
            );

            public Stream Writing(Action<TContext, TItem> write) => new AsyncSequenceStream<TItem, TContext>(
                builder.factory,
                builder.dispose,
                write,
                pool,
                sequence.GetAsyncEnumerator()
            );
        }

        public static Builder<Stream> UsingStream() => Using(stream => stream);

        public static Builder<T> Using<T>(Func<Stream, T> contextFactory) where T : IDisposable => Using(
            contextFactory,
            disposable => disposable.Dispose()
        );

        public static Builder<T> Using<T>(Func<Stream, T> contextFactory, Action<T> dispose) => new(
            contextFactory,
            dispose
        );
    }
}