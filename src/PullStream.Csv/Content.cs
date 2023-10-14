using System;
using CsvHelper;

namespace PullStream.Csv;

public sealed class Content<T> : OutputChunk<CsvWriter>
{
    private readonly T value;

    public Content(T value)
    {
        this.value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override void Write(CsvWriter target)
    {
        target.WriteRecord(value);
    }
}