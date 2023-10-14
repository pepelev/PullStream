using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace PullStream;

public static class SequenceExtensions
{
    [Pure]
    public static IEnumerable<Item<T>> AsItems<T>(this IEnumerable<T> sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(nameof(sequence));
        }

        return sequence.WithItemKind().Indexed().Select(
            triple =>
            {
                var (index, (kind, item)) = triple;
                return new Item<T>(index, kind, item);
            }
        );
    }

    [Pure]
    public static IEnumerable<(ItemKind Kind, T Item)> WithItemKind<T>(this IEnumerable<T> sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(nameof(sequence));
        }

        return Yield();

        IEnumerable<(ItemKind Kind, T Item)> Yield()
        {
            using var enumerator = sequence.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            var current = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                yield return (ItemKind.Single, current);
                yield break;
            }

            yield return (ItemKind.First, current);
            current = enumerator.Current;

            while (enumerator.MoveNext())
            {
                yield return (ItemKind.Middle, current);
                current = enumerator.Current;
            }

            yield return (ItemKind.Last, current);
        }
    }

    [Pure]
    public static IEnumerable<(int Index, T Item)> Indexed<T>(this IEnumerable<T> sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(nameof(sequence));
        }

        return sequence.Select((item, index) => (index, item));
    }

    [Pure]
    internal static IEnumerable<Bytes> Chunks(
        this IEnumerable<Stream> streams,
        ArrayPool<byte> pool,
        int size)
    {
        var buffer = pool.Rent(size);
        try
        {
            foreach (var stream in streams)
            {
                using (stream)
                {
                    while (true)
                    {
                        var read = stream.Read(buffer, 0, size);
                        if (read <= 0)
                        {
                            break;
                        }

                        yield return new Bytes(buffer, 0, read);
                    }
                }
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}