using System;
using CsvHelper;

namespace PullStream.Csv
{
    public sealed class LineFeed : CsvRow
    {
        private readonly CsvRow row;

        public LineFeed(CsvRow row)
        {
            this.row = row ?? throw new ArgumentNullException(nameof(row));
        }

        public override void Write(CsvWriter target)
        {
            row.Write(target);
            target.NextRecord();
        }
    }
}