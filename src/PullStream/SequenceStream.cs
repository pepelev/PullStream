using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PullStream
{
    public sealed class SequenceStream<T, TContext> : Stream
    {
        private readonly CircularBuffer buffer;
        private readonly IEnumerator<T> enumerator;
        private readonly Lazy<TContext> context;
        private readonly Action<TContext> dispose;
        private readonly Action<TContext, T> write;
        private State state = State.MoveNext;

        public SequenceStream(
            Func<Stream, TContext> contextFactory,
            Action<TContext> dispose,
            Action<TContext, T> write,
            ArrayPool<byte> pool,
            IEnumerator<T> enumerator)
        {
            this.dispose = dispose;
            this.write = write;
            this.enumerator = enumerator;
            buffer = new(pool);
            context = new(
                () => contextFactory(buffer.WriteStream),
                LazyThreadSafetyMode.None
            );
        }

        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                CheckDisposed();
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                CheckDisposed();
                return false;
            }
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get
            {
                CheckDisposed();
                return buffer.BytesCut;
            }
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (state == State.Completed || state == State.Disposed)
            {
                return;
            }

            Cleanup();
            state = State.Disposed;
        }

        private void Cleanup()
        {
            enumerator.Dispose();

            if (context.IsValueCreated)
            {
                dispose(context.Value);
            }

            buffer.Dispose();
        }

        public override void Flush()
        {
            CheckDisposed();
        }

        public override int Read(byte[] destination, int offset, int count)
        {
            CheckDisposed();
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
                    Cleanup();
                    state = State.Completed;
                }
            }

            var length = Math.Min(count, buffer.BytesReady);
            buffer.Read(destination.AsSpan().Slice(offset, length));
            buffer.Cut(length);
            return length;
        }

        private void CheckDisposed()
        {
            if (state == State.Disposed)
            {
                throw new ObjectDisposedException(nameof(SequenceStream));
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] source, int offset, int count) => throw new NotSupportedException();
    }
}