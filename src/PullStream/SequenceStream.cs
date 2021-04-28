using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PullStream
{
    public sealed class SequenceStream<T, TContext> : Stream
    {
        private readonly CircularBuffer content;
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
            if (contextFactory == null)
            {
                throw new ArgumentNullException(nameof(contextFactory));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            content = new(pool);
            context = new(
                () => contextFactory(content.WriteStream),
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
                return content.BytesCut;
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

            content.Dispose();
        }

        public override void Flush()
        {
            CheckDisposed();
        }

#if !NETSTANDARD2_0
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = Read(buffer.Span);
            return new System.Threading.Tasks.ValueTask<int>(read);
        }
#endif

#if NETSTANDARD2_0
        private
#else
        public override
#endif
        int Read(Span<byte> buffer)
        {
            CheckDisposed();
            while (state != State.Completed && content.BytesReady < buffer.Length)
            {
                if (state == State.MoveNext)
                {
                    var moved = enumerator.MoveNext();
                    state = moved
                        ? State.Current
                        : State.Cleanup;
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
                else
                {
                    throw new InvalidOperationException($"Stream is in wrong state: {state}");
                }
            }

            var length = Math.Min(buffer.Length, content.BytesReady);
            content.Read(buffer.Slice(0, length));
            content.Cut(length);
            return length;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Must be non-negative");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "offset + count must not exceed buffer array length");
            }

            return Read(new Span<byte>(buffer, offset, count));
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