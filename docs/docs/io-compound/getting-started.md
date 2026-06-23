---
title: Bodu.IO.Compound — Getting started
---

# Bodu.IO.Compound — Getting started

Unfamiliar with terms like *compound file*, *storage*, *stream*, *sector chain*, or *property set*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.IO.Compound
```

Targets `net8.0`. Depends only on `Bodu.Core` for shared throw-helpers; no other NuGet references.

## Open a file and read a stream

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));

CompoundStream workbook = file.RootStorage.OpenStream("Workbook");
ReadOnlyMemory<byte> bytes = workbook.ReadAllBytes();
```

`CompoundFile.Open` reads the source from its current position to the end. The returned instance is <xref:System.IDisposable> — the `using` declaration disposes it and closes the source unless `leaveOpen: true` was passed.

## Probe before opening

```csharp
using Bodu.IO.Compound;

using FileStream source = File.OpenRead(path);

if (CompoundFile.IsCompoundFile(source))
{
    using CompoundFile file = CompoundFile.Open(source, leaveOpen: true);
    // ...
}
```

`IsCompoundFile` checks only the eight-byte OLE2 signature and restores the stream position, so it is cheap to call ahead of a full open.

## Walk the hierarchy

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open(File.OpenRead("message.msg"));

foreach (CompoundEntryInfo info in file.RootStorage.EnumerateEntries())
    Console.WriteLine($"{info.EntryType}: {info.Name} ({info.Length} bytes)");

foreach (CompoundStorage child in file.RootStorage.EnumerateStorages())
    Console.WriteLine($"[{child.Name}]");
```

`EnumerateEntries` yields a metadata snapshot for every direct child; `EnumerateStorages` and `EnumerateStreams` yield the navigable child storages and stream entries.

## Read a large stream incrementally

```csharp
using Bodu.IO.Compound;

// Open the file in streaming mode so large payloads are read on demand.
using FileStream source = File.OpenRead("large.msg");
using CompoundFile file = CompoundFile.Open(source, buffered: false);

using CompoundStream stream = file.RootStorage.OpenStream("__substg1.0_1000001F");
using var reader = new StreamReader(stream, Encoding.Unicode);

string text = reader.ReadToEnd();
```

`OpenStream` returns a read-only, seekable `CompoundStream` you can hand to any `Stream` consumer. Under a streaming file it reads sectors on demand; under a buffered file it works over the in-memory payload.

## Read document metadata

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

using CompoundFile file = CompoundFile.Open(File.OpenRead("report.doc"));

if (file.TryGetSummaryInformation(out SummaryInformation? summary))
{
    Console.WriteLine(summary.Title);
    Console.WriteLine(summary.Author);
    Console.WriteLine(summary.LastSaveTime);
}
```

`TryGetSummaryInformation` returns `false` when the file has no summary-information stream, and every property is nullable, so it is safe to call on any file.

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary behind these samples.
- **[Introduction](index.md)** — headline types and common scenarios.
- **[Bodu.IO.Compound guides](../../guides/io-compound/index.md)** — reading files, buffered vs streaming access, and property sets.
- **API reference** — [Bodu.IO.Compound](xref:Bodu.IO.Compound).
