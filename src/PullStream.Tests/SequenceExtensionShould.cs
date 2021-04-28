using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PullStream.Tests
{
    public sealed class SequenceExtensionShould
    {
        private static IEnumerable<TestCaseData> KindCases
        {
            get
            {
                yield return new TestCaseData(Array.Empty<int>())
                    .Returns(Array.Empty<(ItemKind Kind, int Value)>());

                yield return new TestCaseData(new[] {42})
                    .Returns(new[] {(ItemKind.Single, 42)});

                yield return new TestCaseData(new[] {37, 42})
                    .Returns(new[]
                        {
                            (ItemKind.First, 37),
                            (ItemKind.Last, 42)
                        }
                    );

                yield return new TestCaseData(new[] {32, 85, 48})
                    .Returns(new[]
                        {
                            (ItemKind.First, 32),
                            (ItemKind.Middle, 85),
                            (ItemKind.Last, 48)
                        }
                    );

                yield return new TestCaseData(new[] {93, 17, 84, -2, 76})
                    .Returns(new[]
                        {
                            (ItemKind.First, 93),
                            (ItemKind.Middle, 17),
                            (ItemKind.Middle, 84),
                            (ItemKind.Middle, -2),
                            (ItemKind.Last, 76)
                        }
                    );
            }
        }

        private static IEnumerable<TestCaseData> IndexCases
        {
            get
            {
                yield return new TestCaseData(Array.Empty<string>() as object)
                    .Returns(Array.Empty<(int Index, string Item)>());

                yield return new TestCaseData(new[] {"Dog"} as object)
                    .Returns(new[] {(0, "Dog")});

                yield return new TestCaseData(new[] {"Cat", "Dog"} as object)
                    .Returns(new[] {(0, "Cat"), (1, "Dog")});
            }
        }

        private static IEnumerable<TestCaseData> ItemCases
        {
            get
            {
                yield return new TestCaseData(Array.Empty<string>() as object)
                    .Returns(Array.Empty<Item<string>>());

                yield return new TestCaseData(new[] { "Dog" } as object)
                    .Returns(new[] { new Item<string>(0, ItemKind.Single, "Dog") });

                yield return new TestCaseData(new[] { "Cat", "Dog" } as object)
                    .Returns(new[]
                        {
                            new Item<string>(0, ItemKind.First, "Cat"),
                            new Item<string>(1, ItemKind.Last, "Dog")
                        }
                    );

                yield return new TestCaseData(new[] { "Sparrow", "Jack", "The" } as object)
                    .Returns(new[]
                        {
                            new Item<string>(0, ItemKind.First, "Sparrow"),
                            new Item<string>(1, ItemKind.Middle, "Jack"),
                            new Item<string>(2, ItemKind.Last, "The")
                        }
                    );

                yield return new TestCaseData(new[] { "Quick", "Brown", "Fox", "Jumps", "Over" } as object)
                    .Returns(new[]
                        {
                            new Item<string>(0, ItemKind.First, "Quick"),
                            new Item<string>(1, ItemKind.Middle, "Brown"),
                            new Item<string>(2, ItemKind.Middle, "Fox"),
                            new Item<string>(3, ItemKind.Middle, "Jumps"),
                            new Item<string>(4, ItemKind.Last, "Over")
                        }
                    );
            }
        }

        [Test]
        [TestCaseSource(nameof(KindCases))]
        public IEnumerable<(ItemKind Kind, int Value)> Enrich_With_Kind(int[] sequence) => sequence.WithItemKind();

        [Test]
        [TestCaseSource(nameof(IndexCases))]
        public IEnumerable<(int Index, string Item)> Enrich_With_Index(string[] sequence) => sequence.Indexed();

        [Test]
        [TestCaseSource(nameof(ItemCases))]
        public IEnumerable<Item<string>> Enrich_With_Item(string[] sequence) => sequence.AsItems();

        [Test]
        [TestCaseSource(nameof(KindCases))]
        public async Task<IEnumerable<(ItemKind Kind, int Value)>> Enrich_With_Kind_On_Async_Enumerable(int[] sequence)
        {
            return await sequence.ToAsyncEnumerable().WithItemKind().ToListAsync();
        }

        [Test]
        [TestCaseSource(nameof(IndexCases))]
        public async Task<IEnumerable<(int Index, string Item)>> Enrich_With_Index_On_Async_Enumerable(string[] sequence)
        {
            return await sequence.ToAsyncEnumerable().Indexed().ToListAsync();
        }

        [Test]
        [TestCaseSource(nameof(ItemCases))]
        public async Task<IEnumerable<Item<string>>> Enrich_With_Item_On_Async_Enumerable(string[] sequence)
        {
            return await sequence.ToAsyncEnumerable().AsItems().ToListAsync();
        }

        [Test]
        public void Support_Cancellation_For_With_Item_Kind()
        {
            using var source = new CancellationTokenSource();

            // ReSharper disable once MethodSupportsCancellation
            Assert.ThrowsAsync<OperationCanceledException>(
                async () =>
                {
                    await foreach (var _ in Yield().WithItemKind().WithCancellation(source.Token))
                    {
                        source.Cancel();
                    }
                }
            );
        }

        private static async IAsyncEnumerable<string> Yield([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return "Dog";
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            yield return "Cat";
            token.ThrowIfCancellationRequested();
            yield return "Sparrow";
        }
    }
}