using System;
using System.Collections.Generic;
using ClassLibrary1;
using NUnit.Framework;

namespace LazyStream.Test
{
    public sealed class SequenceExtensionShould
    {
        private static IEnumerable<TestCaseData> Cases
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

        [Test]
        [TestCaseSource(nameof(Cases))]
        public IEnumerable<(ItemKind Kind, int Value)> Test(int[] sequence) => sequence.WithItemKind();
    }
}