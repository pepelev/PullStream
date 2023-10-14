using System;
using CsvHelper;

namespace PullStream.Csv;

public sealed class LineFeed : OutputChunk<CsvWriter>
{
    private readonly OutputChunk<CsvWriter> row;

    public LineFeed(OutputChunk<CsvWriter> row)
    {
        this.row = row ?? throw new ArgumentNullException(nameof(row));
    }

    public override void Write(CsvWriter target)
    {
        row.Write(target);
        target.NextRecord();
    }
}