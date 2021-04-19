using System;
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

            public Builder<TItem, TContext> On<TItem>(IEnumerable<TItem> sequence) => new(this, sequence);
        }

        public sealed class Builder<TItem, TContext>
        {
            private readonly Builder<TContext> builder;
            private readonly IEnumerable<TItem> sequence;

            internal Builder(Builder<TContext> builder, IEnumerable<TItem> sequence)
            {
                this.builder = builder;
                this.sequence = sequence;
            }

            public Builder<(ItemKind Kind, TItem Item), TContext> WithItemKind() => new(
                builder,
                sequence.WithItemKind()
            );

            public SequenceStream<TItem, TContext> Writing(Action<TContext, TItem> write) => new(
                builder.factory,
                builder.dispose,
                write,
                sequence.GetEnumerator()
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