---
title: Binary Formats & I/O — Overview
---

# Binary Formats & I/O

The **Binary Formats & I/O** topic covers readers — and, for `Bodu.IO.Compound` and `Bodu.IO.Biff`, writers — for legacy binary container and document formats. The packages form a strictly layered stack: general-purpose container readers at the bottom (`Bodu.IO.Compound` and `Bodu.IO.Pst`), a record codec in the middle (`Bodu.IO.Biff`), and narrower format readers built on top, so each layer carries only the concepts it needs.

[`Bodu.IO.Compound`](../io-compound/index.md) reads the OLE2 / Compound File Binary (CFB) envelope — the structured-storage "file system in a file" used by legacy Microsoft Office documents — and exposes the embedded named streams with no application-format knowledge. [`Bodu.IO.Biff`](../io-biff/index.md) is a low-level codec for the Excel Binary Interchange File Format (BIFF5 and BIFF8) record streams found inside legacy `.xls` workbooks — the substrate beneath `Bodu.Formats.Excel.Binary`, in the same relation `Bodu.IO.Pst` has to `Bodu.Formats.Outlook.Pst`. [`Bodu.Formats.Excel.Binary`](../excel/index.md) builds on both to surface raw worksheet cell values from BIFF5 and BIFF8 `.xls` workbooks. `Bodu.Formats.Outlook.Msg` also builds on the container to open a `.msg` message. [`Bodu.IO.Pst`](../io-pst/index.md) reads the second container in the topic — the Outlook personal-folders (PST / MS-PST) node database — and `Bodu.Formats.Outlook.Pst` builds on it to open a `.pst` as a mail store: folders, messages, recipients, attachments, and bodies in the shared `Bodu.Formats.Outlook` MAPI value model that both Outlook readers use.

The dependency runs one way: `Bodu.Formats.Excel.Binary` references `Bodu.IO.Compound` to reach the `Workbook` stream inside an `.xls` file, then interprets the BIFF5 or BIFF8 record stream within it through `Bodu.IO.Biff`. The container reader has no knowledge of Excel, and the record codec has no knowledge of the container — and the Excel reader, in turn, is consumed unchanged outside this topic: the Reserve Bank of Australia exchange-rate provider in [`Bodu.Financial.ExchangeRates.Rba`](../../guides/topics/numerics-and-financial.md) parses the RBA's published `.xls` rate sheet through `ExcelBinaryWorkbook`, reading raw cells rather than re-implementing BIFF8. Each layer in this topic therefore earns its keep across more than one consumer — the layering is what makes that reuse cheap.

![A compound file is a structured-storage envelope: a header, allocation tables, and a directory of sectors on the left, resolving via CompoundFile.Open into the logical RootStorage to CompoundStorage to CompoundStream hierarchy on the right.](../../images/diagrams/io-compound-structure.svg)

## The packages

| Package | Status | What it provides | Docs |
|---|---|---|---|
| `Bodu.IO.Compound` | Stable | A CFB container reader and writer: the `CompoundFile` open/create entry points and a transactional `Commit` / `CommitAsync`, the builder API for authoring containers, the `CompoundStorage` / `CompoundStream` hierarchy, the seekable `CompoundStream` cursor with async streaming reads, and OLE property-set readers and writers. | [Intro](../io-compound/index.md) · [Concepts](../io-compound/concepts.md) · [Get started](../io-compound/getting-started.md) |
| `Bodu.IO.Biff` | Preview | A low-level codec for the BIFF5 and BIFF8 record streams inside `.xls` workbooks: the forward-only, allocation-free `BiffReader` (record framing, version from BOF, code page from CODEPAGE, typed accessors for cell, sheet, and workbook-globals records), `BiffSstReader` for the BIFF8 shared string table across CONTINUE records, and `BiffWriter` for emitting BIFF5 or BIFF8 records. No compound-file dependency, no workbook or cell model, no formula evaluation. | [Intro](../io-biff/index.md) · [Concepts](../io-biff/concepts.md) · [Get started](../io-biff/getting-started.md) |
| `Bodu.Formats.Excel.Binary` | Stable | A narrow, read-only BIFF5 and BIFF8 (`.xls`) reader over `Bodu.IO.Compound` and `Bodu.IO.Biff` that surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation. | [Intro](../excel/index.md) · [Concepts](../excel/concepts.md) · [Get started](../excel/getting-started.md) |
| `Bodu.Formats.Outlook.Msg` | Preview | A read-only `.msg` (MS-OXMSG) reader over `Bodu.IO.Compound`: the `OutlookMessage` session exposing every decoded MAPI property, the recipient and attachment tables, nested attached messages, named-property resolution, and the text/HTML/compressed-RTF bodies — sharing the `Bodu.Formats.Outlook` value model with the `.pst` reader. | [Guides](../../guides/outlook/index.md) |
| `Bodu.IO.Pst` | Preview | A read-only PST (MS-PST, Unicode and ANSI formats) container reader: the `PstFile` / `PstNode` session over the node database — B-trees, data and subnode trees, content encodings, checksums — and the LTP property-context and table-context views with wire-typed values; streaming payload access with a decoded-block LRU cache. No MAPI semantics, no writing. | [Intro](../io-pst/index.md) · [Concepts](../io-pst/concepts.md) · [Get started](../io-pst/getting-started.md) |
| `Bodu.Formats.Outlook.Pst` | Preview | A read-only `.pst` mail-store reader over `Bodu.IO.Pst`: the `OutlookMailStore` session exposing the folder hierarchy and every message with decoded MAPI properties, recipients, attachments (including embedded messages), named-property resolution, and the text/HTML/compressed-RTF bodies — sharing the `Bodu.Formats.Outlook` value model with the `.msg` reader. | [Get started](../io-pst/getting-started.md) |

## Why a layered reader

Legacy binary formats nest: an `.xls` file is a BIFF5 or BIFF8 record stream stored *inside* a CFB container's `Workbook` (or `Book`) stream. Modelling those responsibilities as separate packages keeps each one small and independently reusable:

- **`Bodu.IO.Compound`** answers "what named streams does this container hold, and what are their bytes?" — and nothing more. It works for any compound file, Office or not.
- **`Bodu.IO.Biff`** answers "what records does this BIFF stream contain, and what do their fields decode to?" — framing, version, code page, and typed accessors, with no knowledge of the container or of worksheets.
- **`Bodu.Formats.Excel.Binary`** answers "what are the cell values on this worksheet?" by walking the records `Bodu.IO.Biff` decodes out of the container's `Workbook` stream.

A consumer that only needs the container — to pull an embedded thumbnail, a property set, or a custom application stream — depends on `Bodu.IO.Compound` alone and never pulls in the Excel record vocabulary; a consumer that only needs the record codec — to inspect or author a BIFF stream — depends on `Bodu.IO.Biff` alone and never pulls in the container.

## Choosing a package

| Scenario | Reach for |
|---|---|
| Read named streams or storages from any `.xls` / `.doc` / `.ppt` / `.msg` file | <xref:Bodu.IO.Compound.CompoundFile> |
| Test whether a file is a compound file at all | `CompoundFile.IsCompoundFile(stream)` |
| Read authored document metadata (title, author, timestamps) | `CompoundFile.TryGetSummaryInformation(...)` |
| Read worksheet cell values from a BIFF5 or BIFF8 `.xls` workbook | <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> |
| Walk or author a BIFF record stream without a workbook model | <xref:Bodu.IO.Biff.BiffReader> · <xref:Bodu.IO.Biff.BiffWriter> |
| Read a `.msg` message's properties, recipients, attachments, and bodies | <xref:Bodu.Formats.Outlook.OutlookMessage> (`Bodu.Formats.Outlook.Msg`) |
| Bound memory while reading a large container | `CompoundFile.Open(stream, buffered: false)` |
| Read folders, messages, recipients, and attachments from a `.pst` archive | `OutlookMailStore` (`Bodu.Formats.Outlook.Pst`) |
| Read raw nodes, properties, and tables from a `.pst` without MAPI semantics | <xref:Bodu.IO.Pst.PstFile> |

## Scope

`Bodu.Formats.Excel.Binary` is **read-only** — it surfaces raw cell values without evaluating formulas, applying styles, or interpreting higher-level workbook structure. `Bodu.IO.Compound` reads existing containers and additionally **authors** new ones (`CompoundFile.Create`, the `Builders` API, and the OLE property-set writers). `Bodu.IO.Biff` reads record streams and additionally **writes** them (`BiffWriter`), but carries no workbook or cell model and no formula evaluation.

## Install

```bash
dotnet add package Bodu.IO.Compound
dotnet add package Bodu.IO.Biff
dotnet add package Bodu.Formats.Excel.Binary
dotnet add package Bodu.Formats.Outlook.Msg
dotnet add package Bodu.IO.Pst
dotnet add package Bodu.Formats.Outlook.Pst
```

`Bodu.Formats.Excel.Binary` depends on `Bodu.IO.Compound` and `Bodu.IO.Biff`, `Bodu.Formats.Outlook.Msg` depends on `Bodu.IO.Compound`, and `Bodu.Formats.Outlook.Pst` depends on `Bodu.IO.Pst`; both Outlook readers share `Bodu.Formats.Outlook`. `Bodu.IO.Compound` depends only on `Bodu.Core`, `Bodu.IO.Biff` on `Bodu.Core` and the `System.Text.Encoding.CodePages` NuGet package (for BIFF5 byte strings), and `Bodu.IO.Pst` on `Bodu.Core` and `Bodu.Collections` — install the topmost package your application consumes.

## Where to go next

- **[Binary Formats & I/O concepts](binary-formats-concepts.md)** — the shared vocabulary: container, storage, stream, sector chain, BIFF record.
- **[Bodu.IO.Compound introduction](../io-compound/index.md)** — the container reader in detail.
- **[Bodu.IO.Biff introduction](../io-biff/index.md)** — the BIFF5 / BIFF8 record codec beneath the Excel reader.
- **[Bodu.Formats.Excel.Binary introduction](../excel/index.md)** — the BIFF5 and BIFF8 `.xls` reader built on both.
- **[Bodu.IO.Pst introduction](../io-pst/index.md)** — the PST node-database reader and the mail-store reader built on it.
- **[Bodu.Formats.Outlook guides](../../guides/outlook/index.md)** — [reading `.msg` files](../../guides/outlook/reading-msg-files.md) and [properties and named properties](../../guides/outlook/properties-and-named-properties.md).
- **[Binary Formats & I/O guides](../../guides/topics/binary-formats.md)** — recipe-style walk-throughs across the topic.
- **API reference:** [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.IO.Biff](xref:Bodu.IO.Biff) · [Bodu.Formats.Excel](xref:Bodu.Formats.Excel) · [Bodu.IO.Pst](xref:Bodu.IO.Pst) · [Bodu.Formats.Outlook](xref:Bodu.Formats.Outlook).
