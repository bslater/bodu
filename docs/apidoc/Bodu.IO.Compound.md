---
uid: Bodu.IO.Compound
---

![Bodu.IO.Compound](~/images/hero-io-compound.svg)

## Purpose

**Bodu.IO.Compound** is a reader and writer for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office documents (`.xls`, `.doc`, `.ppt`, `.msg`) and other technologies. It opens existing containers, exposing the embedded storage hierarchy and the raw byte payload of each named stream, and it authors new ones. It carries no application-format knowledge of its own: interpreting a `Workbook` or `WordDocument` stream is the caller's job.

A compound file is effectively a small file system embedded in a single file. <xref:Bodu.IO.Compound.CompoundFile> is the managed counterpart of the COM `StgOpenStorage` / `StgCreateStorageEx` entry points: navigation begins at the root storage and descends through nested storages (`IStorage`) to stream leaves (`IStream`). The narrow BIFF8 `.xls` reader in <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> is the worked example of a format reader layered on top.

The public surface spans three namespaces: <xref:Bodu.IO.Compound> (the reader, the in-place writer, and the value model), <xref:Bodu.IO.Compound.Builders> (the detached authoring object model), and <xref:Bodu.IO.Compound.PropertySets> (the OLE property-set readers and writers).

## Static documentation

- **[Introduction](~/docs/io-compound/index.md)** — the headline types, the COM analogues, and the scenarios the library covers.
- **[Core concepts](~/docs/io-compound/concepts.md)** — container, storage, stream, sector chain, mini-stream, directory, and property set.
- **[Getting started](~/docs/io-compound/getting-started.md)** — install and minimal samples for opening, navigating, and reading.
- **[Reading compound files](~/guides/io-compound/reading-compound-files.md)** — the open → navigate → read recipe.
- **[Buffered vs streaming access](~/guides/io-compound/streaming-and-buffering.md)** — the read strategy, the `CompoundStream` cursor, and bounding memory.
- **[Authoring compound files](~/guides/io-compound/authoring-compound-files.md)** — the two write paths, nested storages, options, and writing property sets.
- **[Reading property sets](~/guides/io-compound/property-sets.md)** — the summary-information metadata streams and the raw `OlePropertySet`.
- **[Office format nuances](~/guides/io-compound/office-format-nuances.md)** — how legacy Office documents lay out their named streams.

## Key types

**Container — `Bodu.IO.Compound`**

- <xref:Bodu.IO.Compound.CompoundFile> — the container session. Read factories `Open` / `OpenRead` (path / `Stream`, with a buffered-vs-streaming choice) and `IsCompoundFile`; write factory `Create`; instance `RootStorage`, `Access` / `CanRead` / `CanWrite` / `IsDirty`, `Commit` / `Revert` and the asynchronous `CommitAsync` / `FlushAsync`, the `TryGetSummaryInformation` / `TryGetDocumentSummaryInformation` property-set readers, and their `SetSummaryInformation` / `SetDocumentSummaryInformation` write counterparts.
- <xref:Bodu.IO.Compound.CompoundStorage> — a storage node. Read: `EnumerateEntries` / `EnumerateStorages` / `EnumerateStreams`, `OpenStorage` / `OpenStream` and their `TryOpen…` forms, `TryOpenPropertySet`. Write (on a writable file): `CreateStorage`, `CreateStream`, `Delete`, `Rename`, `WritePropertySet`, and the settable entry metadata `ClassId` / `CreationTime` / `ModifiedTime` / `StateBits`.
- <xref:Bodu.IO.Compound.CompoundStream> — a stream node and seekable <xref:System.IO.Stream> in one. `ReadAllBytes` / `AsMemory`, the standard `Read` / `Seek`, and a `ReadAsync` that is truly asynchronous over a streaming-mode cursor; `Write` / `SetLength` / `Flush` on a writable cursor (payloads capped at `int.MaxValue`).
- <xref:Bodu.IO.Compound.CompoundEntryInfo> — an immutable directory-entry snapshot (`Name`, `EntryType`, `Length`, `ClassId`, timestamps). <xref:Bodu.IO.Compound.CompoundEntryType>, <xref:Bodu.IO.Compound.CompoundEntryColor> — the entry kind and red-black node color.

**Read options**

- <xref:Bodu.IO.Compound.CompoundFileOptions> — `ReadStrategy` (<xref:Bodu.IO.Compound.CompoundReadStrategy>: `Buffered` / `Streaming` / `Auto`), `MaxBufferedBytes`, and `ValidationLevel` (<xref:Bodu.IO.Compound.CompoundValidationLevel>: `Strict` / `Compatible` / `Minimal`).

**Authoring — `Bodu.IO.Compound.Builders`**

- <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> — a detached, mutable authoring tree. `CreateRoot` / `Load` / `FromFile` factories; `AddStorage`, `AddStream` (in-memory, deferred, or `AddStreamFromFile`), `Remove`, `Rename`; and the serializers `Save(path)` / `WriteTo(Stream)` / `WriteTo(IBufferWriter<byte>)` / `ToArray()`.
- <xref:Bodu.IO.Compound.Builders.CompoundStreamBuilder> / <xref:Bodu.IO.Compound.Builders.CompoundEntryBuilder> — the stream node (with `Content`) and its abstract base.
- <xref:Bodu.IO.Compound.Builders.CompoundBuildOptions> — `Version` (<xref:Bodu.IO.Compound.CompoundFileVersion>: `V3` 512-byte / `V4` 4096-byte sectors) and `MaxDepth`. <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilderOptions> — `NameComparisonCaseSensitive`.

**Property sets — `Bodu.IO.Compound.PropertySets`**

- <xref:Bodu.IO.Compound.PropertySets.SummaryInformation> / <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> — typed views over the `\x05SummaryInformation` / `\x05DocumentSummaryInformation` streams, with `Read(Stream)` and a `StreamName` constant.
- <xref:Bodu.IO.Compound.PropertySets.SummaryInformationBuilder> / <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformationBuilder> — author a property set from typed fields; `ToPropertySet()` / `ToArray()` / `WriteTo(Stream)`.
- <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> / <xref:Bodu.IO.Compound.PropertySets.OlePropertySection> / <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> / <xref:Bodu.IO.Compound.PropertySets.OlePropertyType> — the underlying code-paged, sectioned property map for non-standard properties, readable and writable.

**Errors**

- <xref:Bodu.IO.Compound.CompoundFileException> (base), <xref:Bodu.IO.Compound.CompoundFileFormatException> (malformed container, with a <xref:Bodu.IO.Compound.CompoundFileError> category), <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> (missing entry), <xref:Bodu.IO.Compound.CompoundFileSerializationException> (authoring failure).

## Example

```csharp
using Bodu.IO.Compound;

// Read: open, navigate, and read a named stream.
using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));
CompoundStream workbook = file.RootStorage.OpenStream("Workbook");
byte[] bytes = workbook.ReadAllBytes();
```

```csharp
using Bodu.IO.Compound.Builders;

// Write: assemble a container from scratch and serialize it.
var root = CompoundStorageBuilder.CreateRoot();
root.AddStream("Workbook", workbookBytes);
CompoundStorageBuilder storage = root.AddStorage("Storage 1");
storage.AddStream("Nested", new byte[] { 1, 2, 3 });

root.Save("out.xls");   // or ToArray() / WriteTo(stream)
```

## Notes

- **Two ways to read.** <xref:Bodu.IO.Compound.CompoundFileOptions> selects between fully **buffered** access (the whole source is read into memory at open time — the default, and safe to share across threads) and **streaming** access (sectors are read on demand from a seekable source, bounding memory for large files). The `Auto` strategy switches on `MaxBufferedBytes`.
- **Two ways to write.** The detached <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> assembles a tree in memory and serializes it once (`Save` / `WriteTo` / `ToArray`) — ideal for authoring a container from scratch. The in-place <xref:Bodu.IO.Compound.CompoundFile.Create*> path returns a writable file whose `RootStorage.CreateStream` / `CreateStorage` mutate a staging tree that `Commit` (or the asynchronous `CommitAsync` / `FlushAsync`) persists to the destination. A writable <xref:Bodu.IO.Compound.CompoundStream> is a true read-write `Stream` (`CanWrite` is `true`).
- **Names.** Entry names are at most 31 UTF-16 code units, non-empty, and free of `/` and null characters; comparison is case-insensitive by default (per the CFB format). Duplicate names and invalid names throw <xref:Bodu.IO.Compound.CompoundFileSerializationException> at author time.
- **Versions.** `V3` (512-byte sectors) is the most compatible default; `V4` (4096-byte sectors) suits larger containers. Choose via <xref:Bodu.IO.Compound.Builders.CompoundBuildOptions>.
- **Property sets round-trip.** On a writable file, `CompoundFile.SetSummaryInformation` embeds a summary set and `CompoundFile.TryGetSummaryInformation` reads it back; `CompoundStorage.WritePropertySet` does the same for any named set. The writer emits every value shape the reader parses, including vector properties (variant vectors round-trip by value, not byte, identity).
- **See also:** the [introduction](~/docs/io-compound/index.md), [core concepts](~/docs/io-compound/concepts.md), and [getting-started](~/docs/io-compound/getting-started.md); the [reading](~/guides/io-compound/reading-compound-files.md), [authoring](~/guides/io-compound/authoring-compound-files.md), and [property-set](~/guides/io-compound/property-sets.md) guides; and the [Bodu.Formats.Excel.Binary](xref:Bodu.Formats.Excel) reader built on this package.
