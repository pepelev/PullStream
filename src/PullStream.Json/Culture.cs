namespace PullStream.Json;

using System.Globalization;
using Newtonsoft.Json;

public sealed class Culture : Configuration
{
    private readonly CultureInfo value;

    public Culture(CultureInfo value)
    {
        this.value = value;
    }

    public override void Apply(JsonTextWriter target)
    {
        target.Culture = value;
    }
}