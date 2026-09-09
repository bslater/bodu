---
title: Binary Formats & I/O — Concepts
---

# Binary Formats & I/O — Concepts

The shared vocabulary for the [Binary Formats & I/O topic](binary-formats.md) — the terms that cross the boundaries between [`Bodu.IO.Compound`](../io-compound/index.md), [`Bodu.IO.Biff`](../io-biff/index.md), and [`Bodu.Formats.Excel.Binary`](../excel/index.md). Each member library has its own deeper concepts page (linked in the closing list); this page covers only what you need to navigate both at once.

## Container vs format

A **container** describes *how named blobs are packed into one file* without knowing what those blobs mean — directory, allocation tables, sectors. A **format** describes *what the bytes of one blob mean* — records, fields, value types. The topic separates the two, and splits the format side once more into a **record codec** and a **format reader**: `Bodu.IO.Compound` and `Bodu.IO.Pst` are the container readers; `Bodu.IO.Biff` is the record codec that frames and decodes the BIFF5 and BIFF8 record stream; `Bodu.Formats.Excel.Binary` is the format reader that consumes one named blob (the `Workbook` stream) from inside the container and interprets its records — through `Bodu.IO.Biff` — as worksheets and cells.

## Compound file, storage, stream

A **compound file** (OLE2 / CFB) is a single file that embeds a small hierarchical file system. A **storage** is a named container of children (a folder, the COM `IStorage`); a **stream** is a named byte payload (a file, the COM `IStream`). Navigation starts at the **root storage** and is scoped to each storage's direct children, matched with ordinal (case-sensitive) names. See the [Bodu.IO.Compound concepts](../io-compound/concepts.md) for the full container model — sectors, the FAT, the mini-stream, and property sets.

## Sector chain

Within the container, a stream's bytes are stored as a linked chain of fixed-size **sectors** rather than contiguously. Reading a stream means following its chain. This is why the container reader can operate in a **streaming** mode that holds only one sector in memory at a time, instead of materializing the whole payload.

## BIFF record

The Excel binary formats — **BIFF8** (Excel 97–2003) and its predecessor **BIFF5** (Excel 5.0/95) — store a worksheet as a flat sequence of **records** inside the container's `Workbook` stream. Each record has a two-byte type, a two-byte length, and a typed body — a cell value, a shared-string-table entry, a sheet boundary. `Bodu.IO.Biff` frames and decodes this record stream (the `BiffReader` establishes the version from the BOF record and the code page from CODEPAGE, and exposes typed accessors for each record); `Bodu.Formats.Excel.Binary` walks those records and surfaces the cell values; it does not evaluate formulas or apply styling. The container reader supplies the `Workbook` stream's bytes, the record codec decodes them, and the format reader interprets them.

## Read-only vs. authoring

The *format* layer is strictly **read-only**: `Bodu.Formats.Excel.Binary` surfaces cell values without mutating anything, which keeps its surface small and the threading story simple — a buffered workbook is safe to share across threads. The *record codec* beneath it is symmetric — `Bodu.IO.Biff` pairs `BiffReader` with a `BiffWriter` that emits BIFF5 or BIFF8 records — but it authors record streams, not workbooks. The *container* layer is asymmetric. Opened with `FileAccess.Read` (`CompoundFile.OpenRead`), `Bodu.IO.Compound` is read-only too; but it additionally **authors** new containers — `CompoundFile.Create`, the builder API, and the OLE property-set writers — and can open an existing file `FileAccess.ReadWrite`. So when a workflow needs to *produce* a compound file rather than just read one, that capability lives in the container layer alone; nothing in the Excel reader writes.

## Where to go next

- **[Binary Formats & I/O overview](binary-formats.md)** — the topic landing page and package selection.
- **[Bodu.IO.Compound concepts](../io-compound/concepts.md)** — the full container vocabulary.
- **[Bodu.IO.Biff concepts](../io-biff/concepts.md)** — record framing, version and code-page establishment, and the typed record accessors.
- **[Bodu.Formats.Excel.Binary concepts](../excel/concepts.md)** — the BIFF record, workbook globals, and cell-kind vocabulary.
- **[Bodu.IO.Compound introduction](../io-compound/index.md)** — the container reader's headline types.
