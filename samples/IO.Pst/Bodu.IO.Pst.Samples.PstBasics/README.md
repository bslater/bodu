# Bodu.IO.Pst.Samples.PstBasics

A console walk-through of the PST stack, from the raw container to the mail-store view:

Run it from the repository root:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

Everything runs offline against the committed `Data/sample1.pst` (Unicode
format) and `Data/sample2.pst` (ANSI format), both Microsoft pstsdk test-corpus
files under Apache-2.0 — see `Data/NOTICE.md` for provenance. PST files cannot
be authored by this library, so the sample ships real fixtures rather than
generating them; point `Program.SamplePath` or `Program.AnsiSamplePath` at any
other `.pst` of either format to explore your own archive.

## Scenario 1 — DetectAndOpen

**Intent.** PST is not one format — the Unicode and ANSI variants differ in the width of nearly every field, so the layout has to be established from the header before anything else can be read.

**What it does.** Detects whether a file is a PST before opening it, then opens one and reports the format variant and the content encoding the header declared, over both fixtures.

**What to expect.**

```text
--- Detection and the open handshake ---

  sample1.pst : IsPstFile = True
  sample2.pst : IsPstFile = True
  plain text  : IsPstFile = False

  sample1.pst
  format          : Unicode
  content encoding: Permute
  node census     : Internal x12, NormalFolder x5, SearchFolder x2, NormalMessage x1, SearchUpdateQueue x2, SearchCriteria x2, ReceiveFolderTable x1, OutgoingQueueTable x1, HierarchyTable x6, ContentsTable x6, AssociatedContentsTable x6, SearchContentsTable x3, AttachmentTable x1, RecipientTable x1, 22 x1, 23 x1, 24 x1
  sample2.pst
  format          : Ansi
  content encoding: Permute
  node census     : Internal x12, NormalFolder x5, SearchFolder x2, NormalMessage x1, SearchUpdateQueue x2, SearchCriteria x2, ReceiveFolderTable x1, OutgoingQueueTable x1, HierarchyTable x6, ContentsTable x6, AssociatedContentsTable x6, SearchContentsTable x3, AttachmentTable x1, RecipientTable x1, 22 x1, 23 x1, 24 x1
```

## Scenario 2 — NodesAndProperties

**Intent.** This is the substrate beneath the mail-store reader, and it stops deliberately short of MAPI semantics: it reports a property's tag and wire type, not what Outlook would call it.

**What it does.** Enumerates nodes from the node database, reads a property context and a table context over them, and reports the wire-typed values.

**What to expect.**

```text
--- Raw nodes - property and table contexts ---

  message store (0x21) property context:
  0x0E34 (wire 0x0102): 24 bytes
  0x0E38 (wire 0x0003): 4 bytes
  0x0FF9 (wire 0x0102): 16 bytes
  0x3001 (wire 0x001F): "sample1"
  0x3416 (wire 0x0102): 24 bytes
  0x35DF (wire 0x0003): 4 bytes
  0x35E0 (wire 0x0102): 24 bytes
  0x35E3 (wire 0x0102): 24 bytes
  0x35E7 (wire 0x0102): 24 bytes
  0x6633 (wire 0x000B): 1 bytes
  0x66FA (wire 0x0003): 4 bytes
  0x67FF (wire 0x0003): 4 bytes
  root hierarchy table: 4 rows x 13 columns
  row 0x00008022 -> child NormalFolder
  row 0x00008042 -> child NormalFolder
  row 0x00002223 -> child SearchFolder
  row 0x00080023 -> child SearchFolder
```

## Scenario 3 — StreamingAndValidation

**Intent.** A PST can be tens of gigabytes, so streaming node data is a requirement rather than an optimisation — and real files fail their own checksums, which is why validation is a level rather than a flag.

**What it does.** Reads node data as a stream, opens a file under each validation level, and shows how different kinds of failure are classified.

**What to expect.**

```text
--- Streaming, validation levels, and failure classification ---

  largest node 0x00200024 holds 4198 bytes
  streamed 4198 bytes in 16 KiB chunks under Strict validation
  truncated copy rejected: PstFileFormatException (InvalidBlock)
```

## Scenario 4 — ReadMailStore

**Intent.** This is what the node layer is for, and the contrast is the point: the same bytes that were a tree of tagged blobs become a folder hierarchy of messages.

**What it does.** Opens the same files through `Bodu.Formats.Outlook.Pst`, walks the folder hierarchy, and reads messages with their decoded properties, recipients, attachments and bodies.

**What to expect.**

```text
--- The mail-store view ---

  store (Unicode): sample1
  [] (0 messages)
    [Top of Outlook data file] (0 messages)
      [Deleted Items] (0 messages)
      [Sample1] (1 messages)
        Here is a sample message — Terry Mahaffey
        to Terry Mahaffey
        attachment: ByValue: leah_thumper.jpg
        body: With a sample attachment. It’s my daughter and our puppy. Ar…
    [Search Root] (0 messages)
named property 0x8000 resolves to {00062002-0000-0000-c000-000000000046}:0x00008205

  store (ANSI)   : sample2
  [] (0 messages)
    [Top of Outlook data file] (0 messages)
      [Deleted Items] (0 messages)
      [Sample2] (1 messages)
        Here is a sample message — Terry Mahaffey
        to Terry Mahaffey
        attachment: ByValue: leah_thumper.jpg
        body: With a sample attachment. It's my daughter and our puppy. Ar…
    [Search Root] (0 messages)
```

## Layout

```text
Bodu.IO.Pst.Samples.PstBasics/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  Data/sample1.pst                    # committed Unicode-format fixture
  Data/sample2.pst                    # committed ANSI-format fixture
  Data/NOTICE.md                      # fixture provenance (Microsoft pstsdk, Apache-2.0)
  Scenarios/DetectAndOpen.cs
  Scenarios/NodesAndProperties.cs
  Scenarios/StreamingAndValidation.cs
  Scenarios/ReadMailStore.cs
```
