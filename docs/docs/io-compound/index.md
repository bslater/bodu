---
title: Bodu.IO.Compound — Introduction
---

# Bodu.IO.Compound

**Bodu.IO.Compound** is a read-only reader for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office documents (`.xls`, `.doc`, `.ppt`, `.msg`) and other technologies. Part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic, it exposes the container's embedded storage hierarchy and the raw byte payload of each named stream, with no application-format knowledge of its own.

A compound file is effectively a small file system embedded in a single file. <xref:Bodu.IO.Compound.CompoundFile> is the managed counterpart of the COM `StgOpenStorage` entry point: navigation begins at the root storage and descends through nested storages to stream leaves.

![A compound file is a structured-storage envelope: a header, allocation tables, and a directory of sectors on the left, resolving via CompoundFile.Open into the logical RootStorage to CompoundStorage to CompoundStream hierarchy on the right.](../../images/diagrams/io-compound-structure.svg)

| Concept | Type | COM analogue | Role |
|---|---|---|---|
| **File** | <xref:Bodu.IO.Compound.CompoundFile> | `StgOpenStorage` | Opens the container and anchors the hierarchy at `RootStorage`. |
| **Storage** | <xref:Bodu.IO.Compound.CompoundStorage> | `IStorage` | A named container of child storages and streams. |
| **Stream** | <xref:Bodu.IO.Compound.CompoundStream> | `IStream` | A named, file-like leaf with an opaque byte payload; itself a read-only, seekable <xref:System.IO.Stream> cursor over those bytes. |

## Key concepts

| Concept | Plain-language meaning |
|---|---|
| **Compound file** | A single physical file beginning with the OLE2 signature `D0 CF 11 E0` that holds a header, allocation tables, a directory, and sectors. |
| **Storage / stream** | The directory tree: storages are folders, streams are files. Names are matched with ordinal (case-sensitive) equality, scoped to direct children. |
| **Sector chain** | A stream's bytes are stored as a linked chain of fixed-size sectors; the reader follows the chain to assemble or stream the payload. |
| **Buffered vs streaming** | The whole file is read into memory at open time by default, or sectors are read on demand from a seekable source for large files. |
| **Property set** | An OLE metadata stream (`\x05SummaryInformation`, `\x05DocumentSummaryInformation`) mapping integer property IDs to typed values. |

For the full glossary, see [Core concepts](concepts.md).

## Scope and limitations

- **Read-only.** Only read access (`FileMode.Open` with `FileAccess.Read`) is covered here; creation and mutation are out of scope for this introduction.
- **No format interpretation.** The reader surfaces named streams and their bytes; understanding a `Workbook` or `WordDocument` stream is the caller's job. The narrow BIFF8 `.xls` reader in [Bodu.Formats.Excel.Binary](../excel/index.md) is the worked example of a format reader layered on top.

## Worked example — open, navigate, read

A single flow traces the container end-to-end:

1. Probe the input cheaply with `CompoundFile.IsCompoundFile(stream)`.
2. Open the container: `using CompoundFile file = CompoundFile.Open(stream)`.
3. Walk the hierarchy from <xref:Bodu.IO.Compound.CompoundFile.RootStorage> with `EnumerateEntries` / `EnumerateStorages` / `EnumerateStreams`.
4. Resolve a named stream: `file.RootStorage.OpenStream("Workbook")` (or the non-throwing `TryOpenStream`).
5. Read the bytes: `ReadAllBytes()` for a small payload, or `Open()` for a seekable `CompoundStream` cursor.

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));

foreach (CompoundEntryInfo info in file.RootStorage.EnumerateEntries())
    Console.WriteLine($"{info.EntryType}: {info.Name} ({info.Length} bytes)");

CompoundStream workbook = file.RootStorage.OpenStream("Workbook");
ReadOnlyMemory<byte> bytes = workbook.ReadAllBytes();
```

## Common scenarios

| Scenario | Reach for |
|---|---|
| Test whether a file is a compound file | `CompoundFile.IsCompoundFile(stream)` |
| Open a small file fully in memory | `CompoundFile.Open(stream)` |
| Bound memory for a large file | `CompoundFile.Open(stream, buffered: false)` |
| List a storage's children | `EnumerateEntries` / `EnumerateStorages` / `EnumerateStreams` |
| Read a required stream's bytes | `OpenStream(name).ReadAllBytes()` |
| Read a stream incrementally | `OpenStream(name).Open()` → a `CompoundStream` cursor |
| Look up a stream that may be absent | `TryOpenStream(name, out entry)` |
| Read authored document metadata | `file.TryGetSummaryInformation(out summary)` |

## Headline types — <xref:Bodu.IO.Compound>

| Type | Purpose |
|---|---|
| <xref:Bodu.IO.Compound.CompoundFile> | Opens a CFB container and anchors the hierarchy; static `Open` / `IsCompoundFile` factories. |
| <xref:Bodu.IO.Compound.CompoundStorage> | A storage node — enumerates children and resolves child storages and streams by name. |
| <xref:Bodu.IO.Compound.CompoundStream> | A stream node and read-only, seekable `Stream` cursor in one — `ReadAllBytes` for the whole payload, `AsMemory` for a whole-payload view, `Stat` for metadata. |
| <xref:Bodu.IO.Compound.CompoundEntryInfo> | An immutable metadata snapshot — name, entry type, length, class id, timestamps. |
| <xref:Bodu.IO.Compound.CompoundFileFormatException>, <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> | Malformed-container and missing-entry errors. |

## Property sets — <xref:Bodu.IO.Compound.PropertySets>

| Type | Purpose |
|---|---|
| <xref:Bodu.IO.Compound.PropertySets.SummaryInformation> | Typed view over the `\x05SummaryInformation` stream — title, author, timestamps, counts. |
| <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> | Typed view over the `\x05DocumentSummaryInformation` stream. |
| <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> | The underlying code-paged, sectioned property map for non-standard properties. |

## Where to go next

- **[Core concepts](concepts.md)** — full vocabulary: container, storage, stream, sector chain, mini-stream, directory, property set.
- **[Getting started](getting-started.md)** — install + minimal samples for opening, navigating, and reading.
- **[Bodu.IO.Compound guides](../../guides/io-compound/index.md)** — reading files, buffered vs streaming access, and property sets.
- **API reference** — [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.IO.Compound.PropertySets](xref:Bodu.IO.Compound.PropertySets).
- **[Binary Formats & I/O topic overview](../topics/binary-formats.md)** — where the container reader sits beneath the format readers.
- **For the BIFF8 `.xls` reader built on this package**, see [Bodu.Formats.Excel.Binary](../excel/index.md).
