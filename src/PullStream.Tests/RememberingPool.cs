using System;
using System.Buffers;
using System.Collections.Immutable;
using Comparation;

namespace PullStream.Tests
{
    public sealed class RememberingPool : ArrayPool<byte>
    {
        private readonly ArrayPool<byte> source;

        private ImmutableHashSet<byte[]> rentArrays =
            ImmutableHashSet<byte[]>.Empty.WithComparer(
                Equality.ByReference<byte[]>()
            );

        public RememberingPool(ArrayPool<byte> source)
        {
            this.source = source;
        }

        public override byte[] Rent(int minimumLength)
        {
            var array = source.Rent(minimumLength);
            if (rentArrays.Contains(array))
            {
                throw new Exception("Same array returned");
            }

            rentArrays = rentArrays.Add(array);
            Operations++;
            return array;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (!rentArrays.Contains(array))
            {
                throw new Exception("Return array that not rent");
            }

            var newArray = rentArrays.Remove(array);
            source.Return(array, clearArray);
            rentArrays = newArray;
            Operations++;
        }

        public bool HasCurrentRentArrays => !rentArrays.IsEmpty;
        public int Operations { get; private set; }
    }
}