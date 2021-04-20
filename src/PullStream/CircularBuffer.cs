using System;
using System.Buffers;
using System.IO;

namespace PullStream
{
    internal sealed class CircularBuffer : IDisposable
    {
        private readonly ArrayPool<byte> pool;
        private Content content = Content.Empty;

        public CircularBuffer(ArrayPool<byte> pool)
        {
            this.pool = pool;
        }

        public int BytesReady => content.Count;
        public long BytesCut { get; private set; }

        public System.IO.Stream WriteStream => new Stream(this);

        public void Read(Span<byte> destination)
        {
            content.Read(destination);
        }

        public void Cut(int length)
        {
            content = content.Cut(length);
            BytesCut += length;
        }

        private readonly struct Content
        {
            public static Content Empty => new(Array.Empty<byte>(), 0, 0);

            private readonly byte[] buffer;
            private readonly int offset;
            public int Count { get; }

            private Content(byte[] buffer, int offset, int count)
            {
                this.buffer = buffer;
                this.offset = offset;
                Count = count;
            }

            public Content Append(Span<byte> bytes, ArrayPool<byte> pool)
            {
                if (buffer.Length < Count + bytes.Length)
                {
                    var newLength = Math.Max(Count + bytes.Length, buffer.Length * 2);
                    var newBuffer = pool.Rent(newLength);
                    Read(newBuffer.AsSpan()[..Count]);
                    bytes.CopyTo(newBuffer.AsSpan()[Count..]);
                    pool.Return(buffer);
                    return new Content(
                        newBuffer,
                        0,
                        Count + bytes.Length
                    );
                }

                if (buffer.Length == 0)
                {
                    return this;
                }

                var cursor = (offset + Count) % buffer.Length;
                var tail = buffer.AsSpan()[cursor..];
                if (bytes.Length < tail.Length)
                {
                    bytes.CopyTo(tail);
                }
                else
                {
                    bytes[..tail.Length].CopyTo(tail);
                    bytes[tail.Length..].CopyTo(new Span<byte>(buffer));
                }

                return new Content(buffer, offset, Count + bytes.Length);
            }

            public void Read(Span<byte> destination)
            {
                if (Count < destination.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var span = new Span<byte>(buffer);
                if (destination.Length <= span.Length - offset)
                {
                    var source = span.Slice(offset, destination.Length);
                    source.CopyTo(destination);
                    return;
                }

                span[offset..].CopyTo(destination);
                var copied = span.Length - offset;
                span[..(destination.Length - copied)].CopyTo(destination[copied..]);
            }

            public Content Cut(int length)
            {
                if (length > Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(length));
                }

                if (buffer.Length == 0)
                {
                    return this;
                }

                return new Content(
                    buffer,
                    (offset + length) % buffer.Length,
                    Count - length
                );
            }

            public void Return(ArrayPool<byte> pool)
            {
                if (buffer.Length > 0)
                {
                    pool.Return(buffer);
                }
            }
        }

        private sealed class Stream : System.IO.Stream
        {
            private readonly CircularBuffer buffer;

            public Stream(CircularBuffer buffer)
            {
                this.buffer = buffer;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => Length;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] destination, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] source, int offset, int count)
            {
                var bytes = new Span<byte>(source, offset, count);
                buffer.content = buffer.content.Append(bytes, buffer.pool);
            }
        }

        public void Dispose()
        {
            content.Return(pool);
        }
    }
}