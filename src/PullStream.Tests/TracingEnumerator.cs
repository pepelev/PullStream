using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PullStream.Tests;

public sealed class TracingEnumerator<T> : IEnumerator<T>
{
    private static readonly IEnumerator<T> @default = Array.Empty<T>().AsEnumerable().GetEnumerator();

    public bool Disposed { get; private set; }
    public IEnumerator<T> Inner { get; set; } = @default;

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