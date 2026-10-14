---
title: Binary Formats & I/O - Guides
---

# Binary Formats & I/O - Guides

Recipe-style walk-throughs for the **Binary Formats & I/O** topic - readers (and, for `Bodu.IO.Compound` and `Bodu.IO.Biff`, writers) for legacy binary container and document formats. The packages form a strictly layered stack: general-purpose container readers at the bottom (`Bodu.IO.Compound` and `Bodu.IO.Pst`), a record codec in the middle (`Bodu.IO.Biff`), and narrower format readers built on top.

`Bodu.IO.Compound` reads the OLE2 / Compound File Binary (CFB) envelope - the structured-storage "file system in a file" used by legacy Microsoft Office documents - and exposes the embedded named streams with no application-format knowledge. `Bodu.IO.Biff` is a low-level codec for the Excel Binary Interchange File Format (BIFF5 and BIFF8) record streams found inside legacy `.xls` workbooks - the substrate beneath `Bodu.Formats.Excel.Binary`, in the same relation `Bodu.IO.Pst` has to `Bodu.Formats.Outlook.Pst`. `Bodu.Formats.Excel.Binary` builds on both to surface raw worksheet cell values from BIFF5 and BIFF8 `.xls` workbooks, and `Bodu.Formats.Outlook.Msg` builds on the container to open `.msg` messages. `Bodu.IO.Pst` reads the second container in the topic - the Outlook personal-folders (PST) node database - and `Bodu.Formats.Outlook.Pst` builds on it to open a `.pst` mail store in the same `Bodu.Formats.Outlook` MAPI value model.

## Bodu.IO.Compound guides

The CFB container reader and writer - the storage hierarchy, the stream cursor, editing and authoring, and the OLE property sets.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../io-compound/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.IO.Compound</code> - namespace map, the storage-hierarchy mental model, and which guide covers each concern.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-compound/reading-compound-files.md">Reading compound files</a></h3>
  <p>Open a file, probe the signature, walk the storage hierarchy with the enumerate and <code>TryOpen</code> surfaces, and read a named stream's bytes.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-compound/streaming-and-buffering.md">Buffered vs streaming access</a></h3>
  <p>The <code>buffered</code> flag, the <code>CompoundStream</code> cursor, <code>AsMemory</code> vs chunked <code>Read</code>, and bounding memory for large files.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-compound/property-sets.md">Reading property sets</a></h3>
  <p>The <code>SummaryInformation</code> and <code>DocumentSummaryInformation</code> metadata streams, the raw <code>OlePropertySet</code>, and the <code>TryGet*</code> convenience methods.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-compound/office-format-nuances.md">Office format nuances</a></h3>
  <p>How the legacy Office documents (<code>.xls</code>, <code>.doc</code>, <code>.ppt</code>, <code>.msg</code>) lay out their named streams inside the CFB envelope, and the quirks to expect when reading them.</p>
</div>

</div>

## Bodu.IO.Biff guides

The BIFF5 and BIFF8 record codec beneath the `.xls` reader - writing a workbook stream record by record, and reading one in blocks.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../io-biff/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.IO.Biff</code> - how the reader and writer work, the namespace map, and which guide covers each concern.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-biff/authoring-records.md">Authoring a workbook stream</a></h3>
  <p>Assemble the globals and sheet substreams, patch the <code>BOUNDSHEET</code> offsets, write a shared string table across <code>CONTINUE</code> records, and store the result in a compound file.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-biff/resumable-reading.md">Reading a stream a block at a time</a></h3>
  <p>Carry <code>BiffReaderState</code> and <code>BytesConsumed</code> from block to block over any stream, keeping the shared string table whole.</p>
</div>

</div>

## Bodu.Formats.Excel.Binary guides

The BIFF5 and BIFF8 `.xls` reader built on `Bodu.IO.Compound` and `Bodu.IO.Biff` surfaces raw worksheet cell values - strings, numbers, booleans, and errors - without formula evaluation, styling, or higher-level interpretation.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../excel/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.Formats.Excel.Binary</code> - the layered BIFF-on-compound-file model, the namespace map, and which guide covers each concern.</p>
</div>

<div class="bodu-card">
  <h3><a href="../excel/reading-workbooks.md">Reading workbooks</a></h3>
  <p>Open an <code>.xls</code> from a path or stream, list the sheets and used ranges, control stream ownership and optional metadata work, and read document properties.</p>
</div>

<div class="bodu-card">
  <h3><a href="../excel/cell-values-and-dates.md">Cell values and dates</a></h3>
  <p>The <code>ExcelCell</code> kinds and value projections, a formula cell's cached result, date-format detection, serial-date conversion, and A1 references.</p>
</div>

<div class="bodu-card">
  <h3><a href="../excel/worksheets-and-rows.md">Streaming vs materialized</a></h3>
  <p>The forward-only <code>ExcelWorksheetReader</code> versus the randomly addressable <code>ExcelWorksheet</code> - when to reach for each, and how to bound allocation.</p>
</div>

</div>

## Bodu.IO.Pst guides

The read-only container layer for Outlook personal-folders files (Unicode and ANSI): the node database, the property-context and table-context views with wire-typed values, and the validation levels. No MAPI semantics; the `.pst` mail-store reader below builds on it.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../io-pst/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.IO.Pst</code> - how a PST file is a B-tree-indexed database of nodes, the namespace map, and which guide covers each concern.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-pst/reading-nodes-and-tables.md">Reading nodes and tables</a></h3>
  <p>Open a file, walk the node B-tree, and read a node's property context and table context.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-pst/streaming-and-validation.md">Streaming and validation</a></h3>
  <p>Stream large payloads, the decoded-block cache, and the strict, compatible, and minimal validation levels.</p>
</div>

</div>

## Bodu.Formats.Outlook guides

The Outlook format readers - the shared MAPI value model in `Bodu.Formats.Outlook`, the read-only `.msg` reader `Bodu.Formats.Outlook.Msg` built on `Bodu.IO.Compound`, and the read-only `.pst` mail-store reader `Bodu.Formats.Outlook.Pst` built on `Bodu.IO.Pst`.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../outlook/index.md">Overview</a></h3>
  <p>The full guide index for the Outlook readers - how a <code>.msg</code> file sits inside the CFB container and a <code>.pst</code> store inside the PST node database, the namespace map, and which guide covers each concern.</p>
</div>

<div class="bodu-card">
  <h3><a href="../outlook/reading-msg-files.md">Reading .msg files</a></h3>
  <p>Open a message from a path or stream, the scalar and body conveniences, recipients, attachments, and nested messages.</p>
</div>

<div class="bodu-card">
  <h3><a href="../outlook/reading-pst-mail-stores.md">Reading .pst mail stores</a></h3>
  <p>Open a mail store, walk the folder hierarchy, stream its messages, and read their recipients, attachments, and embedded messages.</p>
</div>

<div class="bodu-card">
  <h3><a href="../outlook/properties-and-named-properties.md">Properties and named properties</a></h3>
  <p>The raw property surface, typed accessors, tags and wire types, and resolving named properties.</p>
</div>

<div class="bodu-card">
  <h3><a href="../outlook/reader-options-and-limits.md">Reader options and resource limits</a></h3>
  <p>The options both readers take, the limits that bound a crafted file, and omitting unresolvable values instead of failing.</p>
</div>

</div>

## Start here

1. **[Topic overview](../../docs/topics/binary-formats.md)** - the layered container-vs-format split and package selection on the docs side.
2. **[Topic concepts](../../docs/topics/binary-formats-concepts.md)** - container, storage, stream, sector chain, BIFF record.
3. **[Reading compound files](../io-compound/reading-compound-files.md)** - the core open → navigate → read recipe that every other use builds on.
4. **[Buffered vs streaming access](../io-compound/streaming-and-buffering.md)** - once the file is too large to hold whole, or you need to control the source's lifetime.
5. **[Reading property sets](../io-compound/property-sets.md)** - when you want the authored document metadata rather than the format payload.

## Where to go next

- **[Binary Formats & I/O overview](../../docs/topics/binary-formats.md)** - the topic landing page on the docs side.
- **[Binary Formats & I/O concepts](../../docs/topics/binary-formats-concepts.md)** - the cross-package vocabulary.
- **Member introductions:** [Bodu.IO.Compound](../../docs/io-compound/index.md) · [Bodu.IO.Biff](../../docs/io-biff/index.md) · [Bodu.Formats.Excel.Binary](../../docs/excel/index.md) · [Bodu.IO.Pst](../../docs/io-pst/index.md) · [Bodu.Formats.Outlook](../../docs/outlook/index.md).
- **Complete guide indexes** (this page shows the highlights; each index lists every guide for its package): [Bodu.IO.Compound](../io-compound/index.md) · [Bodu.Formats.Excel.Binary](../excel/index.md) · [Bodu.IO.Pst](../io-pst/index.md) · [Bodu.Formats.Outlook](../outlook/index.md).
- **API reference:** [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.IO.Biff](xref:Bodu.IO.Biff) · [Bodu.Formats.Excel](xref:Bodu.Formats.Excel) · [Bodu.IO.Pst](xref:Bodu.IO.Pst) · [Bodu.Formats.Outlook](xref:Bodu.Formats.Outlook).
