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
byte[] bytes = workbook.ReadAllBytes();
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

`EnumerateEntries` yields a metadata snapshot for every direct child; `EnumerateStorages` and `EnumerateStreams` yield the navigable child storages and stream entries. All three enumerate in **directory (canonical) order** — the in-order traversal of each storage's red-black child tree — and are scoped to direct children only. Names carry their control prefixes verbatim, so a lookup of a summary-information stream must pass the literal `\x05`-prefixed name.

## Walk the whole tree recursively

```csharp
using Bodu.IO.Compound;

static void Walk(CompoundStorage storage, int depth = 0)
{
    string indent = new(' ', depth * 2);
    foreach (CompoundEntryInfo info in storage.EnumerateStreams())
        Console.WriteLine($"{indent}{info.Name} ({info.Length} bytes)");

    foreach (CompoundStorage child in storage.EnumerateStorages())
    {
        Console.WriteLine($"{indent}[{child.Name}]");
        Walk(child, depth + 1);
    }
}

using CompoundFile file = CompoundFile.OpenRead("message.msg");
Walk(file.RootStorage);
```

`EnumerateStorages` returns navigable <xref:Bodu.IO.Compound.CompoundStorage> objects, so descending into a `.msg` attachment or a Word formatting storage is just a recursive call — there is no path syntax, you walk one storage at a time.

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

`OpenStream(name)` returns a read-only, seekable `CompoundStream` you can hand to any `Stream` consumer. Under a streaming file it reads sectors on demand; under a buffered file it works over the in-memory payload. (On a writable file, the `OpenStream(name, FileMode, FileAccess)` overload and `CreateStream` return a read-write cursor instead — see the [authoring guide](../../guides/io-compound/authoring-compound-files.md).)

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

## Tune the read strategy and validation level

The parameterless overloads buffer the whole file and validate at <xref:Bodu.IO.Compound.CompoundValidationLevel.Compatible>. Pass a <xref:Bodu.IO.Compound.CompoundFileOptions> to choose another <xref:Bodu.IO.Compound.CompoundReadStrategy> or validation level:

```csharp
using Bodu.IO.Compound;

var options = new CompoundFileOptions
{
    ReadStrategy = CompoundReadStrategy.Auto,   // buffer small files, stream large ones
    MaxBufferedBytes = 16L * 1024 * 1024,       // the buffer/stream threshold for Auto
    ValidationLevel = CompoundValidationLevel.Strict,
};

using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"), options);
```

`Strict` rejects individually malformed directory entries and unsorted siblings that `Compatible` tolerates; <xref:Bodu.IO.Compound.CompoundValidationLevel.Minimal> recovers from structural corruption instead of throwing. See [Core concepts](concepts.md#validation-level) for the full matrix.

## Handle a malformed file

```csharp
using Bodu.IO.Compound;

try
{
    using CompoundFile file = CompoundFile.OpenRead(path);
    // ...
}
catch (CompoundFileFormatException ex)
{
    // ex.Category is a stable CompoundFileError — InvalidSignature, TruncatedFile,
    // FatCycle, DirectoryCycle, StreamChainTooShort, and so on.
    Console.WriteLine($"Not a usable compound file: {ex.Category}");
}
```

A modern `.xlsx` / `.docx` (a ZIP archive, not OLE2) fails the signature check and surfaces `CompoundFileError.InvalidSignature`. Probe first with `CompoundFile.IsCompoundFile` when a non-compound input is an expected outcome rather than an error.

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary behind these samples: header, FAT/mini-FAT, the red-black directory, validation levels, and the error categories.
- **[Introduction](index.md)** — headline types and common scenarios.
- **[Bodu.IO.Compound guides](../../guides/io-compound/index.md)** — reading files, buffered vs streaming access, and property sets.
- **API reference** — [Bodu.IO.Compound](xref:Bodu.IO.Compound).
