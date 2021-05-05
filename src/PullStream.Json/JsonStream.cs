namespace PullStream.Json
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Newtonsoft.Json;

    public static class JsonStream
    {
        [Pure]
        public static SequenceStream.Builder<JsonWriter> Using(JsonWriterFactory factory) =>
            SequenceStream.Using(factory.Create);

        [Pure]
        public static SequenceStream.Builder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            IEnumerable<T> sequence)
            => Using(new JsonTextWriterFactory()).ArrayOf(sequence);

        [Pure]
        public static SequenceStream.Builder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            this SequenceStream.Builder<JsonWriter> builder,
            IEnumerable<T> sequence)
            => builder.ArrayOf(sequence, JsonSerializer.Create());

        [Pure]
        public static SequenceStream.Builder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            this SequenceStream.Builder<JsonWriter> builder,
            IEnumerable<T> sequence,
            JsonSerializer format)
        {
            var chunks = sequence
                .Select(value => new Item<T>(value, format) as OutputChunk<JsonWriter>)
                .Prepend(new ArrayStart())
                .Append(new ArrayEnd());

            return builder.On(chunks);
        }

        [Pure]
        public static SequenceStream.AsyncBuilder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            IAsyncEnumerable<T> sequence)
            => Using(new JsonTextWriterFactory()).ArrayOf(sequence);

        [Pure]
        public static SequenceStream.AsyncBuilder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            this SequenceStream.Builder<JsonWriter> builder,
            IAsyncEnumerable<T> sequence)
            => builder.ArrayOf(sequence, JsonSerializer.Create());

        [Pure]
        public static SequenceStream.AsyncBuilder<OutputChunk<JsonWriter>, JsonWriter> ArrayOf<T>(
            this SequenceStream.Builder<JsonWriter> builder,
            IAsyncEnumerable<T> sequence,
            JsonSerializer format)
        {
            var chunks = sequence
                .Select(value => new Item<T>(value, format) as OutputChunk<JsonWriter>)
                .Prepend(new ArrayStart())
                .Append(new ArrayEnd());

            return builder.On(chunks);
        }
    }
}