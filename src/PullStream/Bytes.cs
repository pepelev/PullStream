using System;
using System.IO;

namespace PullStream
{
    public sealed class Bytes : OutputChunk<Stream>
    {
        private readonly byte[] array;
        private readonly int count;
        private readonly int offset;

        public Bytes(byte[] array)
            : this(array ?? throw new ArgumentNullException(nameof(array)), 0, array.Length)
        {
        }

        public Bytes(byte[] array, int offset, int count)
        {
            new ArrayFragmentValidation(
                (array, nameof(array)),
                (offset, nameof(offset)),
                (count, nameof(count))
            ).Run();
            this.array = array;
            this.offset = offset;
            this.count = count;
        }

        public override void Write(Stream output)
        {
            output.Write(array, offset, count);
        }
    }
}