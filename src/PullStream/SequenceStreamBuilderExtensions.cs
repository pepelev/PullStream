using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace PullStream
{
    public static class SequenceStreamBuilderExtensions
    {
        [Pure]
        public static Stream Build<Chunk, Context>(this SequenceStream.Builder<Chunk, Context> builder)
            where Chunk : OutputChunk<Context>
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Writing(Write);
        }

        [Pure]
        public static Stream Build<Chunk, Context>(this SequenceStream.AsyncBuilder<Chunk, Context> builder)
            where Chunk : OutputChunk<Context>
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Writing(Write);
        }

        private static void Write<Context>(Context output, OutputChunk<Context> chunk)
        {
            chunk.Write(output);
        }
    }
}