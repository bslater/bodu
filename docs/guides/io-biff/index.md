---
title: Bodu.IO.Biff guides
---

# Bodu.IO.Biff guides

Recipe-style walk-throughs for **Bodu.IO.Biff**, the low-level codec for the BIFF5 and BIFF8 record streams inside legacy `.xls` workbooks. The package frames records, establishes the version and code page, decodes the common records through typed accessors, walks the shared string table across its `CONTINUE` records, and writes records back, in either version.

The library has no workbook model: it knows how BIFF is encoded, not what a workbook means. The reader in [Bodu.Formats.Excel.Binary](../excel/index.md) is built on it - the same codec/format split as `Bodu.IO.Pst` beneath `Bodu.Formats.Outlook.Pst` - and the container around the record stream is [Bodu.IO.Compound](../io-compound/index.md).

If you are new to the library, start with the [introduction](../../docs/io-biff/index.md), the [Core concepts](../../docs/io-biff/concepts.md) glossary, and the [getting-started page](../../docs/io-biff/getting-started.md), which cover reading a whole stream, decoding records, the shared string table, and writing single records. The guides below take each direction further.

## How the library works

A workbook stream is a flat sequence of records - a two-byte identifier, a two-byte length, and a payload - grouped into substreams, each opened by a `BOF` record and closed by an `EOF` record: the workbook globals first, then one per sheet. <xref:Bodu.IO.Biff.BiffReader> is a `ref struct` that walks the sequence forward over a span you supply, one physical record at a time, allocating nothing; the only state it keeps is the version and the code page, which it can hand to a reader over the next block. <xref:Bodu.IO.Biff.BiffWriter> emits records to an `IBufferWriter<byte>` under one version, and leaves their order, and the offsets the globals record, to you.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.IO.Biff> | `BiffReader` with `BiffReaderOptions` and `BiffReaderState`; `BiffSstReader`; `BiffWriter` with `BiffWriterOptions`; the decoded `Biff*Record` types; `BiffString`, `BiffRk`, `BiffRecordHeader`, `BiffLimits`; the vocabulary enumerations; and the `BiffFormatException` / `BiffUnsupportedVersionException` pair. | [Authoring a workbook stream](authoring-records.md) · [Reading a stream a block at a time](resumable-reading.md) |

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="authoring-records.md">Authoring a workbook stream</a></h3>
  <p>Assemble a complete BIFF8 workbook stream - globals, sheet substreams, <code>BOUNDSHEET</code> offsets, and a shared string table split into <code>CONTINUE</code> records - store it in a compound file, read it back, and see what changes under BIFF5.</p>
</div>

<div class="bodu-card">
  <h3><a href="resumable-reading.md">Reading a stream a block at a time</a></h3>
  <p>Read any stream, seekable or not, one buffer at a time with <code>BiffReaderState</code> and <code>BytesConsumed</code>; keep the shared string table whole across blocks; and read one sheet with the state the globals established.</p>
</div>

<div class="bodu-card">
  <h3><a href="../excel/reading-workbooks.md">Reading workbooks</a></h3>
  <p>The spreadsheet reader built on this package: sheets, cells by position, number formats, and dates, from a whole <code>.xls</code> file.</p>
</div>

</div>

## Suggested reading path

1. **[Getting started](../../docs/io-biff/getting-started.md)** - reading, decoding, and writing single records.
2. **[Authoring a workbook stream](authoring-records.md)** - when you produce `.xls` data rather than only read it.
3. **[Reading a stream a block at a time](resumable-reading.md)** - when the stream is large, arrives in pieces, or cannot seek.

## Where to go next

- [Runnable sample](../../samples/io-biff.md) - the `BiffBasics` console project under `samples/IO.Biff/`.
- [Bodu.IO.Biff API reference](xref:Bodu.IO.Biff) - every type and member.
- [Record reference](../../docs/io-biff/records.md) - every record the codec decodes and writes, by version.
- [Package matrix](../../docs/package-matrix.md) - where Bodu.IO.Biff sits in the suite and its dependency stack.
