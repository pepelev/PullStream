using System;
using System.Buffers;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests;

public sealed class CircularBufferShould
{
    private const int kb = 1024;

    [Test]
    public void Have_No_Ready_Bytes_When_Created()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.BytesReady.Should().Be(0);
    }

    [Test]
    public void Have_No_Bytes_Cut_When_Created()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.BytesCut.Should().Be(0);
    }

    [Test]
    public void Increase_Bytes_Ready_On_Write([Values(1, 2, 3)] int count)
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {0, 1, 2}, 0, count);

        sut.BytesReady.Should().Be(count);
    }

    [Test]
    public void Read_What_Written()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {5, 6, 8}, 0, 3);

        var result = new byte[3];
        sut.Read(new Span<byte>(result));

        result.Should().Equal(5, 6, 8);
    }

    [Test]
    public void Read_Part_Of_Written()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8}, 0, 4);

        var result = new byte[2];
        sut.Read(new Span<byte>(result));

        result.Should().Equal(7, 5);
    }

    [Test]
    public void Throw_On_Read_More_Than_Bytes_Ready([Values(5, 7, 42)] int count)
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8}, 0, 4);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sut.Read(new Span<byte>(new byte[count]))
        );
    }

    [Test]
    public void Update_Bytes_Cut_And_Bytes_Ready_On_Cut()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8, 17, 93}, 0, 6);

        sut.Cut(2);

        (sut.BytesReady, sut.BytesCut).Should().Be(
            (4, 2)
        );
    }

    [Test]
    public void Drop_On_Cut()
    {
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8}, 0, 4);

        sut.Cut(2);

        var result = new byte[2];
        sut.Read(new Span<byte>(result));

        result.Should().Equal(6, 8);
    }

    [Test]
    public void Use_Pool()
    {
        var pool = new RememberingPool(ArrayPool<byte>.Shared);
        var sut = new CircularBuffer(pool);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8}, 0, 4);

        pool.Operations.Should().BePositive();
    }

    [Test]
    public void Return_Arrays_To_Pool_On_Dispose()
    {
        var pool = new RememberingPool(ArrayPool<byte>.Shared);
        var sut = new CircularBuffer(pool);

        sut.WriteStream.Write(new byte[] {7, 5, 6, 8}, 0, 4);

        sut.Dispose();

        pool.HasCurrentRentArrays.Should().BeFalse();
    }

    [Test]
    public void Read_What_Written_In_Long_Run()
    {
        var seed = Guid.NewGuid().GetHashCode();
        Console.WriteLine($"Seed is {seed}");
        var random = new Random(seed);
        var input = random.NextBytes(kb * kb, 100 * kb * kb);
        var sut = new CircularBuffer(ArrayPool<byte>.Shared);

        var output = RanRandomOperations(sut, input, random);

        output.SequenceEqual(input).Should().BeTrue();
    }

    [Test]
    public void Return_Rent_Arrays_In_Pool()
    {
        var seed = Guid.NewGuid().GetHashCode();
        Console.WriteLine($"Seed is {seed}");
        var random = new Random(seed);
        var input = random.NextBytes(kb * kb, 100 * kb * kb);
        var pool = new RememberingPool(ArrayPool<byte>.Shared);
        using (var sut = new CircularBuffer(pool))
        {
            RanRandomOperations(sut, input, random);
        }

        pool.HasCurrentRentArrays.Should().BeFalse();
    }

    private static byte[] RanRandomOperations(CircularBuffer sut, byte[] input, Random random)
    {
        var output = new byte[input.Length];
        var writeOffset = 0;
        var stream = sut.WriteStream;
        while (writeOffset < input.Length || sut.BytesReady > 0)
        {
            var operation = writeOffset == input.Length || random.Next(2) == 0
                ? Operation.Read
                : Operation.Write;

            if (operation == Operation.Write)
            {
                var length = Math.Min(input.Length - writeOffset, random.Next(1, 100 * kb));
                stream.Write(input, writeOffset, length);
                writeOffset += length;
            }
            else
            {
                var length = Math.Min(sut.BytesReady, random.Next(1, 100 * kb));
                sut.Read(output.AsSpan((int) sut.BytesCut, length));
                sut.Cut(length);
            }
        }

        return output;
    }

    private enum Operation
    {
        Read,
        Write
    }
}