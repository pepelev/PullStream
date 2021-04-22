using System;
using System.Collections.Generic;
using System.Linq;

namespace PullStream
{
    public static class SequenceExtensions
    {
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

        public static IEnumerable<(int Index, T Item)> Indexed<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            return sequence.Select((item, index) => (index, item));
        }
    }
}