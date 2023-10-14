using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests;

public sealed class SequenceStreamBuilderShould
{
    [Test]
    public void Wrap_Original_Stream_On_Over()
    {
        var sut = SequenceStream.Using(
            stream => new StreamWriter(
                stream,
                Encoding.UTF8,
                leaveOpen: false,
                bufferSize: 128
            )
        );

        var compressed = sut.Over(
            stream => new GZipStream(
                stream,
                leaveOpen: false,
                compressionLevel: CompressionLevel.Fastest
            )
        );

        using var wordsStream = compressed
            .On(new[] {"Read", "the", "content"})
            .Writing((output, word) => output.Write($"{word} "));

        using var reader = new StreamReader(
            new GZipStream(wordsStream, CompressionMode.Decompress),
            Encoding.UTF8
        );
        var result = reader.ReadToEnd();
        result.Should().Be("Read the content ");
    }
}