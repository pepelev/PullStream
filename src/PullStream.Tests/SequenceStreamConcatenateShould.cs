using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests
{
    public sealed class SequenceStreamConcatenateShould
    {
        [Test]
        public void Read_Piece()
        {
            using var concatenation = SequenceStream.Concatenation(
                new Stream[]
                {
                    new MemoryStream(new byte[] {1, 2, 3}),
                    new MemoryStream(new byte[] {7, 40, 80, 78})
                },
                ArrayPool<byte>.Shared,
                2
            );
            var buffer = new byte[5];
            concatenation.Read(buffer, 0, 5);

            buffer.Should().Equal(1, 2, 3, 7, 40);
        }

        [Test]
        public void Read_Entire_Content()
        {
            using var concatenation = SequenceStream.Concatenation(
                new Stream[]
                {
                    new MemoryStream(new byte[] {1, 2, 3}),
                    new MemoryStream(new byte[] {7, 40, 80, 78})
                },
                ArrayPool<byte>.Shared,
                3
            );

            var buffer = new byte[100];
            var read = concatenation.Read(buffer, 0, 100);
            read.Should().Be(7);
            buffer.Take(read).Should().Equal(1, 2, 3, 7, 40, 80, 78);
        }

        [Test]
        public void Dispose_Used_Streams_On_Dispose([Range(1, 5)] int chunkSize, [Values(1, 3, 5, 7, 10, 12, 17)] int read)
        {
            var streams = new[]
            {
                new Disposable(new MemoryStream(new byte[] {7, 40, 80, 78})),
                new Disposable(new MemoryStream(new byte[] {20, 40, 70})),
                new Disposable(new MemoryStream(new byte[] {17, 50, 233, 89, 54}))
            };
            using (var concatenation = SequenceStream.Concatenation(streams, ArrayPool<byte>.Shared, chunkSize))
            {
                concatenation.Read(new byte[read], 0, read);
            }

            streams.Should().OnlyContain(
                stream => stream.Position == 0 || stream.Disposed
            );
        }
        [Test]
        public async Task Read_Async()
        {
            using var concatenation = SequenceStream.Concatenation(
                new Stream[]
                {
                    new MemoryStream(new byte[] {1, 2, 3}),
                    new MemoryStream(new byte[] {7, 40, 80, 78})
                }.ToAsyncEnumerable(),
                ArrayPool<byte>.Shared,
                2
            );
            var buffer = new byte[5];
            await concatenation.ReadAsync(buffer, 0, 5);

            buffer.Should().Equal(1, 2, 3, 7, 40);
        }
    }
}