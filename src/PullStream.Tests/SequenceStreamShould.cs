using System;
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
            using var reader = new StreamReader(stream);
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
            using var reader = new StreamReader(stream);
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
    }
}