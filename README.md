# PullStream

__PullStream__ PullStream allows to create lazy `System.IO.Stream` based on `IEnumerable<T>`.

![PullStream](logo.svg)

## Installation

Install [NuGet package](https://www.nuget.org/packages/PullStream/) using Package Manager

```
Install-Package PullStream -Version 1.1.0
```

## Why do i need a lazy Stream?

Lazy stream is useful when we need a read-oriented Stream and don't want to keep all of the stream's content in memory. For example, this is useful when we need to return a large HTTP response or even an endless one.

## Usage

Let's say we have a sequence of strings and want a stream to contain these strings, with each string being placed on a separate line.

[Go to code](src/PullStream.Tests/Examples.cs#L10)
```csharp
using System;
using System.Collections.Generic;
using System.Text;

IEnumerable<string> strings = ...;
System.IO.Stream stream = PullStream.SequenceStream.FromStrings(
    strings,
    Encoding.UTF8,
    Environment.NewLine
);
```

How we have a stream that will lazily enumerate `strings` and return them as utf8 bytes while we read it.

### Csv

For working with csv see [PullStream.Csv](src/PullStream.Csv)

### Builder

Consider a more flexible way to create streams.

Now we have a sequence of byte arrays - chunks, and we want to put them together in single stream.

[Go to code](src/PullStream.Tests/Examples.cs#L13)
```csharp
using PullStream;

IEnumerable<byte[]> chunks = ...;
var chunksStream = SequenceStream.UsingStream() // We will write chunks in a stream
    .On(chunks)
    .Writing(
        (Stream stream, byte[] chunk) =>
        {   // Put each chunk in a stream
            stream.Write(chunk, 0, chunk.Length); 
        }
    );
```

### Context

Sometimes it's useful to write items into a wrapper instead of plain stream.

[Go to code](src/PullStream.Tests/Examples.cs#L20)
```csharp
public class Person
{
    public string Name { get; }
    public int Age { get; }
}

var personsStream = SequenceStream.Using(
        // We will use BinaryWriter to put
        // each person in a stream.
        // BinaryWriter is our context now.
        (Stream stream) => new BinaryWriter(stream, Encoding.UTF8)
    )
    .On(persons)
    .Writing(
        (BinaryWriter binaryWriter, Person person) =>
        {
            // Writing is made to the context
            binaryWriter.Write(person.Name);
            binaryWriter.Write(person.Age);
        }
    );
```

Context ([BinaryWriter](https://docs.microsoft.com/dotnet/api/system.io.binarywriter) in the example above) is created once for the entire stream life cycle.

## Sequence item meta information

Some scenarios require knowledge about item location in the sequence. For example, in csv format, the header may be placed before the first row to describe the columns. Or we may use such knowledge so that we don't add extra line after the last item.

We can use extension method for `IEnumerable<T>` or `IAsyncEnumerable<T>` to get such information.

[Go to code](src/PullStream.Tests/Examples.cs#L33)
```csharp
IEnumerable<string> names = ...;
IEnumerable<Item<string>> enrichedNames = names.AsItems();

// Item<T> supports deconstruction
foreach ((int index, ItemKind kind, string name) in enrichedNames)
{
    if (kind.IsFirst())
    {
        WriteHeader();
    }
    Write($"{index}: {name}");
    if (!kind.IsLast())
    {
        WriteLine();
    }
}
```

[ItemKind](src/PullStream/ItemKind.cs#L6) may be one the following:

- First - This item is first and there are more items
- Middle - This item is neither the first nor the last
- Last - This item is last and there are previous items
- Sigle - This item is the only item in sequence, there are no others

There are [methods](src/PullStream/ItemKindExtensions.cs#L3) to simplify work with `ItemKind`. For example, [IsFirst() method](src/PullStream/ItemKindExtensions.cs#L8) matches `Single` and `First` values.

Alternative way to get such information is to call [AsItems() method](src/PullStream/SequenceStream.Builder.cs#L208) when stream is constructed.

[Go to code](src/PullStream.Tests/Examples.cs#L51)
```csharp
IEnumerable<string> names = ...;

var namesStream = SequenceStream.Using(
        stream => new StreamWriter(stream, Encoding.UTF8)
    )
    .On(names)
    .AsItems()
    .Writing(
        (writer, item) =>
        {
            if (item.Kind.IsFirst())
            {
                writer.WriteLine("Names");
            }

            writer.Write($"{item.Index}: {item.Value}");
            if (!item.Kind.IsLast())
            {
                writer.WriteLine();
            }
        }
    );
```

## AsyncEnumerable

All features available for both `IEnumerable<T>` and `IAsyncEnumerable<T>`.

Resulting stream may be consumed synchronously (using `int Stream.Read(byte[] buffer, int offset, int count)` or `int Stream.Read(Span<byte>)`) or asynchronously (using `Task<int> Stream.ReadAsync(byte[] buffer, int offset, int count)` or `ValueTask<int> Stream.ReadAsync(Memory<T>, CancellationToken)`). But it's recommended to consume stream created with `IAsyncEnumerable<T>` asynchronously, because otherwise async-to-sync conversion (`ReadAsync(...).Result`) will occur.

## Recycling and guidance

When stream is no longer needed, the user must dispose it. Either through `using` statement or calling `Stream.Dispose()` directly. Stream may be disposed at any stage of it's life cycle. The user must not use disposed stream. Stream contains

- `IEnumerator<T>` or `IAsyncEnumerator<T>`,
- context with automatic in case when context implements `IDisposable` interface or user defined cleanup,
- pooled array

which require disposal.

Stream instances are not thread safe. I.e. the user must call method and properties only is serial manner.

It's unsafe to work with stream that thrown exception on read attempt. The stream provides shallow consistency i.e. stream as such can't go to inconsistent state by any sequence of method calls, even if exception thrown. But `IEnumerator<T>` or `IAsyncEnumerator<T>` may be not such consistent. And writing an item to the context may be not atomic or not idempotent. Using stream after exception may lead to missing items, duplicate items, partially written items, etc. So if stream throws an exception dispose and discard it.

## Internals

When stream content is read stream enumerates next element of the sequence and write it to internal buffer. If internal buffer is able to fulfill read request that data is returned and buffer is cut on the size of returned data. Otherwise next element of the sequence is requested and written to internal buffer. Thus, the stream doesn't keep entire content in memory, once the data is read it's dropped.

Such approach can cause big latency if the context uses caching and sequence elements come slowly. To prevent this the user may set caching buffer size to lower values or `Flush()` the context on every item writing. Stream expects the context to flush all buffered data during its disposal.