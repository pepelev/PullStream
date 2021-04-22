using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests
{
    public class SequenceStreamShould
    {
        [Test]
        public void Contain_Items()
        {
            using var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(new[] {"Dog", "Cat", "Sparrow"})
                .WithItemKind()
                .Writing(
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

        private sealed class Enumerator<T> : IEnumerator<T>
        {
            public bool Disposed { get; private set; }
            public IEnumerator<T> Inner { get; set; } = Array.Empty<T>().AsEnumerable().GetEnumerator();

            public bool MoveNext()
            {
                return Inner.MoveNext();
            }

            public void Reset() => Inner.Reset();

            public T Current => Inner.Current;
            object IEnumerator.Current => Current;

            public void Dispose()
            {
                try
                {
                    Inner.Dispose();
                }
                finally
                {
                    Disposed = true;
                }
            }
        }

        private sealed class DirtySequence<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> sequence;
            public Enumerator<T> Enumerator { get; } = new();

            public DirtySequence(IEnumerable<T> sequence)
            {
                this.sequence = sequence;
            }

            public IEnumerator<T> GetEnumerator()
            {
                var enumerator = sequence.GetEnumerator();
                Enumerator.Inner = enumerator;
                return Enumerator;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Test]
        public void Dispose_Enumerator_When_Read_To_End()
        {
            var sequence = new DirtySequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            using var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(sequence)
                .Writing(
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
            var sequence = new DirtySequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(sequence)
                .Writing(
                    (output, animal) =>
                    {
                        output.Write(animal);
                        output.WriteLine();
                    }
                );

            stream.Dispose();

            sequence.Enumerator.Disposed.Should().BeTrue();
        }

        [Test]
        public void Dispose_Enumerator_When_Stream_Read_Disposed()
        {
            var sequence = new DirtySequence<string>(new[] { "Dog", "Cat", "Sparrow" });
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(sequence)
                .Writing(
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
            var contextCreated = false;
            var stream = SequenceStream.Using(
                    output =>
                    {
                        var context = new StreamWriter(output, Encoding.UTF8);
                        contextCreated = true;
                        return context;
                    }
                )
                .On(new[] {"Dog", "Cat", "Sparrow"})
                .Writing(
                    (output, animal) =>
                    {
                        output.Write(animal);
                        output.WriteLine();
                    }
                );

            stream.Dispose();

            contextCreated.Should().BeFalse();
        }

        [Test]
        public void Dispose_Context_When_Stream_Read_Disposed()
        {
            var disposed = false;
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8),
                    output =>
                    {
                        output.Dispose();
                        disposed = true;
                    }
                )
                .On(new[] { "Dog", "Cat", "Sparrow" })
                .Writing(
                    (output, animal) =>
                    {
                        output.Write(animal);
                        output.WriteLine();
                        output.Flush();
                    }
                );

            stream.ReadByte();
            stream.Dispose();

            disposed.Should().BeTrue();
        }

        [Test]
        public void Not_Write_All_Items_On_Read()
        {
            var writeCalls = 0;
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(new[] { "Dog", "Cat", "Sparrow" })
                .Writing(
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

        private IEnumerable<string> Strings(int countHint)
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
                        (char) (random.Next('z' - 'a') + 'a')
                    );
                }

                yield return line.ToString();
            }
        }

        [Test]
        public void Use_Pool()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(Strings(15_000))
                .Pooling(pool)
                .Writing(
                    (output, animal) =>
                    {
                        output.WriteLine(animal);
                    }
                );
            new StreamReader(stream).ReadToEnd();

            pool.Operations.Should().BePositive();
        }

        [Test]
        public void Return_All_Arrays_To_Pool_When_Read_To_End()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(Strings(15_000))
                .Pooling(pool)
                .Writing(
                    (output, animal) =>
                    {
                        output.WriteLine(animal);
                    }
                );
            new StreamReader(stream, Encoding.UTF8).ReadToEnd();

            pool.HasCurrentRentArrays.Should().BeFalse();
        }

        [Test]
        public void Return_All_Arrays_To_Pool_When_Stream_Read_And_Disposed()
        {
            var pool = new RememberingPool(ArrayPool<byte>.Shared);
            var stream = SequenceStream.Using(
                    output => new StreamWriter(output, Encoding.UTF8)
                )
                .On(Strings(15_000))
                .Pooling(pool)
                .Writing(
                    (output, animal) =>
                    {
                        output.WriteLine(animal);
                    }
                );
            new StreamReader(stream, Encoding.UTF8).ReadLine();
            stream.Dispose();

            pool.HasCurrentRentArrays.Should().BeFalse();
        }
    }
}