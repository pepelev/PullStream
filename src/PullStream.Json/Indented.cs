namespace PullStream.Json;

using Newtonsoft.Json;

public sealed class Indented : Configuration
{
    private readonly char indentChar;
    private readonly int indentationSize;

    public Indented(int indentationSize = 2, char indentChar = ' ')
    {
        this.indentChar = indentChar;
        this.indentationSize = indentationSize;
    }

    public override void Apply(JsonTextWriter target)
    {
        target.Formatting = Formatting.Indented;
        target.IndentChar = indentChar;
        target.Indentation = indentationSize;
    }
}