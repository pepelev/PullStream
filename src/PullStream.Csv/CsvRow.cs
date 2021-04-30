using CsvHelper;

namespace PullStream.Csv
{
    public abstract class CsvRow
    {
        public abstract void Write(CsvWriter target);
    }
}