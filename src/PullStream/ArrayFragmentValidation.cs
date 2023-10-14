using System;

namespace PullStream;

internal readonly struct ArrayFragmentValidation
{
    private readonly (byte[] Value, string Name) array;
    private readonly (int Value, string Name) offset;
    private readonly (int Value, string Name) count;

    public ArrayFragmentValidation(
        (byte[] Value, string Name) array,
        (int Value, string Name) offset,
        (int Value, string Name) count)
    {
        this.array = array;
        this.offset = offset;
        this.count = count;
    }

    public void Run()
    {
        if (array.Value == null)
        {
            throw new ArgumentNullException(array.Name);
        }

        if (offset.Value < 0)
        {
            throw new ArgumentOutOfRangeException(offset.Name, offset.Value, "Must be non-negative");
        }

        if (offset.Value + count.Value > array.Value.Length)
        {
            throw new ArgumentOutOfRangeException(
                count.Name,
                count.Value,
                "offset + count must not exceed array length"
            );
        }
    }
}