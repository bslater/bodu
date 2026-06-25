---
title: Office format nuances
---

# Office format nuances

The OLE2 / Compound File Binary (CFB) container that
[`CompoundFile`](xref:Bodu.IO.Compound.CompoundFile) reads is the
structured-storage envelope behind the *legacy* binary Microsoft Office
formats — Excel `.xls`, Word `.doc`, PowerPoint `.ppt`, and Outlook
`.msg`. Each of these is a compound file whose root storage holds one or
more well-known named streams and storages. The file extension tells you
nothing the container does not; the *names of the entries* are what
identify the format.

This guide maps the well-known stream and storage names to the formats
that use them, and shows how to identify a format by walking the root
storage. One boundary up front:
[`Bodu.IO.Compound` exposes the envelope only](reading-compound-files.md) —
it gives you the named streams as raw bytes and does not interpret any
application format. Parsing the BIFF / Word / PowerPoint / MAPI payloads
inside those streams is a separate concern; for `.xls` specifically, point
the workbook stream at
[`Bodu.Formats.Excel.Binary`](xref:Bodu.Formats.Excel.ExcelBinaryWorkbook).

## The well-known entries per format

Every legacy Office file is a compound file. What distinguishes them is the
set of entries directly under the root storage:

| Format | Extension | Distinguishing root entry | Kind |
|---|---|---|---|
| Excel workbook | `.xls` | `Workbook` (older files: `Book`) | Stream — BIFF8 record sequence |
| Word document | `.doc` | `WordDocument` (+ `0Table` / `1Table`, `Data`) | Stream(s) |
| PowerPoint | `.ppt` | `PowerPoint Document` (+ `Current User`, `Pictures`) | Stream(s) |
| Outlook message | `.msg` | `__substg1.0_*`, `__attach_*`, `__recip_*`, `__properties_version1.0` | Streams + storages |

Notes that matter when probing:

- **Excel** keeps its entire workbook in a single `Workbook` stream — a
  flat BIFF8 record sequence. Very old files name it `Book`. Both are
  direct children of the root storage.
- **Word** stores the main text in `WordDocument` and its formatting
  tables in `0Table` or `1Table` (the active one is selected by a flag
  inside `WordDocument`), with document properties in `Data`.
- **PowerPoint** uses `PowerPoint Document` for the slide stream, plus a
  `Current User` stream and a `Pictures` stream.
- **Outlook `.msg`** is the most structured: it nests MAPI properties as a
  tree of storages. Property *values* live in `__substg1.0_<id><type>`
  streams, attachments in `__attach_version1.0_#xxxxxxxx` storages,
  recipients in `__recip_version1.0_#xxxxxxxx` storages, and the
  fixed-size property block in `__properties_version1.0`.

All four also typically carry the summary-information property-set streams
(`\x05SummaryInformation` and `\x05DocumentSummaryInformation`); see
[Reading property sets](property-sets.md) for those.

## Identifying a format from the root storage

Because the format is defined by entry names, identification is just a walk
of the root storage's direct children. `EnumerateEntries` yields a
[`CompoundEntryInfo`](xref:Bodu.IO.Compound.CompoundEntryInfo) snapshot
(carrying `Name`, `EntryType`, and `Length`) for every child — storage
*and* stream:

```csharp
using Bodu.IO.Compound;

static string IdentifyOfficeFormat(CompoundFile file)
{
    var names = new HashSet<string>(StringComparer.Ordinal);
    foreach (CompoundEntryInfo entry in file.RootStorage.EnumerateEntries())
        names.Add(entry.Name);

    if (names.Contains("Workbook") || names.Contains("Book"))
        return "Excel (.xls)";

    if (names.Contains("WordDocument"))
        return "Word (.doc)";

    if (names.Contains("PowerPoint Document"))
        return "PowerPoint (.ppt)";

    // .msg uses prefixed entries rather than one fixed name.
    if (names.Any(n => n.StartsWith("__substg1.0_", StringComparison.Ordinal))
        || names.Contains("__properties_version1.0"))
        return "Outlook message (.msg)";

    return "Unknown compound file";
}
```

Note that compound-file names are matched case-insensitively by the
container itself; the `HashSet` above uses ordinal comparison only because
the well-known names have a fixed canonical casing. Probe the container
first so you do not pay an open on a non-compound file:

```csharp
using Bodu.IO.Compound;

using FileStream source = File.OpenRead(path);
if (!CompoundFile.IsCompoundFile(source))
    return;   // not an OLE2 file at all — e.g. a .xlsx (ZIP) or plain text

using CompoundFile file = CompoundFile.Open(source, leaveOpen: true);
Console.WriteLine(IdentifyOfficeFormat(file));
```

`IsCompoundFile` inspects only the eight-byte OLE2 signature and restores
the stream position, so it is cheap to call ahead of a full open. A
modern `.xlsx` / `.docx` / `.pptx` is a ZIP archive, **not** a compound
file, and returns `false` here — these formats are out of scope for
`Bodu.IO.Compound`.

## Walking a `.msg` storage tree

Outlook messages are the one format whose interesting data is nested below
the root, so they exercise `EnumerateStorages` and the `TryOpenStorage` /
`TryOpenStream` pair on
[`CompoundStorage`](xref:Bodu.IO.Compound.CompoundStorage). Attachments and
recipients are child storages; their property values are
`__substg1.0_*` streams within them:

```csharp
using Bodu.IO.Compound;

using CompoundFile msg = CompoundFile.OpenRead("message.msg");

// Subject is property 0x0037, stored as Unicode text (type 001F).
if (msg.RootStorage.TryOpenStream("__substg1.0_0037001F", out CompoundStream? subject))
    using (subject)
        Console.WriteLine($"Subject is {subject.ReadAllBytes().Length} UTF-16 bytes");

// Attachments are child storages named __attach_version1.0_#xxxxxxxx.
foreach (CompoundStorage attach in msg.RootStorage.EnumerateStorages())
{
    if (!attach.Name.StartsWith("__attach_version1.0_", StringComparison.Ordinal))
        continue;

    // 0x3704 is the attachment file name; 0x3701 the attachment data.
    if (attach.TryOpenStream("__substg1.0_3704001F", out CompoundStream? name))
        using (name)
            Console.WriteLine($"attachment name stream: {name.ReadAllBytes().Length} bytes");
}
```

The `__substg1.0_` suffix encodes the MAPI property tag: the first four hex
digits are the property id and the last four the property type (`001F` =
Unicode string, `0102` = binary, and so on). `Bodu.IO.Compound` hands you
those streams as bytes — decoding the MAPI property model from them is the
caller's job.

## Reading an `.xls` workbook

For `.xls` you *can* open the `Workbook` stream directly and read its BIFF8
records yourself, but you do not have to — that is exactly what
[`Bodu.Formats.Excel.Binary`](xref:Bodu.Formats.Excel.ExcelBinaryWorkbook)
exists for. It builds on `Bodu.IO.Compound`, locates the `Workbook`
stream, and exposes worksheet cell values without you touching the record
layer:

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("book.xls");
foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
    Console.WriteLine($"{sheet.Index}: {sheet.Name}");
```

Reach into the raw `Workbook` stream through `Bodu.IO.Compound` only when
you need bytes the higher-level reader does not surface; for ordinary cell
extraction, the Excel package is the right entry point. See
[Reading compound files](reading-compound-files.md) for the byte-level
stream access pattern.

## See also

- [Reading compound files](reading-compound-files.md) — opening a container, walking the hierarchy, and reading a stream's bytes.
- [Buffered vs streaming access](streaming-and-buffering.md) — the `CompoundStream` cursor for large payloads.
- [Reading property sets](property-sets.md) — the `\x05SummaryInformation` streams these formats carry.
- API reference — <xref:Bodu.IO.Compound.CompoundFile>, <xref:Bodu.IO.Compound.CompoundStorage>, <xref:Bodu.IO.Compound.CompoundEntryInfo>, and <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> for `.xls`.
