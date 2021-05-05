# PullStream.Json

__PullStream.Json__ allows to create lazy `System.IO.Stream` that contains json array of sequence of elements. __PullStream.Json__ uses __PullStream__, and therefore inherits all aspects of it.

## Installation

Install [NuGet package](https://www.nuget.org/packages/PullStream.Csv/) using Package Manager

```
Install-Package PullStream.Json
```

__PullStream.Json__ uses the [Json.Net package](https://www.nuget.org/packages/Newtonsoft.Json/) and supports a broad range of its versions. So, if it is desired to use a new version of Json.Net package, direct reference to Json.Net should be added.

```
Install-Package PullStream.Json
Install-Package Json.Net -Version 13.0.1
```

## Usage

```csharp
using System.Collections.Generic;
using PullStream.Json;

class Person
{
    public string Name { get; }
    public int Age { get; }
}

IEnumerable<Person> persons = ...;
var stream = JsonStream.ArrayOf(persons).Build();
```

The resulting `stream` will contain json array of persons with no indentation.
If you need an indented output, use factory and configuration:

```csharp
var stream = JsonStream
    .Using(
        new JsonTextWriterFactory(
            configuration: new Indented(),
            bufferSize: 1024
        )
    )
    .ArrayOf(sequence)
    .Build();
```

There are different configurations and you can combine them with `Configuration.Composite` class like this

```csharp
using System.Globalization;

new JsonTextWriterFactory(
    configuration: new Configuration.Composite(
        new Culture(CultureInfo.GetCultureInfo("en-us")),
        new Indented()
    ),
    bufferSize: 1024
)
```

A way to create csv stream from `IAsyncEnumerable` is also presented and is similar.

## Caveats

__PullStream.Json__ allows you to control `Newtonsoft.Json.JsonWriter` creation as well as pass
custom `Newtonsoft.Json.JsonSerializer` instance during stream configuration. This can cause
wrong or unexpected output.

Let's see some examples:

```csharp
var numbers = new[] {1, 2, 3};
var rightStream = JsonStream
    .Using(
        new JsonTextWriterFactory(
            configuration: new Indented(),
            bufferSize: 1024
        )
    )
    .ArrayOf(numbers)
    .Build();
/*
 * Produces nice indented output
 * [
 *   1,
 *   2,
 *   3
 * ]
 */
```

on the other hand

```csharp
using Newtonsoft.Json;

var numbers = new[] {1, 2, 3};
var wrongStream = JsonStream
    .Using(
        new JsonTextWriterFactory()
    )
    .ArrayOf(
        numbers,
        JsonSerializer.Create(
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            }
        )
    )
    .Build();
/*
 * Produces partially indented result
 * [
 *   1,
 *   2,
 *   3]
 */
```

This inconsistency exists because open and close brackets of main array are written directly on `JsonWriter`, but items of main array are written using `JsonSerializer`.

The general rule here is to configure `JsonSerializer` only if particular aspect could not be
configured on `JsonWriter`.