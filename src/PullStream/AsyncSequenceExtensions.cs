using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PullStream
{
    public static class AsyncSequenceExtensions
    {
        public static IAsyncEnumerable<Item<T>> AsItems<T>(this IAsyncEnumerable<T> sequence)
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

        public static IAsyncEnumerable<(ItemKind Kind, T Item)> WithItemKind<T>(this IAsyncEnumerable<T> sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            return sequence.WithItemKindYield();
        }

        private static async IAsyncEnumerable<(ItemKind Kind, T Item)> WithItemKindYield<T>(
            this IAsyncEnumerable<T> sequence,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await using var enumerator = sequence.WithCancellation(token).GetAsyncEnumerator();
            if (!await enumerator.MoveNextAsync())
            {
                yield break;
            }

            var current = enumerator.Current;
            if (!await enumerator.MoveNextAsync())
            {
                yield return (ItemKind.Single, current);
                yield break;
            }

            yield return (ItemKind.First, current);
            current = enumerator.Current;

            while (await enumerator.MoveNextAsync())
            {
                yield return (ItemKind.Middle, current);
                current = enumerator.Current;
            }

            yield return (ItemKind.Last, current);
        }

        public static IAsyncEnumerable<(int Index, T Item)> Indexed<T>(this IAsyncEnumerable<T> sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            return sequence.Select((item, index) => (index, item));
        }

        internal static async IAsyncEnumerable<ArraySegment<byte>> Chunks(
            this IAsyncEnumerable<Stream> streams,
            ArrayPool<byte> pool,
            int size,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            var buffer = pool.Rent(size);
            try
            {
                await foreach (var stream in streams.WithCancellation(token))
                {
#if !NETSTANDARD2_0
                    await
#endif
                    using (stream)
                    {
                        while (true)
                        {
                            var read = await stream.ReadAsync(buffer, 0, size, token);
                            if (read <= 0)
                            {
                                break;
                            }

                            yield return new ArraySegment<byte>(buffer, 0, read);
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
}