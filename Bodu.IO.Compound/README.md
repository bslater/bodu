# Bodu.IO.Compound

A small, dependency-free reader for the **OLE2 / Compound File Binary (CFB)** container
format — the structured-storage envelope behind legacy Microsoft Office files such as
`.xls`, `.doc`, `.ppt`, and `.msg`.

The reader understands only the *container*: it surfaces the directory of storages and
streams and materializes the bytes of any named stream from its sector chain. It applies
no interpretation to the contents of a stream — turning those bytes into a workbook, a
document, or anything else is the consumer's job.

```csharp
using Bodu.IO.Compound;

using var file = CompoundBinaryFile.Open(stream);

foreach (CompoundDirectoryEntry entry in file.Entries)
    Console.WriteLine($"{entry.Name} ({entry.Type}, {entry.Size} bytes)");

if (file.TryGetStream("Workbook", out CompoundStream? workbook))
{
    using (workbook)
    {
        // read workbook.AsMemory() or use it as a Stream
    }
}
```

## Capabilities

- Header, sector-size, and signature validation.
- Regular FAT traversal, including extended DIFAT sectors.
- Mini-FAT and mini-stream resolution for streams below the cutoff.
- Named-stream lookup (`TryGetStream` / `GetStream`) returning a read-only, seekable
  `CompoundStream`.

## Out of scope

Writing, mutation, encryption, and damaged-file recovery are intentionally not supported.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
