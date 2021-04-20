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
        private readonly CircularBuffer buffer;
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
            CleanupAsync().AsTask().Wait();
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

            buffer.Dispose();
        }

        public override void Flush()
        {
            CheckDisposed();
        }

        public override Task<int> ReadAsync(byte[] destination, int offset, int count, CancellationToken cancellationToken)
        {
            var memory = new Memory<byte>(destination, offset, count);
            return ReadAsync(memory, cancellationToken).AsTask();
        }

#if NETSTANDARD2_0
        private
#else
        public override
#endif
        async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = new())
        {
            CheckDisposed();
            while (state != State.Completed && buffer.BytesReady < destination.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (state == State.MoveNext)
                {
                    if (!await enumerator.MoveNextAsync())
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
                    await CleanupAsync();
                    state = State.Completed;
                }
            }

            var length = Math.Min(destination.Length, buffer.BytesReady);
            buffer.Read(destination.Span);
            buffer.Cut(length);
            return length;
        }

        public override int Read(byte[] destination, int offset, int count)
        {
            return ReadAsync(destination, offset, count).Result;
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