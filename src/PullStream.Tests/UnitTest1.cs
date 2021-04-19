using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClassLibrary1;
using CsvHelper;
using CsvHelper.Configuration;
using NUnit.Framework;

namespace LazyStream.Test
{
    public class Tests
    {
        //[Test]
        public void Test1()
        {
            var dosya = 0;
            var result = new LazyStream<(ItemKind Kind, Person Person)>(
                (stream, item) =>
                {
                    using var writer = new CsvWriter(
                        new StreamWriter(
                            stream,
                            new UTF8Encoding(false),
                            32
                        ),
                        new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            BufferSize = 4
                        }
                    );
                    var (kind, person) = item;
                    if (kind == ItemKind.First)
                    {
                        writer.WriteHeader<Person>();
                        writer.NextRecord();
                    }

                    writer.WriteRecord(person);
                    writer.NextRecord();
                    dosya++;
                },
                Enumerable.Repeat(
                    new Person("Alice", 21),
                    100_000
                ).WithItemKind()
            );

            using var destination = new FileStream("D:/out.txt", FileMode.Create);
            result.CopyTo(destination);
        }

        [Test]
        public void Test2()
        {
            using var stream = PullStream.Using(
                    output => new CsvWriter(
                        new StreamWriter(
                            output,
                            Encoding.UTF8
                        ),
                        new CsvConfiguration(CultureInfo.InvariantCulture)
                    )
                )
                .On(
                    Enumerable.Repeat(
                        new Person("Alice", 21),
                        10_000_000
                    )
                )
                .WithItemKind()
                .Writing(
                    (csv, item) =>
                    {
                        var (kind, person) = item;
                        if (kind.IsFirst())
                        {
                            csv.WriteHeader<Person>();
                            csv.NextRecord();
                        }

                        csv.WriteRecord(person);
                        csv.NextRecord();
                    }
                );


            using var destination = new FileStream("D:/out-2.txt", FileMode.Create);
            stream.CopyTo(destination);
        }

        [Test]
        public void Test()
        {
            using var writer = new CsvWriter(
                new StreamWriter(
                    new FileStream("D:/out.txt", FileMode.Create),
                    new UTF8Encoding(false)
                ),
                new CsvConfiguration(CultureInfo.InvariantCulture)
            );

            foreach (var (kind, person) in Enumerable.Repeat(
                new Person("Alice", 21),
                100_000
            ).WithItemKind())
            {
                if (kind == ItemKind.First)
                {
                    writer.WriteHeader<Person>();
                    writer.NextRecord();
                }

                writer.WriteRecord(person);
                writer.NextRecord();
                //writer.Flush();
            }
        }

        public record Person(string Name, int Age);
    }
}