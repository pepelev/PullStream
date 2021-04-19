using System.Collections.Generic;
using System.Linq;

namespace PullStream
{
    public static class AsyncSequenceExtensions
    {
        public static IAsyncEnumerable<Item<T>> AsItems<T>(this IAsyncEnumerable<T> sequence) =>
            sequence.WithItemKind().Indexed().Select(
                triple =>
                {
                    var (index, (kind, item)) = triple;
                    return new Item<T>(index, kind, item);
                }
            );

        public static async IAsyncEnumerable<(ItemKind Kind, T Item)> WithItemKind<T>(this IAsyncEnumerable<T> sequence)
        {
            await using var enumerator = sequence.GetAsyncEnumerator();
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

        public static IAsyncEnumerable<(int Index, T Item)> Indexed<T>(this IAsyncEnumerable<T> sequence) =>
            sequence.Select((item, index) => (index, item));
    }
}