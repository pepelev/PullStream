using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PullStream
{
    public sealed class NotDisposing : Stream
    {
        private readonly Stream stream;

        public NotDisposing(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public bool Disposed { get; private set; }

        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                CheckDisposed();
                return stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                CheckDisposed();
                return stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                CheckDisposed();
                return stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                return stream.Position;
            }
            set
            {
                CheckDisposed();
                stream.Position = value;
            }
        }

        private void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(NotDisposing));
            }
        }

        public override void Close()
        {
            base.Close();
            Disposed = true;
        }

        public override void Flush()
        {
            CheckDisposed();
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            return stream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            CheckDisposed();
            return stream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            CheckDisposed();
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            CheckDisposed();
            stream.WriteByte(value);
        }

#if !NETSTANDARD2_0
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckDisposed();
            stream.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            CheckDisposed();
            return stream.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            CheckDisposed();
            return stream.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            return stream.ReadAsync(buffer, cancellationToken);
        }
#endif
    }
}