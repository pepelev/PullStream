using System.Collections.Generic;
using System.Linq;

namespace ClassLibrary1
{
    public static class SequenceExtensions
    {
        public static IEnumerable<Item<T>> WithItemInfo<T>(this IEnumerable<T> sequence) =>
            sequence.WithItemKind().Indexed().Select(
                triple =>
                {
                    var (index, (kind, item)) = triple;
                    return new Item<T>(index, kind, item);
                }
            );

        public static IEnumerable<(ItemKind Kind, T Item)> WithItemKind<T>(this IEnumerable<T> sequence)
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

        public static IEnumerable<(int Index, T Item)> Indexed<T>(this IEnumerable<T> sequence) =>
            sequence.Select((item, index) => (index, item));
    }
}