---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for `Bodu.IO.Biff`, the BIFF
record codec beneath the Excel reader, under
[`samples/IO.Biff/`](https://github.com/bslater/bodu/tree/master/samples/IO.Biff).
It is **offline and deterministic**: it reads the committed `Data/sample-biff8.xls` (the BIFF8
fixture the Excel reader's tests use), extracting the `Workbook` stream with `Bodu.IO.Compound`
since the codec itself has no container dependency, and it authors its own BIFF8 stream with
`BiffWriter` for the round-trip scenario. It is a member of `bodu.slnx`, built and executed by
CI. The README documents every scenario individually.

Run it from the repository root:

```bash
dotnet run --project samples/IO.Biff/Bodu.IO.Biff.Samples.BiffBasics
```

## The sample

### Bodu.IO.Biff.Samples.BiffBasics

The codec from the physical record view to authoring:

- **RecordCensus** — <xref:Bodu.IO.Biff.BiffReader> over a real workbook stream: every physical
  record framed, the `BOF` records marking each substream and establishing
  <xref:Bodu.IO.Biff.BiffVersion>, and a tally by identifier that includes records the codec does
  not name.
- **CellsAndSharedStrings** — the typed accessors: the sheet directory from `BOUNDSHEET`, the
  shared string table read one string at a time through <xref:Bodu.IO.Biff.BiffSstReader> across
  its `CONTINUE` records, and the first sheet's cell records decoded into values with the version
  seeded through <xref:Bodu.IO.Biff.BiffReaderOptions>.
- **WriteAndReadBack** — <xref:Bodu.IO.Biff.BiffWriter> assembling a BIFF8 workbook stream with
  fonts, formats, a shared string table, a bound sheet, and a sheet holding every cell kind, with
  `BytesCommitted` supplying the sheet offset, then read straight back.
- **MalformedInput** — the error contract: truncated headers and payloads and short known records
  as <xref:Bodu.IO.Biff.BiffFormatException>, a BIFF4 stream as
  <xref:Bodu.IO.Biff.BiffUnsupportedVersionException>, an unknown record as no error at all, and
  chunked reading with `isFinalBlock: false`.

## Where to go next

- **[Bodu.IO.Biff introduction](../docs/io-biff/index.md)** — the package at a glance.
- **[Getting started](../docs/io-biff/getting-started.md)** — install and minimal samples.
- **[Bodu.Formats.Excel.Binary samples](excel.md)** — the same fixture read as a workbook.
