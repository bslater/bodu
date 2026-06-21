---
title: Binary Formats & I/O — Concepts
---

# Binary Formats & I/O — Concepts

The shared vocabulary for the [Binary Formats & I/O topic](binary-formats.md) — the terms that cross the boundary between [`Bodu.IO.Compound`](../io-compound/index.md) and [`Bodu.Formats.Excel.Binary`](xref:Bodu.Formats.Excel.Binary). Each member library has its own deeper concepts page (linked in the closing list); this page covers only what you need to navigate both at once.

## Container vs format

A **container** describes *how named blobs are packed into one file* without knowing what those blobs mean — directory, allocation tables, sectors. A **format** describes *what the bytes of one blob mean* — records, fields, value types. The topic separates the two: `Bodu.IO.Compound` is the container reader; `Bodu.Formats.Excel.Binary` is a format reader that consumes one named blob (the `Workbook` stream) from inside the container.

## Compound file, storage, stream

A **compound file** (OLE2 / CFB) is a single file that embeds a small hierarchical file system. A **storage** is a named container of children (a folder, the COM `IStorage`); a **stream** is a named byte payload (a file, the COM `IStream`). Navigation starts at the **root storage** and is scoped to each storage's direct children, matched with ordinal (case-sensitive) names. See the [Bodu.IO.Compound concepts](../io-compound/concepts.md) for the full container model — sectors, the FAT, the mini-stream, and property sets.

## Sector chain

Within the container, a stream's bytes are stored as a linked chain of fixed-size **sectors** rather than contiguously. Reading a stream means following its chain. This is why the container reader can operate in a **streaming** mode that holds only one sector in memory at a time, instead of materializing the whole payload.

## BIFF record

The Excel 97–2003 binary format (**BIFF8**) stores a worksheet as a flat sequence of **records** inside the container's `Workbook` stream. Each record has a two-byte type, a two-byte length, and a typed body — a cell value, a shared-string-table entry, a sheet boundary. `Bodu.Formats.Excel.Binary` reads this record stream and surfaces the cell values; it does not evaluate formulas or apply styling. The container reader supplies the `Workbook` stream's bytes; the format reader interprets them.

## Read-only

Every reader in this topic is **read-only**. The container reader opens a file in <xref:Bodu.IO.Compound.CompoundFileMode.Read> only — there is no create, commit, or revert — and the Excel reader surfaces values without mutating anything. This keeps the surface small and the threading story simple: a buffered container is safe to share across threads.

## Where to go next

- **[Binary Formats & I/O overview](binary-formats.md)** — the topic landing page and package selection.
- **[Bodu.IO.Compound concepts](../io-compound/concepts.md)** — the full container vocabulary.
- **[Bodu.IO.Compound introduction](../io-compound/index.md)** — the container reader's headline types.
