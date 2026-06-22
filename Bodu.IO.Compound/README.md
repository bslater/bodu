# Bodu.IO.Compound

A small, dependency-free reader for the **OLE2 / Compound File Binary (CFB)** container
format — the structured-storage envelope behind legacy Microsoft Office files such as
`.xls`, `.doc`, `.ppt`, and `.msg`.

The reader understands the *container*: it navigates the storage hierarchy, exposes the
metadata of every entry, materializes the bytes of any named stream, and parses OLE
**property sets** (summary information). It applies no interpretation to application stream
contents — turning those bytes into a workbook, a document, or anything else is the
consumer's job.

```csharp
using Bodu.IO.Compound;

using var file = CompoundFile.Open(stream);

// Navigate the storage hierarchy (the IStorage / IStream model).
foreach (CompoundEntryInfo entry in file.RootStorage.EnumerateEntries())
    Console.WriteLine($"{entry.Name} ({entry.EntryType}, {entry.Length} bytes, clsid {entry.ClassId})");

if (file.RootStorage.TryOpenStream("Workbook", out CompoundStreamEntry? workbook))
{
    using CompoundStream stream = workbook.Open();
    // read stream.AsMemory() or use it as a Stream
}

// Read document metadata from the OLE property sets.
if (file.TryGetSummaryInformation(out var summary))
    Console.WriteLine($"{summary.Title} by {summary.Author}, created {summary.CreateTime}");
```

## Capabilities

- Header, sector-size, and signature validation; `CompoundFile.IsCompoundFile` for a
  non-destructive signature probe.
- Regular FAT traversal (including extended DIFAT sectors) and mini-FAT / mini-stream
  resolution, with cycle and out-of-range detection.
- A navigable storage hierarchy (`CompoundStorage` / `CompoundStreamEntry`) with child
  lookups scoped per storage — the managed counterpart of COM `IStorage` / `IStream`.
- **Bounded-memory streaming reads**: `CompoundFile.Open(stream, buffered: false)` reads sectors on
  demand from a seekable stream, and `CompoundStreamEntry.Open()` returns a lazy `CompoundStream` for
  large streams, so a multi-gigabyte file can be read without buffering it whole.
  `CompoundFileBuilder.FromFile(file, lazy: true)` reads into deferred nodes for a fully streamed read → re-save copy.
- Per-entry metadata via `CompoundEntryInfo` (the `STATSTG` analogue): class id, state
  bits, creation / modified time stamps, and red-black node color.
- OLE property-set parsing **and writing** (`Bodu.IO.Compound.PropertySets`): `OlePropertySet` /
  `OlePropertyValue` (PROPVARIANT) plus the strongly-typed `SummaryInformation` /
  `DocumentSummaryInformation` views and `…Builder` authors, including user-defined custom properties.
- A single authoring surface, `CompoundFileBuilder`: populate its `Root` with a
  JsonNode-style tree of `CompoundStorageNode` / `CompoundStreamNode` children (or
  `CompoundFileBuilder.Load` an existing file into one), then `WriteTo` / `Save` / `ToArray`
  to a conforming container (v3 or v4). Output is verified byte-for-byte and cross-checked
  against the independent `olefile` and OpenMcdf parsers.
- **Bounded-memory streaming writes**: `CompoundFileBuilder.WriteTo(Stream)` emits one sector
  at a time and large payloads can be sourced on demand via
  `CompoundStreamNode.CreateFromFile(name, path)` or `Create(name, Func<Stream>, length)` (and
  the matching `CompoundStorageNode.AddStreamFromFile`), so multi-gigabyte containers serialize
  without being buffered whole in memory.
- Stable, message-independent failure classification through
  `CompoundFileFormatException.Category` (`CompoundFileError`) and a
  `CompoundFileSerializationException` for authoring errors.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

var builder = new CompoundFileBuilder();
builder.Root.AddStorage("Storage 1").AddStream("Stream 1", new byte[] { 1, 2, 3 });
builder.Root.AddStream(SummaryInformation.StreamName,
    new SummaryInformationBuilder { Title = "Report", Author = "Ada" }.ToArray());
builder.WriteTo(stream);   // writes an OLE2 / CFB file
```

## Out of scope

In-place transacted editing (the COM `IStorage`/`Commit` model), encryption, and
damaged-file recovery are out of scope; authoring is done by building and saving a
detached object model rather than mutating a file in place.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
