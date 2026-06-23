---
title: Reading compound files
---

# Reading compound files

<xref:Bodu.IO.Compound.CompoundFile> opens an OLE2 / Compound File Binary container and exposes its storage hierarchy and stream payloads. This guide covers the end-to-end recipe: probe the input, open the file, walk the directory, and read a named stream's bytes.

The mental model is a file system in a single file — storages are directories, streams are files. Navigation starts at `RootStorage` and is scoped to each storage's direct children, compared with ordinal (case-sensitive) names.

## Pattern 1 — open a file and read a known stream

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));

CompoundStream workbook = file.RootStorage.OpenStream("Workbook");
ReadOnlyMemory<byte> bytes = workbook.ReadAllBytes();
```

`Open` reads the stream from its current position to the end. The returned `CompoundFile` is <xref:System.IDisposable> — the `using` declaration disposes it, which also closes the source stream unless `leaveOpen: true` was passed. `OpenStream` resolves a direct child of `RootStorage` by name and throws <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> when no such stream exists.

## Pattern 2 — probe before opening

```csharp
using Bodu.IO.Compound;

using FileStream source = File.OpenRead(path);

if (!CompoundFile.IsCompoundFile(source))
{
    log.Warn("Not a compound file");
    return;
}

using CompoundFile file = CompoundFile.Open(source, leaveOpen: true);
```

`CompoundFile.IsCompoundFile` inspects only the eight-byte OLE2 signature (`D0 CF 11 E0 A1 B1 1A E1`) and restores the stream position before returning, so it is cheap to call ahead of a full open. There is also a `ReadOnlySpan<byte>` overload for bytes you already hold.

## Pattern 3 — walk the hierarchy

```csharp
using Bodu.IO.Compound;

static void Print(CompoundStorage storage, int depth = 0)
{
    foreach (CompoundEntryInfo info in storage.EnumerateEntries())
        Console.WriteLine($"{new string(' ', depth * 2)}{info.EntryType}: {info.Name} ({info.Length} bytes)");

    foreach (CompoundStorage child in storage.EnumerateStorages())
    {
        Console.WriteLine($"{new string(' ', depth * 2)}[{child.Name}]");
        Print(child, depth + 1);
    }
}

using CompoundFile file = CompoundFile.Open(File.OpenRead("message.msg"));
Print(file.RootStorage);
```

Each <xref:Bodu.IO.Compound.CompoundStorage> exposes three enumerators over its direct children, all in directory order: `EnumerateEntries` yields a <xref:Bodu.IO.Compound.CompoundEntryInfo> metadata snapshot for every child (storage *and* stream), while `EnumerateStorages` and `EnumerateStreams` yield the navigable child storages and stream entries respectively. `CompoundEntryInfo` carries the `Name`, `EntryType`, `Length`, `ClassId`, and the creation / modification timestamps.

## Pattern 4 — non-throwing lookup

```csharp
using Bodu.IO.Compound;

if (file.RootStorage.TryOpenStorage("ObjectPool", out CompoundStorage? pool) &&
    pool.TryOpenStream("Contents", out CompoundStream? contents))
{
    ReadOnlyMemory<byte> data = contents.ReadAllBytes();
    Process(data);
}
```

Prefer the `TryOpenStorage` / `TryOpenStream` pair when a missing entry is a normal outcome — they return `false` instead of raising `CompoundStreamNotFoundException`. The throwing `OpenStorage` / `OpenStream` forms are better when the entry is required and its absence is a programming or data error; the exception's `StreamName` property names the entry that was not found.

## Reading the bytes

The <xref:Bodu.IO.Compound.CompoundStream> returned by `OpenStream` gives you two ways to read its payload:

| Member | Returns | Use when |
|---|---|---|
| `ReadAllBytes` | `ReadOnlyMemory<byte>` | The payload is small and consumed in one pass. |
| the stream itself | a seekable <xref:System.IO.Stream> | The payload is large or read incrementally — `CompoundStream` is a seekable `Stream` cursor you can hand to `BinaryReader`, `StreamReader`, or `CopyTo`. |

```csharp
using CompoundStream stream = file.RootStorage.OpenStream("Workbook");
using var reader = new BinaryReader(stream);

ushort recordType = reader.ReadUInt16();
ushort recordSize = reader.ReadUInt16();
```

See [Buffered vs streaming access](streaming-and-buffering.md) for how the cursor behaves under buffered and streaming files, and how to bound memory for large payloads.

## Error handling

| Exception | Cause |
|---|---|
| <xref:System.ArgumentNullException> | The stream passed to `Open` is `null`. |
| <xref:System.ArgumentException> | `buffered: false` was requested over a non-seekable stream, or `IsCompoundFile` was given a non-seekable stream. |
| <xref:System.NotSupportedException> | An unsupported `FileMode` / `FileAccess` combination was requested. |
| <xref:Bodu.IO.Compound.CompoundFileFormatException> | The content is not a well-formed compound file, or a stream's sector chain is malformed. |
| <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> | `OpenStream` / `OpenStorage` named an entry that does not exist. |

## Where to go next

- [Buffered vs streaming access](streaming-and-buffering.md) — the `buffered` flag and the `CompoundStream` cursor in depth.
- [Reading property sets](property-sets.md) — pull authored metadata from the summary-information streams.
- [Bodu.IO.Compound API reference](xref:Bodu.IO.Compound).
