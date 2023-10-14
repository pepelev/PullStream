namespace PullStream.Json;

using Newtonsoft.Json;

public sealed class Item<T> : OutputChunk<JsonWriter>
{
    private readonly JsonSerializer serializer;
    private readonly T value;

    public Item(T value, JsonSerializer serializer)
    {
        this.value = value;
        this.serializer = serializer;
    }

    public override void Write(JsonWriter output)
    {
        serializer.Serialize(output, value);
    }
}