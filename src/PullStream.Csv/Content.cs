using CsvHelper;

namespace PullStream.Csv
{
    public sealed class Content<T> : CsvRow
    {
        private readonly T value;

        public Content(T value)
        {
            this.value = value;
        }

        public override void Write(CsvWriter target)
        {
            target.WriteRecord(value);
        }
    }
}