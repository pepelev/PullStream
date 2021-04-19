using System;
using System.Collections.Generic;
using System.IO;

namespace ClassLibrary1
{
    public sealed class LazyStream<T> : Stream
    {
        private readonly CircularBuffer buffer = new();
        private readonly IEnumerator<T> enumerator;

        private readonly Action<Stream, T> write;
        private State state = State.MoveNext;

        public LazyStream(Action<Stream, T> write, IEnumerable<T> enumerator)
        {
            this.write = write;
            this.enumerator = enumerator.GetEnumerator();
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
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] destination, int offset, int count)
        {
            while (state != State.Enumerated && buffer.BytesReady < count)
            {
                if (state == State.MoveNext)
                {
                    if (!enumerator.MoveNext())
                    {
                        state = State.Enumerated;
                        continue;
                    }

                    state = State.Current;
                }
                else if (state == State.Current)
                {
                    using var stream = buffer.BeginWrite();
                    write(stream, enumerator.Current);
                    stream.Commit();
                    state = State.MoveNext;
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
            Enumerated
        }
    }
}