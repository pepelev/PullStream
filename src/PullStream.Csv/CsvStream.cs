using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace PullStream.Csv
{
    public static class CsvStream
    {
        public static SequenceStream.Builder<CsvRow, CsvWriter> Of<T>(
            IEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine = LastNewLine.No) => SequenceStream.Using(
                stream => CreateCsv(stream, configuration)
            )
            .On(
                Rows(sequence, configuration, lastNewLine)
            );

        public static SequenceStream.AsyncBuilder<CsvRow, CsvWriter> Of<T>(
            IAsyncEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine = LastNewLine.No) => SequenceStream.Using(
                stream => CreateCsv(stream, configuration)
            )
            .On(
                Rows(sequence, configuration, lastNewLine)
            );

        private static CsvWriter CreateCsv(Stream stream, CsvConfiguration configuration) => new(
            new StreamWriter(
                stream,
                configuration.Encoding,
                leaveOpen: false,
                bufferSize: configuration.BufferSize
            ),
            configuration
        );

        private static IEnumerable<CsvRow> Rows<T>(
            IEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine)
        {
            var data = sequence.Select(item => new Content<T>(item) as CsvRow);
            var content = configuration.HasHeaderRecord
                ? data.Prepend(new Header(typeof(T)))
                : data;

            if (lastNewLine == LastNewLine.Yes)
            {
                return content.Select(item => new LineFeed(item) as CsvRow);
            }

            return content.WithItemKind().Select(pair => pair.Kind.IsLast()
                ? pair.Item
                : new LineFeed(pair.Item)
            );
        }

        private static IAsyncEnumerable<CsvRow> Rows<T>(
            IAsyncEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine)
        {
            var data = sequence.Select(item => new Content<T>(item) as CsvRow);
            var content = configuration.HasHeaderRecord
                ? data.Prepend(new Header(typeof(T)))
                : data;

            if (lastNewLine == LastNewLine.Yes)
            {
                return content.Select(item => new LineFeed(item) as CsvRow);
            }

            return content.WithItemKind().Select(pair => pair.Kind.IsLast()
                ? pair.Item
                : new LineFeed(pair.Item)
            );
        }

        public static Stream Build(this SequenceStream.Builder<CsvRow, CsvWriter> builder) =>
            builder.Writing(Write);

        public static Stream Build(this SequenceStream.AsyncBuilder<CsvRow, CsvWriter> builder) =>
            builder.Writing(Write);

        private static void Write(CsvWriter output, CsvRow row)
        {
            row.Write(output);
        }
    }
}