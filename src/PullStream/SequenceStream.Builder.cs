using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace PullStream
{
    public static class SequenceStream
    {
        public static Builder<Stream> UsingStream() => Using(stream => stream);

        public static Builder<T> Using<T>(Func<Stream, T> contextFactory) where T : IDisposable
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException(nameof(contextFactory));
            }

            return Using(contextFactory, disposable => disposable.Dispose());
        }

        public static Builder<T> Using<T>(Func<Stream, T> contextFactory, Action<T> dispose)
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException(nameof(contextFactory));
            }

            if (dispose == null)
            {
                throw new ArgumentNullException(nameof(dispose));
            }

            return new(contextFactory, dispose);
        }

        public static Stream FromStrings(IEnumerable<string> sequence, Encoding encoding, string separator)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }

            return Using(stream => new StreamWriter(stream, encoding))
                .On(sequence)
                .WithItemKind()
                .Writing(
                    (writer, item) =>
                    {
                        var (kind, value) = item;
                        writer.Write(value);
                        if (!kind.IsLast())
                        {
                            writer.Write(separator);
                        }
                    }
                );
        }

        public static Stream FromStrings(IAsyncEnumerable<string> sequence, Encoding encoding, string separator) =>
            FromStrings(sequence, encoding, separator, CancellationToken.None);

        public static Stream FromStrings(
            IAsyncEnumerable<string> sequence,
            Encoding encoding,
            string separator,
            CancellationToken token)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }

            return Using(stream => new StreamWriter(stream, encoding))
                .On(sequence)
                .WithCancellation(token)
                .WithItemKind()
                .Writing(
                    (writer, item) =>
                    {
                        var (kind, value) = item;
                        writer.Write(value);
                        if (!kind.IsLast())
                        {
                            writer.Write(separator);
                        }
                    }
                );
        }

        public static Stream Concatenation(IEnumerable<Stream> streams, ArrayPool<byte> pool, int chunkSize) =>
            UsingStream()
                .On(streams.Chunks(pool, chunkSize))
                .Build();

        public static Stream Concatenation(
            IAsyncEnumerable<Stream> streams,
            ArrayPool<byte> pool,
            int chunkSize,
            CancellationToken token = default) =>
            UsingStream()
                // ReSharper disable once MethodSupportsCancellation
                .On(streams.Chunks(pool, chunkSize))
                .WithCancellation(token)
                .Build();

        public sealed class Builder<TContext>
        {
            internal readonly Func<Stream, TContext> factory;
            internal readonly Action<TContext> dispose;

            internal Builder(Func<Stream, TContext> factory, Action<TContext> dispose)
            {
                this.factory = factory;
                this.dispose = dispose;
            }

            public Builder<TItem, TContext> On<TItem>(IEnumerable<TItem> sequence)
            {
                if (sequence == null)
                {
                    throw new ArgumentNullException(nameof(sequence));
                }

                return new(this, sequence, ArrayPool<byte>.Shared);
            }

            public AsyncBuilder<TItem, TContext> On<TItem>(IAsyncEnumerable<TItem> sequence)
            {
                if (sequence == null)
                {
                    throw new ArgumentNullException(nameof(sequence));
                }

                return new(this, sequence, ArrayPool<byte>.Shared, CancellationToken.None);
            }

            public Builder<TContext> Over(Func<Stream, Stream> wrap)
            {
                if (wrap == null)
                {
                    throw new ArgumentNullException(nameof(wrap));
                }

                return new(
                    stream => factory(wrap(stream)),
                    dispose
                );
            }
        }

        public sealed class Builder<TItem, TContext>
        {
            private readonly Builder<TContext> builder;
            private readonly ArrayPool<byte> pool;
            private readonly IEnumerable<TItem> sequence;

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

            public Builder<TItem, TContext> Pooling(ArrayPool<byte> newPool)
            {
                if (newPool == null)
                {
                    throw new ArgumentNullException(nameof(newPool));
                }

                return new(builder, sequence, newPool);
            }

            public Stream Writing(Action<TContext, TItem> write)
            {
                if (write == null)
                {
                    throw new ArgumentNullException(nameof(write));
                }

                return new SequenceStream<TItem, TContext>(
                    builder.factory,
                    builder.dispose,
                    write,
                    pool,
                    sequence.GetEnumerator()
                );
            }
        }

        public sealed class AsyncBuilder<TItem, TContext>
        {
            private readonly Builder<TContext> builder;
            private readonly ArrayPool<byte> pool;
            private readonly IAsyncEnumerable<TItem> sequence;
            private readonly CancellationToken token;

            internal AsyncBuilder(
                Builder<TContext> builder,
                IAsyncEnumerable<TItem> sequence,
                ArrayPool<byte> pool,
                CancellationToken token)
            {
                this.builder = builder;
                this.sequence = sequence;
                this.pool = pool;
                this.token = token;
            }

            public AsyncBuilder<(ItemKind Kind, TItem Item), TContext> WithItemKind() => new(
                builder,
                sequence.WithItemKind(),
                pool,
                token
            );

            public AsyncBuilder<Item<TItem>, TContext> AsItems() => new(
                builder,
                sequence.AsItems(),
                pool,
                token
            );

            public AsyncBuilder<(int Index, TItem Item), TContext> Indexed() => new(
                builder,
                sequence.Indexed(),
                pool,
                token
            );

            public AsyncBuilder<TItem, TContext> Pooling(ArrayPool<byte> newPool)
            {
                if (newPool == null)
                {
                    throw new ArgumentNullException(nameof(newPool));
                }

                return new(builder, sequence, newPool, token);
            }

            public AsyncBuilder<TItem, TContext> WithCancellation(CancellationToken cancellationToken) => new(
                builder,
                sequence,
                pool,
                cancellationToken
            );

            public Stream Writing(Action<TContext, TItem> write) => new AsyncSequenceStream<TItem, TContext>(
                builder.factory,
                builder.dispose,
                write,
                pool,
                sequence.GetAsyncEnumerator(token)
            );
        }
    }
}