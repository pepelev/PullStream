namespace PullStream.Json
{
    using System.IO;
    using Newtonsoft.Json;

    public abstract class JsonWriterFactory
    {
        public abstract JsonWriter Create(Stream stream);
    }
}