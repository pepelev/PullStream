using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace PullStream.Tests
{
    [TestFixtureSource(typeof(StreamFixtures))]
    public sealed class SequenceStreamShould
    {
        private readonly IStreamFixture fixture;

        public SequenceStreamShould(IStreamFixture fixture)
        {
            this.fixture = fixture;
        }

        [Test]
        public void Pass_Items_In_Sequence_Order()
        {
            var write = Substitute.For<Action<StreamWriter, string>>();

            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                new[] { "Dog", "Cat", "Sparrow" },
                write
            );

            using var reader = new StreamReader(stream, Encoding.UTF8);
            reader.ReadToEnd();

            Received.InOrder(
                () =>
                {
                    write(Arg.Any<StreamWriter>(), "Dog");
                    write(Arg.Any<StreamWriter>(), "Cat");
                    write(Arg.Any<StreamWriter>(), "Sparrow");
                }
            );
        }

        [Test]
        public void Be_Empty_When_No_Item_Outputs_Itself()
        {
            using var stream = fixture.Create(
                output => new StreamWriter(output, new UTF8Encoding(false)),
                new[] { "Dog", "Cat", "Sparrow" },
                (_, _) => { }
            );
            stream.ReadByte().Should().Be(-1);
        }

        [Test]
        public void Write_Items()
        {
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                new[] {"Dog", "Cat", "Sparrow"}.WithItemKind(),
                (output, value) =>
                {
                    var (kind, animal) = value;
                    output.Write(animal);
                    if (!kind.IsLast())
                    {
                        output.WriteLine();
                    }
                }
            );
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            result.Should().Be($"Dog{Environment.NewLine}Cat{Environment.NewLine}Sparrow");
        }

        [Test]
        public void Dispose_Enumerator_When_Read_To_End()
        {
            var sequence = new TracingSequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                sequence,
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                }
            );
            using var reader = new StreamReader(stream, Encoding.UTF8);
            reader.ReadToEnd();

            sequence.Enumerator.Disposed.Should().BeTrue();
        }

        [Test]
        public void Dispose_Enumerator_When_Stream_Disposed()
        {
            var sequence = new TracingSequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                sequence,
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                }
            );

            stream.Dispose();

            (!sequence.EnumeratorRequested || sequence.Enumerator.Disposed).Should().BeTrue();
        }

        [Test]
        public void Dispose_Enumerator_When_Stream_Read_And_Disposed()
        {
            var sequence = new TracingSequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                sequence,
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                    output.Flush();
                }
            );

            stream.ReadByte();
            stream.Dispose();

            sequence.Enumerator.Disposed.Should().BeTrue();
        }

        [Test]
        public void Not_Create_Context_When_Stream_Not_Read_And_Disposed()
        {
            var factory = Substitute.For<Func<Stream, StreamWriter>>();
            factory.Invoke(Arg.Any<Stream>()).Throws(new Exception("Should not be called"));

            var stream = fixture.Create(
                factory,
                new[] { "Dog", "Cat", "Sparrow" },
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                }
            );

            stream.Dispose();

            factory.DidNotReceive().Invoke(Arg.Any<Stream>());
        }

        [Test]
        public void Dispose_Context_When_Stream_Read_Disposed()
        {
            var dispose = Substitute.For<Action<StreamWriter>>();
            var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                dispose,
                new[] { "Dog", "Cat", "Sparrow" },
                ArrayPool<byte>.Shared,
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                    output.Flush();
                }
            );

            stream.ReadByte();
            stream.Dispose();

            dispose.Received(1).Invoke(Arg.Any<StreamWriter>());
        }

        [Test]
        public void Not_Write_All_Items_On_Read()
        {
            var writeCalls = 0;

            var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                new[] {"Dog", "Cat", "Sparrow"},
                (output, animal) =>
                {
                    output.Write(animal);
                    output.WriteLine();
                    output.Flush();
                    writeCalls++;
                }
            );

            stream.ReadByte();
            writeCalls.Should().Be(1);
        }

        [Test]
        public void Use_Pool()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                Strings(15_000),
                pool,
                (output, animal) =>
                {
                    output.WriteLine(animal);
                }
            );
            using var reader = new StreamReader(stream, Encoding.UTF8);
            reader.ReadToEnd();

            pool.Operations.Should().BePositive();
        }

        [Test]
        public void Return_All_Arrays_To_Pool_When_Read_To_End()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                Strings(15_000),
                pool,
                (output, animal) =>
                {
                    output.WriteLine(animal);
                }
            );
            using var reader = new StreamReader(stream, Encoding.UTF8);
            reader.ReadToEnd();

            pool.HasCurrentRentArrays.Should().BeFalse();
        }

        [Test]
        public void Return_All_Arrays_To_Pool_When_Stream_Read_And_Disposed()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                Strings(15_000),
                pool,
                (output, animal) =>
                {
                    output.WriteLine(animal);
                }
            );
            using var reader = new StreamReader(stream, Encoding.UTF8);
            reader.ReadLine();

            stream.Dispose();

            pool.HasCurrentRentArrays.Should().BeFalse();
        }

        [Test]
        public void Contain_Items_In_Long_Run()
        {
            var items = Strings(15_000).AsItems().ToList();
            using var stream = fixture.Create(
                output => new StreamWriter(output, Encoding.UTF8),
                items,
                (output, item) =>
                {
                    var (_, kind, value) = item;
                    if (kind.IsLast())
                    {
                        output.Write(value);
                    }
                    else
                    {
                        output.WriteLine(value);
                    }
                }
            );
            var expectation = string.Join(Environment.NewLine, items.Select(item => item.Value));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var actualChars = new char[expectation.Length];
            var offset = 0;

            while (offset < actualChars.Length)
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                var length = Math.Min(actualChars.Length - offset, random.Next(1, 250));
                reader.Read(actualChars, offset, length);
                offset += length;
            }

            var actual = new string(actualChars);
            actual.Should().Be(expectation);
        }

        private static IEnumerable<string> Strings(int countHint)
        {
            var random = new Random();
            var count = random.Next(countHint / 2, countHint * 3 / 2);
            for (var i = 0; i < count; i++)
            {
                var length = random.Next(1, 150);
                var line = new StringBuilder(length);
                for (var j = 0; j < length; j++)
                {
                    line.Append(
                        (char) (random.Next('z' - 'a' + 1) + 'a')
                    );
                }

                yield return line.ToString();
            }
        }
    }
}