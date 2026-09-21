---
title: Streaming and validation
---

# Streaming and validation

Two things separate reading a PST you trust from reading one you do not: how much of it you let into memory at once, and how much of it you insist is well-formed. This guide covers both — the streaming twins of every payload accessor, the four knobs on <xref:Bodu.IO.Pst.PstFileOptions>, what each <xref:Bodu.IO.Pst.PstValidationLevel> actually checks, and the complete <xref:Bodu.IO.Pst.PstFileError> catalogue so you can say *why* a file was rejected.

The samples run against `sample1.pst` from the [runnable PST sample](../../samples/io-pst.md), copied as `archive.pst`; the quoted output is what they print.

## Pattern 1 — price a payload, then stream it

Every payload has a cheap length and a streaming read beside its buffered convenience. On a <xref:Bodu.IO.Pst.PstNode>: `DataLength` sums the data tree's leaf lengths from its index blocks without reading any leaf; `ReadAllBytes()` flattens the tree into one array; `OpenDataStream()` returns a seekable read-only <xref:System.IO.Stream> that keeps one leaf block resident, so the logical payload can exceed available memory.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

PstNodeInfo biggest = file.EnumerateNodes().MaxBy(n => n.DataLength)!;
PstNode node = file.GetNode(biggest.NodeId);

Console.WriteLine($"{node}: {node.DataLength} bytes");   // priced from the data tree's index blocks only

using Stream data = node.OpenDataStream();                // one leaf block resident at a time
byte[] buffer = new byte[16 * 1024];
long total = 0;
int read;
while ((read = data.Read(buffer, 0, buffer.Length)) > 0)
    total += read;

Console.WriteLine($"streamed {total} bytes; ReadAllBytes would allocate {node.ReadAllBytes().Length}");

// 0x00200024 (NormalMessage): 4198 bytes
// streamed 4198 bytes; ReadAllBytes would allocate 4198
```

The interesting payloads are usually not node data but *property values* — an attachment's bytes live behind `PidTagAttachDataBinary` (`0x3701`) in the attachment object's property context. <xref:Bodu.IO.Pst.PstPropertyContext> offers the same pair: `TryGetValueLength(id, out length)` reads only the value's index blocks, and `TryOpenValueStream(id, out stream)` serves a subnode-resident value block by block — the same stream `OpenDataStream` returns — while a heap-resident value is served from the decoded heap bytes.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

foreach (PstNodeInfo info in file.EnumerateNodes())
{
    if (info.NodeId.Type != PstNodeType.NormalMessage || !info.HasSubnodes)
        continue;

    PstNode message = file.GetNode(info.NodeId);
    foreach (PstNodeInfo child in message.EnumerateSubnodes())
    {
        if (child.NodeId.Type != PstNodeType.Attachment || !message.TryGetSubnode(child.NodeId, out PstNode? attachment))
            continue;

        PstPropertyContext properties = attachment.ReadPropertyContext();

        // 0x3701 PidTagAttachDataBinary: price it without reading it...
        if (!properties.TryGetValueLength(0x3701, out long length))
            continue;

        Console.WriteLine($"{message.Id} -> {child.NodeId}: {length}-byte payload");

        // ...then copy it block by block; MaxNodeDataLength never applies here.
        if (properties.TryOpenValueStream(0x3701, out Stream? payload))
        {
            using (payload)
            using (FileStream target = File.Create($"{child.NodeId.Index}.bin"))
                payload.CopyTo(target);
        }
    }
}

// 0x00200024 -> 0x00008025: 93142-byte payload
```

Both `Try*` members return `false` when the property is absent and throw <xref:Bodu.IO.Pst.PstFileFormatException> when its storage is malformed. Inline and fixed-width values are exposed through the stream as their raw little-endian bytes for uniformity; the typed accessors on <xref:Bodu.IO.Pst.PstPropertyValue> remain the natural way to read those.

Table cells have the identical pair on <xref:Bodu.IO.Pst.PstTableRow> — `TryGetCellLength(id, out length)` and `TryOpenCellStream(id, out stream)` — which answer `false` when the column is missing *or* the row's existence bitmap marks the cell absent:

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

foreach (PstNodeInfo info in file.EnumerateNodes())
{
    if (info.NodeId.Type != PstNodeType.ContentsTable)
        continue;

    PstTableContext contents = file.GetNode(info.NodeId).ReadTableContext();
    foreach (PstTableRow row in contents.EnumerateRows())
    {
        // 0x0037 PidTagSubject as the contents table stores it: priced, then opened.
        if (row.TryGetCellLength(0x0037, out long length) && row.TryOpenCellStream(0x0037, out Stream? cell))
        {
            using (cell)
                Console.WriteLine($"{info.NodeId} row {new PstNodeId(row.RowId)}: subject cell {length} bytes, stream length {cell.Length}");
        }
    }
}

// 0x0000808E row 0x00200024: subject cell 52 bytes, stream length 52
```

> [!WARNING]
> Every stream these members return is bound to the session. After the <xref:Bodu.IO.Pst.PstFile> is disposed, each read throws <xref:System.ObjectDisposedException> — even for blocks already in the cache. Dispose the stream before the session, and never hand one across a `using` boundary that outlives the file.

| Need | Buffered | Priced | Streamed |
|---|---|---|---|
| A node's payload | `PstNode.ReadAllBytes()` | `PstNode.DataLength` | `PstNode.OpenDataStream()` |
| A property value | `PstPropertyContext.GetValue` / `TryGetValue` | `TryGetValueLength` | `TryOpenValueStream` |
| A table cell | `PstTableRow.TryGetCell` | `TryGetCellLength` | `TryOpenCellStream` |

The buffered column is governed by `MaxNodeDataLength` (below); the priced and streamed columns are not.

## `PstFileOptions`

```csharp
using Bodu.IO.Pst;

var options = new PstFileOptions
{
    ValidationLevel = PstValidationLevel.Strict,   // every CRC and trailer signature enforced
    BlockCacheSize = 512,                          // decoded pages/blocks kept (0 disables the cache)
    MaxNodeDataLength = 64L * 1024 * 1024,         // refuse to materialize a node payload above 64 MiB
    MaxDataTreeLeaves = 16_384,                    // refuse a data tree fanning out to more leaf blocks
};

using PstFile file = PstFile.Open(File.OpenRead("archive.pst"), options);
Console.WriteLine(file.Format);
```

| Property | Default | Effect | When it trips |
|---|---|---|---|
| `ValidationLevel` | `Compatible` | How much cross-checking each read performs — see the next section. | Depends on the level. |
| `BlockCacheSize` | `256` | The least-recently-used cache of decoded pages and block payloads, in entries. Each entry is at most one 8 KiB block, so the default bounds the cache near 2 MB per session. Repeated structural reads — B-tree walks, the same node's contexts read twice — are served from it instead of re-reading and re-decoding the source. `0` disables caching; negative values throw <xref:System.ArgumentOutOfRangeException>. | Not a limit. |
| `MaxNodeDataLength` | 256 MiB | The largest node payload the session **materializes**: it governs `ReadAllBytes`, the heap-on-node parse behind `ReadPropertyContext` / `ReadTableContext`, and subnode-resident property values read whole. The *declared* size is what is bounded — a crafted tree can reference the same physical block thousands of times, so the file size proves nothing. `OpenDataStream` and the `TryOpen*Stream` members are unaffected by design. Zero or negative values throw. | <xref:Bodu.IO.Pst.PstFileFormatException> with `Error == PstFileError.LimitExceeded`, at every validation level. |
| `MaxDataTreeLeaves` | 65,536 | The most leaf data blocks one node's data tree may reference (about 512 MiB of 8 KiB blocks). Enforced while the tree's internal blocks are walked, before any leaf is read, for streaming and buffered reads alike — the leaf list itself is the allocation it bounds. Zero or negative values throw. | <xref:Bodu.IO.Pst.PstFileFormatException> with `Error == PstFileError.LimitExceeded`, at every validation level. |

The mail-store reader forwards the first three from <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions> and leaves `MaxDataTreeLeaves` at its default; open the file with `PstFile` when you need to change it.

## What each validation level checks

<xref:Bodu.IO.Pst.PstValidationLevel> governs only how the reader treats *recoverable* inconsistencies. The memory-safety invariants — the magic, a declared version, a header of the right size, the sentinel byte, page and block geometry, every bounds check on every offset, and the two resource limits above — hold at every level.

| Check | `Minimal` | `Compatible` (default) | `Strict` |
|---|---|---|---|
| Header structure (magic, version, size, sentinel) | Yes | Yes | Yes |
| Header CRC (`dwCRCPartial` over 471 bytes) | — | Yes | Yes |
| Header file length within the stream | — | — | Yes |
| Page trailer type and B-tree page geometry | Yes | Yes | Yes |
| Page CRC, trailer signature, and recorded block identifier | — | — | Yes |
| Block geometry and trailer length | Yes | Yes | Yes |
| Block CRC, trailer signature, and recorded block identifier | — | — | Yes |
| Unknown wire type in a property-context record | tolerated (kept as a raw dword) | tolerated | rejected (`InvalidPropertyValue`) |
| Unordered property-context keys | re-sorted | re-sorted | rejected |
| BTree-on-heap key order | — | — | Yes |
| Row matrix holding more rows than the row index declares | tolerated | tolerated | rejected (`InvalidTableContext`) |

`Compatible` is the choice for real-world files — writers have shipped stores with stale trailer CRCs that Outlook reads happily. `Strict` is for validating a corpus or refusing tampered input. `Minimal` is for salvage: it still walks structure safely but will follow a checksum-failed page as far as the geometry allows.

## Pattern 2 — open under `Strict` and handle a truncated file

A truncated PST is the commonest damage. Under `Strict` the header's declared file length is compared with the stream at open, so the file is rejected before a single node is read:

```csharp
using Bodu.IO.Pst;

byte[] bytes = File.ReadAllBytes("archive.pst");
var strict = new PstFileOptions { ValidationLevel = PstValidationLevel.Strict };

try
{
    using var truncated = new MemoryStream(bytes, 0, bytes.Length / 2, writable: false);
    using PstFile file = PstFile.Open(truncated, strict);
    Console.WriteLine($"opened: {file.Format}");
}
catch (PstUnsupportedFormatException ex)
{
    Console.WriteLine($"unsupported: {ex.Message}");
}
catch (PstFileFormatException ex) when (ex.Error == PstFileError.InvalidHeader)
{
    Console.WriteLine($"rejected at open: {ex.Error} — {ex.Message}");
}
catch (PstFileException ex)
{
    Console.WriteLine($"rejected: {ex.Error} — {ex.Message}");
}

// rejected at open: InvalidHeader — The header declares a file length beyond the end of the stream.
```

Catch <xref:Bodu.IO.Pst.PstUnsupportedFormatException> first when you want to say "this is an OST" rather than "this is corrupt"; it is a sibling of <xref:Bodu.IO.Pst.PstFileFormatException>, not a subclass, so the order between those two does not matter, but both derive from <xref:Bodu.IO.Pst.PstFileException>, which must come last.

The tolerant default opens the same stream — the header is intact and the B-tree pages sit in the surviving half — and fails only when a read reaches past the end:

```csharp
using Bodu.IO.Pst;

byte[] bytes = File.ReadAllBytes("archive.pst");

using var truncated = new MemoryStream(bytes, 0, bytes.Length / 2, writable: false);
using PstFile file = PstFile.OpenRead(truncated);
Console.WriteLine($"opened: {file.Format}");

try
{
    int nodes = 0;
    long bytesRead = 0;
    foreach (PstNodeInfo info in file.EnumerateNodes())      // the B-tree pages survived the cut
    {
        nodes++;
        bytesRead += file.GetNode(info.NodeId).ReadAllBytes().Length;   // the payloads did not
    }

    Console.WriteLine($"{nodes} nodes, {bytesRead} bytes");
}
catch (PstFileFormatException ex)
{
    Console.WriteLine($"failed while reading: {ex.Error} — {ex.Message}");
}

// opened: Unicode
// failed while reading: InvalidBlock — The block at offset 153152 is malformed or failed its trailer validation.
```

A read that escapes the file surfaces as `InvalidBlock` (or `InvalidPage` for a page), which is why a salvage tool can enumerate what survived and stop cleanly at the cut. Corruption never escapes as any other exception type.

Damage that leaves the geometry intact is where the levels diverge. Flip one byte inside a block's payload and `Compatible` reads straight through it — the bytes are wrong, but nothing structural disagrees — while `Strict` catches the CRC:

```csharp
using Bodu.IO.Pst;

byte[] bytes = File.ReadAllBytes("archive.pst");
bytes[0x4D10] ^= 0xFF;   // one byte inside a data block's payload

foreach (PstValidationLevel level in new[] { PstValidationLevel.Compatible, PstValidationLevel.Strict })
{
    try
    {
        using PstFile file = PstFile.Open(new MemoryStream(bytes), new PstFileOptions { ValidationLevel = level });
        int nodes = file.EnumerateNodes().Count();
        long payload = 0;
        foreach (PstNodeInfo info in file.EnumerateNodes())
            payload += file.GetNode(info.NodeId).ReadAllBytes().Length;
        Console.WriteLine($"{level}: read {nodes} nodes, {payload} payload bytes");
    }
    catch (PstFileFormatException ex)
    {
        Console.WriteLine($"{level}: {ex.Error} — {ex.Message}");
    }
}

// Compatible: read 52 nodes, 16650 payload bytes
// Strict: InvalidBlock — The block at offset 19648 is malformed or failed its trailer validation.
```

## The `PstFileError` catalogue

Every failure surfaces through <xref:Bodu.IO.Pst.PstFileException> or a subclass, and every one carries a <xref:Bodu.IO.Pst.PstFileError> in `Error` so you can branch without parsing the message.

| `PstFileError` | Thrown as | Condition |
|---|---|---|
| `None` | <xref:Bodu.IO.Pst.PstFileException> | No category was recorded (the base constructors without a category). |
| `InvalidHeader` | <xref:Bodu.IO.Pst.PstFileFormatException> | The stream does not begin with the `!BDN` magic; the version word is not a known PST version (and is below the OST range); the header is shorter than the format's header size; the header CRC does not verify (`Compatible` / `Strict`); the sentinel byte is not `0x80`; or, under `Strict`, the declared file length lies beyond the end of the stream. |
| `UnsupportedFormat` | <xref:Bodu.IO.Pst.PstUnsupportedFormatException> | The version word is 36 or above (the 4 KiB-page OST variant), or the content-encoding byte names Windows Information Protection encryption (`0x10`) rather than none / permute / cyclic. |
| `InvalidPage` | <xref:Bodu.IO.Pst.PstFileFormatException> | A B-tree page's trailer type does not match what was expected; a page's entry stride, entry count, depth, or level is out of range; a node identifier in a page exceeds 32 bits; or, under `Strict`, the page's CRC, signature, or recorded block identifier disagrees. |
| `InvalidBlock` | <xref:Bodu.IO.Pst.PstFileFormatException> | A block's declared length is zero or its on-disk size exceeds the 8 KiB maximum; the trailer's length field disagrees; a read at the block's offset escapes the end of the stream; or, under `Strict`, the block's CRC, signature, or recorded block identifier disagrees. |
| `InvalidDataTree` | <xref:Bodu.IO.Pst.PstFileFormatException> | An `XBLOCK` / `XXBLOCK` header has the wrong type or level, its entry count overruns the block, a child is not at the level the parent implies, or a referenced block is missing from the block B-tree. |
| `InvalidSubnodeTree` | <xref:Bodu.IO.Pst.PstFileFormatException> | An `SLBLOCK` / `SIBLOCK` is malformed, or a value reference names a subnode the owning node's subnode tree does not contain. |
| `InvalidHeap` | <xref:Bodu.IO.Pst.PstFileFormatException> | A heap-on-node header or page map is malformed, a heap identifier does not resolve to an item, or a BTree-on-heap is malformed (including, under `Strict`, keys out of order). |
| `InvalidPropertyContext` | <xref:Bodu.IO.Pst.PstFileFormatException> | The node's heap does not carry a property context, a record is malformed, or a fixed-width value's payload is shorter than its declared width. |
| `InvalidTableContext` | <xref:Bodu.IO.Pst.PstFileFormatException> | The node's heap does not carry a table context, the `TCINFO` header is malformed, the row matrix does not resolve or holds fewer rows than the row index records, the row index names a row the matrix does not hold, or, under `Strict`, the matrix holds more rows than the index records. |
| `InvalidPropertyValue` | <xref:Bodu.IO.Pst.PstFileFormatException> | Under `Strict`, a property-context record carries a wire type the reader does not know (the tolerant levels keep it as a raw dword). |
| `NodeNotFound` | <xref:Bodu.IO.Pst.PstNodeNotFoundException> | `PstFile.GetNode` was asked for an identifier the node B-tree does not contain. Use `TryGetNode` to avoid the exception. |
| `PropertyNotFound` | <xref:Bodu.IO.Pst.PstFileException> | `PstPropertyContext.GetValue` was asked for an identifier the context does not contain. Use `TryGetValue` to avoid the exception. |
| `LimitExceeded` | <xref:Bodu.IO.Pst.PstFileFormatException> | A data tree's declared total exceeds `MaxNodeDataLength` on a materializing read, or its leaf count exceeds `MaxDataTreeLeaves` while the tree is walked. |

The three subclasses partition the space: <xref:Bodu.IO.Pst.PstFileFormatException> for anything structurally wrong, <xref:Bodu.IO.Pst.PstUnsupportedFormatException> for a file that is well-formed but a variant the library does not read, and <xref:Bodu.IO.Pst.PstNodeNotFoundException> for a lookup miss on a well-formed file. `PropertyNotFound` is the one category raised on the base type.

> [!NOTE]
> The typed accessors on <xref:Bodu.IO.Pst.PstPropertyValue> are the one place the library throws something other than the family: a wire-type or width mismatch is a *caller* error and surfaces as <xref:System.InvalidOperationException>. An unreadable or non-seekable stream at open is <xref:System.ArgumentException>, and anything after `Dispose` is <xref:System.ObjectDisposedException>.

## Where to go next

- [Reading nodes and tables](reading-nodes-and-tables.md) — the open → look up → read recipe and the typed accessors.
- [Reader options and resource limits](../outlook/reader-options-and-limits.md) — how the mail-store reader forwards these options, and its own limits on top.
- [Reading .pst mail stores](../outlook/reading-pst-mail-stores.md) — the two exception families as the mail-store reader surfaces them.
- [Bodu.IO.Pst core concepts](../../docs/io-pst/concepts.md) — header, trailers, and the decoded-block cache in the vocabulary page.
- [Runnable PST sample](../../samples/io-pst.md) — the StreamingAndValidation scenario over the same fixtures.
- [Bodu.IO.Pst guides](index.md) — every guide in this topic.
