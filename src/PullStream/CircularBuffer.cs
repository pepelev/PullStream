using System;
using System.IO;

namespace ClassLibrary1
{
    internal sealed class CircularBuffer
    {
        private Content content = Content.Empty;
        private int version;
        public int BytesReady => content.Count;
        public long BytesCut { get; private set; }

        public void Read(Span<byte> destination)
        {
            content.Read(destination);
        }

        public void Cut(int length)
        {
            content = content.Cut(length);
            version++;
            BytesCut += length;
        }

        public TransactionStream BeginWrite()
        {
            version++;
            return new TransactionStream(this);
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

            public Content Append(ReadOnlySpan<byte> bytes)
            {
                if (buffer.Length < Count + bytes.Length)
                {
                    var newLength = Math.Max(1024 * 80, Math.Max(Count + bytes.Length, buffer.Length * 2));
                    var newBuffer = new byte[newLength];
                    Read(newBuffer.AsSpan()[..Count]);
                    bytes.CopyTo(newBuffer.AsSpan()[Count..]);
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
                    bytes[tail.Length..].CopyTo(buffer);
                }

                return new Content(buffer, offset, Count + bytes.Length);
            }

            public void Read(Span<byte> destination)
            {
                if (Count < destination.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var span = new ReadOnlySpan<byte>(buffer);
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
        }

        public sealed class TransactionStream : Stream
        {
            private readonly CircularBuffer buffer;
            private readonly long workingVersion;
            private Content content;

            public TransactionStream(CircularBuffer buffer)
            {
                this.buffer = buffer;
                content = buffer.content;
                workingVersion = buffer.version;
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

            public void Commit()
            {
                CheckVersion();
                buffer.content = content;
                buffer.version++;
            }

            public override void Flush() => CheckVersion();

            public override int Read(byte[] destination, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] source, int offset, int count)
            {
                CheckVersion();
                var bytes = new ReadOnlySpan<byte>(source, offset, count);
                content = content.Append(bytes);
            }

            private void CheckVersion()
            {
                if (buffer.version != workingVersion)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}