---
title: Bodu.IO.Compound — Introduction
---

# Bodu.IO.Compound

**Bodu.IO.Compound** is a reader and writer for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office documents (`.xls`, `.doc`, `.ppt`, `.msg`) and other technologies. Part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic, it opens existing containers — exposing the embedded storage hierarchy and the raw byte payload of each named stream — and authors new ones through `CompoundFile.Create` and the `Bodu.IO.Compound.Builders` API, with no application-format knowledge of its own.

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
| **Storage / stream** | The directory tree: storages are folders, streams are files. Names are matched **case-insensitively** by the compound-file relationship, scoped to a storage's direct children. |
| **Sector chain** | A stream's bytes are stored as a linked chain of fixed-size sectors indexed by the FAT (or, for streams under the mini-stream cutoff, the mini-FAT); the reader follows the chain to assemble or stream the payload. |
| **Buffered vs streaming** | The whole file is read into memory at open time by default, or sectors are read on demand from a seekable source for large files; a third `Auto` strategy picks per source size. |
| **Property set** | An OLE metadata stream (`\x05SummaryInformation`, `\x05DocumentSummaryInformation`) mapping integer property IDs to typed values. |

For the full glossary, see [Core concepts](concepts.md).

## Scope and limitations

- **Reading and writing.** This introduction focuses on the read path; the library also **authors** CFB containers — `CompoundFile.Create` plus `Commit`, the detached `CompoundStorageBuilder`, and the property-set builders. See the [Authoring compound files](../../guides/io-compound/authoring-compound-files.md) guide.
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
byte[] bytes = workbook.ReadAllBytes();
```

## Common scenarios

| Scenario | Reach for |
|---|---|
| Test whether a file is a compound file | `CompoundFile.IsCompoundFile(stream)` |
| Open a small file fully in memory | `CompoundFile.Open(stream)` |
| Bound memory for a large file | `CompoundFile.Open(stream, buffered: false)` |
| Tune the read strategy or validation level | `CompoundFile.Open(stream, options)` with a `CompoundFileOptions` |
| Classify why a file was rejected | `catch (CompoundFileFormatException ex)` → `ex.Category` |
| List a storage's children | `EnumerateEntries` / `EnumerateStorages` / `EnumerateStreams` |
| Read a required stream's bytes | `OpenStream(name).ReadAllBytes()` |
| Read a stream incrementally | `OpenStream(name).Open()` → a `CompoundStream` cursor |
| Look up a stream that may be absent | `TryOpenStream(name, out entry)` |
| Read authored document metadata | `file.TryGetSummaryInformation(out summary)` |
| Author a container | `CompoundStorageBuilder.CreateRoot()` → `AddStream` → `Save` (or `CompoundFile.Create` + `Commit`) |

## Headline types — <xref:Bodu.IO.Compound>

| Type | Purpose |
|---|---|
| <xref:Bodu.IO.Compound.CompoundFile> | Opens or creates a CFB container and anchors the hierarchy; static `Open` / `OpenRead` / `IsCompoundFile` readers and the `Create` writer (finalized by `Commit`). |
| <xref:Bodu.IO.Compound.CompoundStorage> | A storage node — enumerates children and resolves child storages and streams by name. |
| <xref:Bodu.IO.Compound.CompoundStream> | A stream node and read-only, seekable `Stream` cursor in one — `ReadAllBytes` for the whole payload, `AsMemory` for a whole-payload view, `Stat` for metadata. |
| <xref:Bodu.IO.Compound.CompoundEntryInfo> | An immutable metadata snapshot — name, <xref:Bodu.IO.Compound.CompoundEntryType>, length, class id, timestamps, and red-black <xref:Bodu.IO.Compound.CompoundEntryColor>. |
| <xref:Bodu.IO.Compound.CompoundFileOptions> | Read options — <xref:Bodu.IO.Compound.CompoundReadStrategy> (buffered / streaming / auto) and <xref:Bodu.IO.Compound.CompoundValidationLevel> (strict / compatible / minimal). |
| <xref:Bodu.IO.Compound.CompoundFileException> | The common base for every compound-file failure; `CompoundFileFormatException` (with a <xref:Bodu.IO.Compound.CompoundFileError> `Category`) and `CompoundStreamNotFoundException` derive from it. |

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
