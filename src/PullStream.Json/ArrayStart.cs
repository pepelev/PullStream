namespace PullStream.Json;

using Newtonsoft.Json;

public sealed class ArrayStart : OutputChunk<JsonWriter>
{
    public override void Write(JsonWriter output)
    {
        output.WriteStartArray();
    }
}