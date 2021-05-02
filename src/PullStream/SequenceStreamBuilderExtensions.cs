using System.IO;

namespace PullStream
{
    public static class SequenceStreamBuilderExtensions
    {
        public static Stream Build<Chunk, Context>(this SequenceStream.Builder<Chunk, Context> builder)
            where Chunk : OutputChunk<Context> =>
            builder.Writing(Write);

        public static Stream Build<Chunk, Context>(this SequenceStream.AsyncBuilder<Chunk, Context> builder)
            where Chunk : OutputChunk<Context> =>
            builder.Writing(Write);

        private static void Write<Context>(Context output, OutputChunk<Context> chunk)
        {
            chunk.Write(output);
        }
    }
}