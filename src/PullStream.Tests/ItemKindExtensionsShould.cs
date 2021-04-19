using NUnit.Framework;

namespace PullStream.Tests
{
    public sealed class ItemKindExtensionsShould
    {
        [Test]
        [TestCase(ItemKind.First, ExpectedResult = true)]
        [TestCase(ItemKind.Single, ExpectedResult = true)]
        [TestCase(ItemKind.Middle, ExpectedResult = false)]
        [TestCase(ItemKind.Last, ExpectedResult = false)]
        public bool IsFirst(ItemKind kind) => kind.IsFirst();

        [Test]
        [TestCase(ItemKind.First, ExpectedResult = false)]
        [TestCase(ItemKind.Single, ExpectedResult = false)]
        [TestCase(ItemKind.Middle, ExpectedResult = true)]
        [TestCase(ItemKind.Last, ExpectedResult = false)]
        public bool IsMiddle(ItemKind kind) => kind.IsMiddle();

        [Test]
        [TestCase(ItemKind.First, ExpectedResult = false)]
        [TestCase(ItemKind.Single, ExpectedResult = true)]
        [TestCase(ItemKind.Middle, ExpectedResult = false)]
        [TestCase(ItemKind.Last, ExpectedResult = true)]
        public bool IsLast(ItemKind kind) => kind.IsLast();
    }
}