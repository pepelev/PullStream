using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Tests;

public sealed class AsyncSequenceStreamShould
{
    [Test]
    public void Test()
    {
        using var source = new CancellationTokenSource();
        var cancelled = false;
        using var strings = SequenceStream.Using(stream => new StreamWriter(stream, Encoding.UTF8))
            .On(Yield())
            .WithCancellation(source.Token)
            .Writing(
                (output, @string) => output.Write(@string)
            );
        using var streamReader = new StreamReader(strings);

        Assert.ThrowsAsync<TaskCanceledException>(
            streamReader.ReadToEndAsync
        );
        cancelled.Should().BeTrue();

        async IAsyncEnumerable<string> Yield(
#pragma warning disable 8424
            [EnumeratorCancellation]
#pragma warning restore 8424
            CancellationToken token = default)
        {
            yield return "One";
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            yield return "Two";
            source.Cancel();
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                throw;
            }
        }
    }
}