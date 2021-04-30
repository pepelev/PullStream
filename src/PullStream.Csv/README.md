# PullStream.Csv

__PullStream.Csv__ allows to create lazy `System.IO.Stream` that contains csv representation of sequence of elements. __PullStream.Csv__ uses __PullStream__, and therefore inherits all aspects of it.

## Installation

Install [NuGet package](https://www.nuget.org/packages/PullStream.Csv/) using Package Manager

```
Install-Package PullStream.Csv -Version 1.0.0
```

__PullStream.Csv__ uses the [CsvHelper package](https://www.nuget.org/packages/CsvHelper/) and supports a broad range of its versions. So, if it is desired to use a new version of CsvHelper package, direct reference to CsvHelper should be added.

```
Install-Package PullStream.Csv -Version 1.0.0
Install-Package CsvHelper -Version 27.0.2
```

## Usage

```csharp
using System.Collections.Generic;
using CsvHelper.Configuration;
using PullStream.Csv;

class Person
{
    public string Name { get; }
    public int Age { get; }
}

IEnumerable<Person> persons = ...;
var configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
var stream = CsvStream.Of(persons, configuration, LastNewLine.No).Build();
```

The resulting `stream` will contain header and a row for each person in `persons` but no trailing newline.
Encoding, newline characters, presence of header and other aspects of csv content are determined by `CsvConfiguration`. For example if header does not needed, one can use such a configuration

```csharp
var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = false
};
```

A way to create csv stream from `IAsyncEnumerable` is also presented and is similar.