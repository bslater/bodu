---
title: Authoring compound files
---

# Authoring compound files

**Bodu.IO.Compound** writes as well as reads. This guide covers building a Compound File Binary (CFB) container from scratch, embedding nested storages and named streams, and round-tripping OLE property sets. As with the read path, the library has no application-format knowledge — it lays out the storage tree and the raw bytes you give it, and leaves the format payload to you.

There are two authoring paths, and they suit different jobs:

| Path | Reach for it when | Entry point |
|---|---|---|
| **Builder** | You are assembling a container from scratch, or editing one you loaded, and want to serialize it once at the end. | <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> |
| **In-place** | You want a writable <xref:Bodu.IO.Compound.CompoundFile> whose streams you mutate through the familiar `Stream` surface, finalized by a single `Commit`. | <xref:Bodu.IO.Compound.CompoundFile.Create*> |

The builder path is the default recommendation: it is a detached, in-memory object model with no open file handle until you serialize. Reach for the in-place path when you want to write a stream's bytes through a real <xref:System.IO.Stream> cursor.

## Pattern 1 — assemble a container with the builder

<xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder.CreateRoot> returns a detached root. `AddStream` adds a named stream from an in-memory payload; `AddStorage` adds a nested storage and returns it so you can descend. Serialize with `Save`, `WriteTo`, or `ToArray`.

```csharp
using Bodu.IO.Compound.Builders;

var root = CompoundStorageBuilder.CreateRoot();

// A stream directly under the root.
root.AddStream("Workbook", workbookBytes);

// A nested storage with its own stream.
CompoundStorageBuilder storage = root.AddStorage("Storage 1");
storage.AddStream("Nested", new byte[] { 1, 2, 3 });

root.Save("out.xls");                 // write to a file
byte[] bytes = root.ToArray();        // …or materialize the bytes
```

`AddStream` accepts a `ReadOnlyMemory<byte>`, so a `byte[]` binds directly. Both `AddStream` and `AddStorage` throw <xref:Bodu.IO.Compound.CompoundFileSerializationException> if a child with the same name already exists in that storage.

## Pattern 2 — defer large payloads

When a stream's bytes come from a file or another source you do not want to hold in memory, add it with a deferred opener or directly from a path. The bytes are read only when the container is serialized.

<!-- compile -->
```csharp
using Bodu.IO.Compound.Builders;

var root = CompoundStorageBuilder.CreateRoot();

// Deferred: the delegate is invoked at serialization time.
root.AddStream("Pictures", () => File.OpenRead("media.bin"), length: new FileInfo("media.bin").Length);

// Or straight from a file path.
root.AddStreamFromFile("Data", "payload.dat");

root.Save("out.cfb");
```

## Pattern 3 — create and mutate a file in place

<xref:Bodu.IO.Compound.CompoundFile.Create*> returns a writable <xref:Bodu.IO.Compound.CompoundFile>. Its `RootStorage.CreateStream` returns a writable <xref:Bodu.IO.Compound.CompoundStream> — a real read-write `Stream` (`CanWrite` is `true`) — and `CreateStorage` adds a nested storage. Edits are staged in memory and persisted by `Commit`.

```csharp
using Bodu.IO.Compound;

using FileStream output = File.Create("book.xls");
using CompoundFile file = CompoundFile.Create(output);

using (CompoundStream stream = file.RootStorage.CreateStream("Workbook"))
{
    stream.Write(workbookBytes);
}

CompoundStorage storage = file.RootStorage.CreateStorage("Storage 1");
using (CompoundStream nested = storage.CreateStream("Nested"))
{
    nested.Write(new byte[] { 1, 2, 3 });
}

file.Commit();   // nothing is written to `output` until this call
```

A writable <xref:Bodu.IO.Compound.CompoundStorage> also exposes its directory-entry metadata as settable properties — <xref:Bodu.IO.Compound.CompoundStorage.ClassId>, <xref:Bodu.IO.Compound.CompoundStorage.CreationTime>, <xref:Bodu.IO.Compound.CompoundStorage.ModifiedTime>, and <xref:Bodu.IO.Compound.CompoundStorage.StateBits>. The root storage's `ClassId` is the conventional file-type discriminator for OLE2-based formats:

```csharp
file.RootStorage.ClassId = new Guid("00020820-0000-0000-c000-000000000046");   // Excel workbook CLSID
```

Metadata is never stamped automatically — `Commit` leaves timestamps untouched, so a value only changes when you set it (and byte-identical re-saves stay possible). Per MS-CFB, only storage entries carry this metadata; stream entries are always written with zero CLSID, timestamps, and state bits.

Disposing the file **without** calling `Commit` discards the staged edits — `Commit` is the only thing that writes to the destination. `Revert` drops staged edits explicitly; `IsDirty` reports whether any are pending.

## Pattern 4 — edit an existing container

To change a file you already have, load it into a builder, mutate the tree, and re-serialize. <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder.FromFile*> copies from an open <xref:Bodu.IO.Compound.CompoundFile> (pass `lazy: true` to defer reading stream payloads until serialization); `Load` reads from a stream.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;

CompoundStorageBuilder root;
using (CompoundFile source = CompoundFile.OpenRead("in.xls"))
    root = CompoundStorageBuilder.FromFile(source);

root.Remove("Obsolete");                       // drop a stream
root.AddStream("Added", new byte[] { 9, 9 });  // add another
root.Rename("Storage 1", "Archive");           // rename a child

root.Save("out.xls");
```

## Pattern 5 — write document property sets

The <xref:Bodu.IO.Compound.PropertySets> namespace authors OLE property sets as well as reading them. Build a <xref:Bodu.IO.Compound.PropertySets.SummaryInformationBuilder> from typed fields, serialize it with `ToArray`, and embed it at the conventional stream name. `CompoundFile.TryGetSummaryInformation` reads it back.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

var summary = new SummaryInformationBuilder
{
    Title = "Quarterly report",
    Author = "Ada",
    WordCount = 1280,
    CreateTime = DateTimeOffset.UtcNow,
};

var root = CompoundStorageBuilder.CreateRoot();
root.AddStream(SummaryInformation.StreamName, summary.ToArray());   // "\x05SummaryInformation"
root.AddStream("Workbook", workbookBytes);

byte[] cfb = root.ToArray();

// Round-trip: read the metadata back.
using CompoundFile file = CompoundFile.Open(new MemoryStream(cfb));
if (file.TryGetSummaryInformation(out SummaryInformation? read))
    Console.WriteLine(read.Title);   // "Quarterly report"
```

<xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformationBuilder> works the same way for the `\x05DocumentSummaryInformation` stream, and the lower-level <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> / <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> types let you author non-standard properties. See [Reading property sets](property-sets.md) for the read side.

## Options and rules

- **Sector version.** <xref:Bodu.IO.Compound.Builders.CompoundBuildOptions> selects the on-disk version: `V3` (512-byte sectors) is the most compatible default; `V4` (4096-byte sectors) suits larger containers. Pass it to any serializer, e.g. `root.Save("out.cfb", new CompoundBuildOptions { Version = CompoundFileVersion.V4 })`.
- **Nesting depth.** `CompoundBuildOptions.MaxDepth` bounds storage nesting (default 64); exceeding it throws <xref:Bodu.IO.Compound.CompoundFileSerializationException>.
- **Names.** An entry name is at most 31 UTF-16 code units, non-empty, and must not contain `/` or null characters. Names are compared case-insensitively by default (per the CFB format); set `NameComparisonCaseSensitive` on <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilderOptions> via `CreateRoot(options)` to change that. Duplicate or invalid names throw <xref:Bodu.IO.Compound.CompoundFileSerializationException>.
- **Errors.** Authoring failures surface through <xref:Bodu.IO.Compound.CompoundFileSerializationException>; a malformed container encountered while loading one to edit surfaces through <xref:Bodu.IO.Compound.CompoundFileFormatException>.

## Where to go next

- **[Reading compound files](reading-compound-files.md)** — the open → navigate → read recipe the write path mirrors.
- **[Reading property sets](property-sets.md)** — the read side of the summary-information streams.
- **[Buffered vs streaming access](streaming-and-buffering.md)** — the read strategy and the `CompoundStream` cursor.
- [Bodu.IO.Compound API reference](xref:Bodu.IO.Compound) and the [Bodu.IO.Compound.Builders](xref:Bodu.IO.Compound.Builders) authoring types.
