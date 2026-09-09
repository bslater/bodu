---
uid: Bodu.IO.Compound.Builders
---

![Bodu.IO.Compound](~/images/hero-io-compound.svg)

## Purpose

**Bodu.IO.Compound.Builders** is the detached authoring object model of <xref:Bodu.IO.Compound>: a mutable tree of storages and streams that you assemble in memory — from scratch, or by loading an existing container — and serialize once at the end. It is the authoring counterpart of the read-only <xref:Bodu.IO.Compound.CompoundStorage> / <xref:Bodu.IO.Compound.CompoundStream> navigation surface and is shaped after the `JsonNode` family: <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> is the `JsonObject` analogue (children keyed by name), <xref:Bodu.IO.Compound.Builders.CompoundStreamBuilder> the `JsonValue` leaf, and the abstract <xref:Bodu.IO.Compound.Builders.CompoundEntryBuilder> their common base. A compound file has no array or null concept, so those are the only two node kinds.

The model is **staged**: no file handle is held while you build, a node belongs to at most one parent at a time, and nothing is written until `Save` / `WriteTo` / `ToArray` lays the tree out. This is the recommended path for authoring a container; the in-place path (<xref:Bodu.IO.Compound.CompoundFile.Create*> plus `Commit`) suits writing through a real `Stream` cursor instead. Large payloads need not be held in memory — a **deferred** stream node holds a re-openable source of known length and copies it straight to the output during serialization.

## Static documentation

- **[Authoring compound files](~/guides/io-compound/authoring-compound-files.md)** — the builder and in-place paths side by side, deferred payloads, editing a loaded container, options and rules.
- **[Bodu.IO.Compound introduction](~/docs/io-compound/index.md)**, **[core concepts](~/docs/io-compound/concepts.md)**, and **[getting started](~/docs/io-compound/getting-started.md)** — the container vocabulary and the read path the builders mirror.
- **[Reading property sets](~/guides/io-compound/property-sets.md)** — authoring the summary-information payload a builder stream typically carries.

## Key types

**Nodes**

- <xref:Bodu.IO.Compound.Builders.CompoundEntryBuilder> — the abstract node. `Name`, `Parent`, `Root` (the topmost ancestor), and `EntryType` (<xref:Bodu.IO.Compound.CompoundEntryType>); the directory-entry metadata `ClassId` (CLSID; `Guid.Empty` when unassigned), `CreationTime` / `ModifiedTime` (`null` when unrecorded), and `StateBits` (raw user-defined flags); the casts `AsStorage()` / `AsStream()` (each throws <xref:System.InvalidOperationException> for the other kind); and `DeepClone()`, an independent, parentless copy of the node and its descendants.
- <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> — a storage node and an `IDictionary<string, CompoundEntryBuilder>` in one. Factories `CreateRoot()` / `CreateRoot(options)` (a detached root named `Root Entry`), `Load(Stream)` (a tree mirroring an existing container, payloads copied), and `FromFile(CompoundFile, lazy)` (mirror an open file; `lazy: true` builds deferred nodes that read from the file on demand, which must then stay open). Authoring: `AddStorage(name)`, `AddStream(name, ReadOnlyMemory<byte>)`, `AddStream(name, Func<Stream> openRead, long length)`, `AddStreamFromFile(name, path)`, `Remove(name)`, `Rename(oldName, newName)`, `Clear()`. Lookup: `ContainsName`, `TryGetStorage` / `TryGetStream` (kind-checked), `EnumerateStorages()` / `EnumerateStreams()`, plus the dictionary surface — `Keys`, `Values`, `Count`, the indexer, `Add(key, value)`, `ContainsKey`, `TryGetValue`, enumeration of `KeyValuePair<string, CompoundEntryBuilder>`. Serialization: `Save(path, options)`, `WriteTo(Stream, options)`, `WriteTo(IBufferWriter<byte>, options)`, `ToArray(options)`.
- <xref:Bodu.IO.Compound.Builders.CompoundStreamBuilder> — a stream node. `Content` (the payload; reading it on a deferred node materializes the source, setting it makes the node in-memory), `Length` (reported without opening a deferred source), `SetContent(ReadOnlySpan<byte>)` / `SetContent(string, Encoding?)`. Factories for a detached node to `Add` to a storage: `Create(name, ReadOnlyMemory<byte>)`, `Create(name, string text, Encoding?)`, `Create(name, Stream source)`, `Create(name, Func<Stream> openRead, long length)` (deferred), `CreateFromFile(name, path)` (deferred).

**Options**

- <xref:Bodu.IO.Compound.Builders.CompoundBuildOptions> — passed to any serializer. `Version` selects the sector size (<xref:Bodu.IO.Compound.CompoundFileVersion>: `V3` 512-byte sectors — the default and most compatible; `V4` 4096-byte sectors for larger containers); `MaxDepth` bounds storage nesting (`0` selects the built-in default of 64).
- <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilderOptions> — passed to `CreateRoot(options)`. `NameComparisonCaseSensitive` (`false` by default, matching the compound-file format's case-insensitive names).

## Example

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

// Assemble a container: an in-memory stream, a deferred stream, a nested storage, and entry metadata.
var root = CompoundStorageBuilder.CreateRoot();
root.ClassId = new Guid("00020820-0000-0000-C000-000000000046");   // the Excel 97-2003 workbook CLSID

root.AddStream(SummaryInformation.StreamName, new SummaryInformationBuilder { Title = "Report" }.ToArray());
root.AddStreamFromFile("Workbook", "workbook.bin");                // read only at serialization time

CompoundStorageBuilder pictures = root.AddStorage("Pictures");
CompoundStreamBuilder logo = pictures.AddStream("Logo", File.ReadAllBytes("logo.png"));
logo.ModifiedTime = DateTimeOffset.UtcNow;

// The dictionary surface: same tree, IDictionary-shaped access.
if (root.TryGetStream("Workbook", out CompoundStreamBuilder? workbook))
    Console.WriteLine($"{workbook.Name}: {workbook.Length} bytes");
Console.WriteLine(string.Join(", ", root.Keys));

root.Save("report.xls", new CompoundBuildOptions { Version = CompoundFileVersion.V3 });
```

```csharp
using Bodu.IO.Compound.Builders;

// Edit a loaded container: mirror it, change it, write it back — and keep an independent copy.
CompoundStorageBuilder root;
using (FileStream source = File.OpenRead("report.xls"))
    root = CompoundStorageBuilder.Load(source);

CompoundStorageBuilder snapshot = root.DeepClone().AsStorage();   // detached, parentless copy

root.Rename("Pictures", "Media");
root.Remove("Workbook");
root["Notes"] = CompoundStreamBuilder.Create("Notes", "edited", System.Text.Encoding.UTF8);

byte[] edited = root.ToArray();
byte[] original = snapshot.ToArray();
```

## Notes

- **Staged, not streamed.** The tree is an ordinary object graph until serialization; there is nothing to commit or revert, and nothing touches the destination until `Save` / `WriteTo` / `ToArray`. Serialization order is determined by the builder, not by insertion order.
- **Deferred payloads.** `AddStream(name, openRead, length)`, `AddStreamFromFile`, `CompoundStreamBuilder.Create(name, openRead, length)`, `CreateFromFile`, and `FromFile(file, lazy: true)` create nodes whose bytes are read only while serializing; `Length` is known without opening the source, and reading `Content` materializes it once. Keep a lazily mirrored `CompoundFile` open for as long as the tree — or any clone of its nodes — is read or serialized.
- **Names and errors.** An entry name is at most 31 UTF-16 code units, non-empty, and free of `/` and null characters; comparison is case-insensitive unless `NameComparisonCaseSensitive` is set on the root. A duplicate or invalid name — from `AddStorage`, `AddStream`, `Add`, the indexer, or `Rename` — and a tree that cannot be represented (nesting past `MaxDepth`) throw <xref:Bodu.IO.Compound.CompoundFileSerializationException>; a malformed source given to `Load` throws <xref:Bodu.IO.Compound.CompoundFileFormatException>.
- **Metadata round-trips.** `ClassId`, `CreationTime`, `ModifiedTime`, and `StateBits` are written into the directory entry and are the same fields <xref:Bodu.IO.Compound.CompoundEntryInfo> reports on read.
- **See also:** the [authoring guide](~/guides/io-compound/authoring-compound-files.md), the container namespace <xref:Bodu.IO.Compound>, and the property-set authoring types in <xref:Bodu.IO.Compound.PropertySets>.
