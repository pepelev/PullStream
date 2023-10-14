using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace PullStream.Tests;

public sealed class StreamFixtures : IEnumerable<TestFixtureData>
{
    public IEnumerator<TestFixtureData> GetEnumerator()
    {
        yield return new TestFixtureData(
            new SyncAndAsyncMixingStreamFixture(
                new SyncStreamFixture()
            )
        ).SetArgDisplayNames("Sync");

        yield return new TestFixtureData(
            new SyncAndAsyncMixingStreamFixture(
                new AsyncStreamFixture()
            )
        ).SetArgDisplayNames("Async");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}