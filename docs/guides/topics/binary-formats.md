---
title: Binary Formats & I/O — Guides
---

# Binary Formats & I/O — Guides

Recipe-style walk-throughs for the **Binary Formats & I/O** topic — the read-only readers for legacy binary container and document formats. The packages form a strictly layered stack: a general-purpose container reader at the bottom, with narrower format readers built on top.

`Bodu.IO.Compound` reads the OLE2 / Compound File Binary (CFB) envelope — the structured-storage "file system in a file" used by legacy Microsoft Office documents — and exposes the embedded named streams with no application-format knowledge. `Bodu.Formats.Excel.Binary` builds on it to surface raw worksheet cell values from BIFF8 `.xls` workbooks.

## Bodu.IO.Compound guides

The CFB container reader — the storage hierarchy, the stream cursor, and the OLE property sets.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../io-compound/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.IO.Compound</code> — namespace map, the storage-hierarchy mental model, and which guide covers each concern.</p>
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

</div>

## Bodu.Formats.Excel.Binary guides

The BIFF8 `.xls` reader built on `Bodu.IO.Compound` surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../excel/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.Formats.Excel.Binary</code> — the layered BIFF8-on-compound-file model, the namespace map, and which guide covers each concern.</p>
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
  <p>The forward-only <code>ExcelWorksheetReader</code> versus the randomly addressable <code>ExcelWorksheet</code> — when to reach for each, and how to bound allocation.</p>
</div>

</div>

## Start here

1. **[Topic overview](../../docs/topics/binary-formats.md)** — the layered container-vs-format split and package selection on the docs side.
2. **[Topic concepts](../../docs/topics/binary-formats-concepts.md)** — container, storage, stream, sector chain, BIFF record.
3. **[Reading compound files](../io-compound/reading-compound-files.md)** — the core open → navigate → read recipe that every other use builds on.
4. **[Buffered vs streaming access](../io-compound/streaming-and-buffering.md)** — once the file is too large to hold whole, or you need to control the source's lifetime.
5. **[Reading property sets](../io-compound/property-sets.md)** — when you want the authored document metadata rather than the format payload.

## Where to go next

- **[Binary Formats & I/O overview](../../docs/topics/binary-formats.md)** — the topic landing page on the docs side.
- **[Binary Formats & I/O concepts](../../docs/topics/binary-formats-concepts.md)** — the cross-package vocabulary.
- **Member introductions:** [Bodu.IO.Compound](../../docs/io-compound/index.md) · [Bodu.Formats.Excel.Binary](../../docs/excel/index.md).
- **Guide index:** [Bodu.IO.Compound](../io-compound/index.md) · [Bodu.Formats.Excel.Binary](../excel/index.md).
- **API reference:** [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.Formats.Excel](xref:Bodu.Formats.Excel).
