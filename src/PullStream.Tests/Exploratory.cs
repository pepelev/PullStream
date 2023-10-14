using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests;

public sealed class Exploratory
{
    [Test]
    public void Memory_Stream_Allows_To_Read_Zero_Bytes()
    {
        var stream = new MemoryStream();
        stream.Write(new byte[] {1, 2, 3}, 0, 3);
        stream.Position = 0;
        var read = stream.Read(new byte[3], 0, 0);
        read.Should().Be(0);
    }
}