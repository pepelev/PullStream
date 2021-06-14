using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace PullStream.Csv
{
    public static class CsvStream
    {
        private static readonly List<LastNewLine> validLastNewLineValues = new(2)
        {
            LastNewLine.No,
            LastNewLine.Yes
        };

        [Pure]
        public static SequenceStream.Builder<CsvRow, CsvWriter> Of<T>(
            IEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine = LastNewLine.No)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ValidateLastNewLine(lastNewLine, nameof(lastNewLine));
            return SequenceStream.Using(
                    stream => CreateCsv(stream, configuration)
                )
                .On(
                    Rows(sequence, configuration, lastNewLine)
                );
        }

        [Pure]
        public static SequenceStream.AsyncBuilder<CsvRow, CsvWriter> Of<T>(
            IAsyncEnumerable<T> sequence,
            CsvConfiguration configuration,
            LastNewLine lastNewLine = LastNewLine.No)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ValidateLastNewLine(lastNewLine, nameof(lastNewLine));
            return SequenceStream.Using(
                    stream => CreateCsv(stream, configuration)
                )
                .On(
                    Rows(sequence, configuration, lastNewLine)
                );
        }

        private static void ValidateLastNewLine(LastNewLine lastNewLine, string name)
        {
            if (validLastNewLineValues.Contains(lastNewLine))
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                name,
                lastNewLine,
                $"Value must be one of [{string.Join(", ", validLastNewLineValues)}]"
            );
        }

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

        [Pure]
        public static Stream Build(this SequenceStream.Builder<CsvRow, CsvWriter> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Writing(Write);
        }

        [Pure]
        public static Stream Build(this SequenceStream.AsyncBuilder<CsvRow, CsvWriter> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Writing(Write);
        }

        private static void Write(CsvWriter output, CsvRow row)
        {
            row.Write(output);
        }
    }
}