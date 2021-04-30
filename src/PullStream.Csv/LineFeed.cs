using CsvHelper;

namespace PullStream.Csv
{
    public sealed class LineFeed : CsvRow
    {
        private readonly CsvRow row;

        public LineFeed(CsvRow row)
        {
            this.row = row;
        }

        public override void Write(CsvWriter target)
        {
            row.Write(target);
            target.NextRecord();
        }
    }
}