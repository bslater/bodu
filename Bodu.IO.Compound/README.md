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
- Per-entry metadata via `CompoundEntryInfo` (the `STATSTG` analogue): class id, state
  bits, creation / modified time stamps, and red-black node color.
- OLE property-set parsing **and writing** (`Bodu.IO.Compound.PropertySets`): `OlePropertySet` /
  `OlePropertyValue` (PROPVARIANT) plus the strongly-typed `SummaryInformation` /
  `DocumentSummaryInformation` views and `…Builder` authors, including user-defined custom properties.
- A mutable, JsonNode-style object model (`Bodu.IO.Compound.Nodes`) for **authoring**:
  build or `Load` a tree of `CompoundStorageNode` / `CompoundStreamNode`, mutate it, then
  `Save` / `ToArray` to a conforming container (v3 or v4). `CompoundWriter` offers a
  low-level imperative alternative. Output is verified byte-for-byte and cross-checked
  against the independent `olefile` and OpenMcdf parsers.
- **Bounded-memory streaming writes**: `Save(Stream)` emits one sector at a time and
  large payloads can be sourced on demand via `CompoundStreamNode.CreateFromFile(name, path)`
  or `Create(name, Func<Stream>, length)` (and the matching `AddStreamFromFile` /
  `WriteStreamFromFile`), so multi-gigabyte containers serialize without being buffered whole
  in memory.
- Stable, message-independent failure classification through
  `CompoundFileFormatException.Category` (`CompoundFileError`) and a
  `CompoundFileSerializationException` for authoring errors.

```csharp
using Bodu.IO.Compound.Nodes;
using Bodu.IO.Compound.PropertySets;

var root = CompoundStorageNode.CreateRoot();
root.AddStorage("Storage 1").AddStream("Stream 1", new byte[] { 1, 2, 3 });
root.AddStream(SummaryInformation.StreamName,
    new SummaryInformationBuilder { Title = "Report", Author = "Ada" }.ToArray());
root.Save(stream);   // writes an OLE2 / CFB file
```

## Out of scope

In-place transacted editing (the COM `IStorage`/`Commit` model), encryption, and
damaged-file recovery are out of scope; authoring is done by building and saving a
detached object model rather than mutating a file in place.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
