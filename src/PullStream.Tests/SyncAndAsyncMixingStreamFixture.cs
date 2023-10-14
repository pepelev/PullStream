using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PullStream.Tests;

public sealed class SyncAndAsyncMixingStreamFixture : IStreamFixture
{
    private readonly IStreamFixture fixture;

    public SyncAndAsyncMixingStreamFixture(IStreamFixture fixture)
    {
        this.fixture = fixture;
    }

    public System.IO.Stream Create<TItem, TContext>(
        Func<System.IO.Stream, TContext> contextFactory,
        Action<TContext> dispose,
        IEnumerable<TItem> sequence,
        ArrayPool<byte> pool,
        Action<TContext, TItem> write)
        => new Stream(
            fixture.Create(contextFactory, dispose, sequence, pool, write),
            new Random(Guid.NewGuid().GetHashCode())
        );

    private sealed class Stream : System.IO.Stream
    {
        private readonly System.IO.Stream stream;
        private readonly Random random;

        public Stream(System.IO.Stream stream, Random random)
        {
            this.stream = stream;
            this.random = random;
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (random.Next(0, 2) == 0)
            {
                return InternalReadSync(buffer, offset, count);
            }

            return InternalReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (random.Next(0, 2) == 0)
            {
                return InternalReadSync(buffer, offset, count);
            }

            return await InternalReadAsync(buffer, offset, count);
        }

        private int InternalReadSync(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        private Task<int> InternalReadAsync(byte[] buffer, int offset, int count)
        {
            return stream.ReadAsync(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            stream.Dispose();
        }
    }
}