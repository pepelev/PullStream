using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ClassLibrary1
{
    public static class PullStream
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

            public PullStream<TItem, TContext> Writing(Action<TContext, TItem> write) => new(
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

    public sealed class PullStream<T, TContext> : Stream
    {
        private readonly CircularBuffer buffer = new();
        private readonly IEnumerator<T> enumerator;
        private readonly Lazy<TContext> context;
        private readonly Action<TContext> cleanup;
        private readonly Action<TContext, T> write;
        private State state = State.MoveNext;

        public PullStream(
            Func<Stream, TContext> contextFactory,
            Action<TContext> cleanup,
            Action<TContext, T> write,
            IEnumerator<T> enumerator)
        {
            this.cleanup = cleanup;
            this.write = write;
            this.enumerator = enumerator;
            context = new Lazy<TContext>(
                () => contextFactory(buffer.WriteStream),
                LazyThreadSafetyMode.None
            );
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => buffer.BytesCut;
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            enumerator.Dispose();

            if (context.IsValueCreated)
            {
                cleanup(context.Value);
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] destination, int offset, int count)
        {
            while (state != State.Completed && buffer.BytesReady < count)
            {
                if (state == State.MoveNext)
                {
                    if (!enumerator.MoveNext())
                    {
                        state = State.Cleanup;
                        continue;
                    }

                    state = State.Current;
                }
                else if (state == State.Current)
                {
                    write(context.Value, enumerator.Current);
                    state = State.MoveNext;
                }
                else if (state == State.Cleanup)
                {
                    Dispose(true);
                    state = State.Completed;
                }
            }

            var length = Math.Min(count, buffer.BytesReady);
            buffer.Read(destination.AsSpan().Slice(offset, length));
            buffer.Cut(length);
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] source, int offset, int count) => throw new NotSupportedException();

        private enum State
        {
            MoveNext,
            Current,
            Cleanup,
            Completed
        }
    }
}