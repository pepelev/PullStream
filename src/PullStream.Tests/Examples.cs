using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PullStream.Tests
{
    public static class Examples
    {
        public static Stream Strings(IEnumerable<string> strings) =>
            SequenceStream.FromStrings(strings, Encoding.UTF8, Environment.NewLine);

        public static Stream Bytes(IEnumerable<byte[]> chunks) =>
            SequenceStream.UsingStream()
                .On(chunks)
                .Writing(
                    (stream, bytes) => { stream.Write(bytes, 0, bytes.Length); }
                );

        public static Stream BinaryContext(IEnumerable<Person> persons) =>
            SequenceStream.Using(
                    stream => new BinaryWriter(stream, Encoding.UTF8)
                )
                .On(persons)
                .Writing(
                    (binaryWriter, person) =>
                    {
                        binaryWriter.Write(person.Name);
                        binaryWriter.Write(person.Age);
                    }
                );

        public static void ItemMetaInformation(IEnumerable<string> names)
        {
            var enrichedNames = names.AsItems();

            foreach (var (index, kind, name) in enrichedNames)
            {
                if (kind.IsFirst())
                {
                    Console.WriteLine("Names");
                }
                Console.Write($"{index}: {name}");
                if (!kind.IsLast())
                {
                    Console.WriteLine();
                }
            }
        }

        public static Stream ItemMetaInformationOnBuilder(IEnumerable<string> names)
        {
            return SequenceStream.Using(
                    stream => new StreamWriter(stream, Encoding.UTF8)
                )
                .On(names)
                .AsItems()
                .Writing(
                    (writer, item) =>
                    {
                        if (item.Kind.IsFirst())
                        {
                            writer.WriteLine("Names");
                        }

                        writer.Write($"{item.Index}: {item.Value}");
                        if (!item.Kind.IsLast())
                        {
                            writer.WriteLine();
                        }
                    }
                );
        }

        public class Person
        {
            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public string Name { get; }
            public int Age { get; }
        }
    }
}