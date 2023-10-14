namespace PullStream.Json;

using Newtonsoft.Json;

public sealed class ArrayEnd : OutputChunk<JsonWriter>
{
    public override void Write(JsonWriter output)
    {
        output.WriteEndArray();
    }
}