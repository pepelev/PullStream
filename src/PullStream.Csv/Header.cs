using System;
using CsvHelper;

namespace PullStream.Csv
{
    public sealed class Header : OutputChunk<CsvWriter>
    {
        private readonly Type type;

        public Header(Type type)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public override void Write(CsvWriter target)
        {
            target.WriteHeader(type);
        }
    }
}