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

## Bodu.Formats.Excel.Binary

The BIFF8 `.xls` reader built on `Bodu.IO.Compound` surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation. It does not yet ship a dedicated guide; consult the [Bodu.Formats.Excel.Binary API reference](xref:Bodu.Formats.Excel.Binary) directly.

## Start here

1. **[Reading compound files](../io-compound/reading-compound-files.md)** — the core open → navigate → read recipe that every other use builds on.
2. **[Buffered vs streaming access](../io-compound/streaming-and-buffering.md)** — once the file is too large to hold whole, or you need to control the source's lifetime.
3. **[Reading property sets](../io-compound/property-sets.md)** — when you want the authored document metadata rather than the format payload.

## Where to go next

- **Guide index:** [Bodu.IO.Compound](../io-compound/index.md).
- **API reference:** [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.Formats.Excel.Binary](xref:Bodu.Formats.Excel.Binary).
- **[Package matrix](../../docs/package-matrix.md)** — where these packages sit in the suite and their dependency stack.
