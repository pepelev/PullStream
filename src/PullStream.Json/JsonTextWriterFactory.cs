namespace PullStream.Json
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    public sealed class JsonTextWriterFactory : JsonWriterFactory
    {
        private readonly int bufferSize;
        private readonly Configuration configuration;

        public JsonTextWriterFactory()
            : this(1024, Configuration.Default)
        {
        }

        public JsonTextWriterFactory(int bufferSize, Configuration configuration)
        {
            this.bufferSize = bufferSize;
            this.configuration = configuration;
        }

        public override JsonWriter Create(Stream stream)
        {
            var output = new JsonTextWriter(
                new StreamWriter(
                    stream,
                    Encoding.UTF8,
                    leaveOpen: false,
                    bufferSize: bufferSize
                )
            );
            configuration.Apply(output);
            return output;
        }
    }
}