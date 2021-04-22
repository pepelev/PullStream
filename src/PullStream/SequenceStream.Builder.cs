using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

                return new(this, sequence, ArrayPool<byte>.Shared);
            }
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

            public AsyncBuilder<TItem, TContext> Pooling(ArrayPool<byte> newPool)
            {
                if (newPool == null)
                {
                    throw new ArgumentNullException(nameof(newPool));
                }

                return new(builder, sequence, newPool);
            }

            public Stream Writing(Action<TContext, TItem> write) => new AsyncSequenceStream<TItem, TContext>(
                builder.factory,
                builder.dispose,
                write,
                pool,
                sequence.GetAsyncEnumerator()
            );
        }

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

        public static Stream FromStrings(IAsyncEnumerable<string> sequence, Encoding encoding, string separator)
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
    }
}