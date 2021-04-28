using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PullStream
{
    public sealed class AsyncSequenceStream<T, TContext> : Stream
    {
        private readonly CircularBuffer content;
        private readonly IAsyncEnumerator<T> enumerator;
        private readonly Lazy<TContext> context;
        private readonly Action<TContext> dispose;
        private readonly Action<TContext, T> write;
        private State state = State.MoveNext;

        public AsyncSequenceStream(
            Func<Stream, TContext> contextFactory,
            Action<TContext> dispose,
            Action<TContext, T> write,
            ArrayPool<byte> pool,
            IAsyncEnumerator<T> enumerator)
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
            DisposeAsync().AsTask().Wait();
        }

#if NETSTANDARD2_0
        private
#else
        public override
#endif
        async ValueTask DisposeAsync()
        {
            if (state == State.Completed || state == State.Disposed)
            {
                return;
            }

            await CleanupAsync();
            state = State.Disposed;
        }

        private async ValueTask CleanupAsync()
        {
            await enumerator.DisposeAsync();

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

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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

            var memory = new Memory<byte>(buffer, offset, count);
            return await ReadAsync(memory, cancellationToken);
        }

#if NETSTANDARD2_0
        private
#else
        public override
#endif
        async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            while (state != State.Completed && content.BytesReady < destination.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (state == State.MoveNext)
                {
                    var moved = await enumerator.MoveNextAsync();
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
                    await CleanupAsync();
                    state = State.Completed;
                }
                else
                {
                    throw new InvalidOperationException($"Stream is in wrong state: {state}");
                }
            }

            var length = Math.Min(destination.Length, content.BytesReady);
            content.Read(destination.Span.Slice(0, length));
            content.Cut(length);
            return length;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
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