---
title: Bodu.IO.Biff — Core concepts
---

# Bodu.IO.Biff — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md), and refer back whenever a term feels imprecise.

`Bodu.IO.Biff` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — a record-codec tier between the `Bodu.IO.Compound` container and the `Bodu.Formats.Excel.Binary` format reader. For the high-level shape of the library, start with the [introduction](index.md).

## BIFF

The **Binary Interchange File Format** is the record format every pre-2007 Excel workbook is stored in. From Excel 5.0 onward the record stream lives inside an OLE2 compound file — in a stream named `Book` (BIFF5, Excel 5.0/95) or `Workbook` (BIFF8, Excel 97–2003). This package reads and writes that record stream; the container around it is the business of `Bodu.IO.Compound`, and what the records *mean* as a spreadsheet is the business of `Bodu.Formats.Excel.Binary`. Excel 2007's `.xlsx` is a different format entirely (zipped XML) and is out of scope.

## Record

Every record is a **header** — a 16-bit little-endian identifier followed by the 16-bit length of the payload — and the **payload** itself. <xref:Bodu.IO.Biff.BiffRecordHeader> is the header; <xref:Bodu.IO.Biff.BiffRecordType> names the identifiers the codec knows (`BOF` `0x0809`, `EOF` `0x000A`, `NUMBER` `0x0203`, `SST` `0x00FC`, …). The catalogue is curated rather than exhaustive: a record with an unlisted identifier is framed exactly like any other and exposed through <xref:Bodu.IO.Biff.BiffReader.RecordId> and <xref:Bodu.IO.Biff.BiffReader.ValueSpan>, so an application can process records the codec does not interpret, and a stream can be copied record for record without loss.

A payload may declare up to 65,535 bytes, but the format caps a conformant record at **2,080 bytes (BIFF5)** or **8,224 bytes (BIFF8)** — <xref:Bodu.IO.Biff.BiffLimits>. The reader tolerates any declared length that fits its buffer; the writer enforces the version's cap.

## Version

<xref:Bodu.IO.Biff.BiffVersion> is `Biff5` or `Biff8` (or `Unknown` before either is known). The reader establishes it from the first `BOF` record's version field — `0x0500` or `0x0600` — and keeps it for the rest of the stream; a later `BOF` that disagrees is rejected as malformed. A caller that already knows the version (a sheet substream read after the workbook globals) can supply it through <xref:Bodu.IO.Biff.BiffReaderOptions> so no `BOF` need be seen first.

The version decides three things the codec handles internally: how strings are represented (below), the width of the row fields in `DIMENSIONS` (16-bit in BIFF5, 32-bit in BIFF8), and the maximum record length. Records that exist only in BIFF8 — the shared string table and `LABELSST` — are rejected by the writer under BIFF5.

BIFF2, BIFF3, and BIFF4 used different beginning-of-file identifiers (`0x0009`, `0x0209`, `0x0409`) and stored the stream without a container. The reader recognizes them so the failure is reported as <xref:Bodu.IO.Biff.BiffUnsupportedVersionException> — the data is not corrupt, it is older than the codec supports.

## Substream

The record stream is a sequence of **substreams**, each bracketed by a `BOF` and an `EOF`. The first is the **workbook globals** — the date system, code page, fonts, formats, extended formats, the sheet directory (`BOUNDSHEET` records), and in BIFF8 the shared string table. Each sheet that follows is its own substream, beginning at the absolute stream offset its `BOUNDSHEET` entry records. <xref:Bodu.IO.Biff.BiffSubstreamType> names the kinds a `BOF` can open. The reader does not track substreams; the writer counts them only so an `EOF` without an open `BOF` is caught.

## Continuation

When a logical structure exceeds one record, the format carries the overflow in one or more **`CONTINUE`** records that immediately follow. The reader is deliberately physical: `Read()` yields the `CONTINUE` record like any other, and <xref:Bodu.IO.Biff.BiffReader.IsContinuation> flags it. Two facilities handle the logical view:

- <xref:Bodu.IO.Biff.BiffReader.TryReadContinuation*> consumes the next record *only if* it is a `CONTINUE`, for structures whose continuation is a plain byte split (drawing and text objects, for example).
- <xref:Bodu.IO.Biff.BiffSstReader> handles the one structure with its own continuation rules — the shared string table — where a string's header never straddles a boundary, its characters may, and every continued run restarts with a flags byte selecting 8-bit or 16-bit characters.

The writer mirrors both: `WriteContinuedRecord` splits at the maximum length, and `WriteSst` splits at string and character boundaries with the flags byte the reader expects.

## Strings and code pages

BIFF8 stores text as a **Unicode string**: a length prefix (8-bit in `BOUNDSHEET` and `FONT`, 16-bit elsewhere), a flags byte, and the characters as UTF-16LE code units or — when every character fits a byte — one low byte per character ("compressed"). Optional trailers carry rich-text formatting runs and phonetic (extended) data. BIFF5 stores text as a **byte string**: a length prefix and bytes in the code page the stream's `CODEPAGE` record declares (Windows-1252 when absent).

<xref:Bodu.IO.Biff.BiffString> is the one view over both: <xref:Bodu.IO.Biff.BiffString.IsUnicode> and <xref:Bodu.IO.Biff.BiffString.IsHighByte> tell you the form, <xref:Bodu.IO.Biff.BiffString.RawCharacters> exposes the bytes, and `GetString()` / `CopyTo` decode — for a byte string with the code page the reader tracked, which is why the accessor takes no argument. The `CODEPAGE` value is normalized on the way in: the format's private markers for Apple Roman (`0x8000`) and the pre-BIFF5 ANSI page (`0x8001`) become Windows code page numbers.

## Shared string table

BIFF8 pools cell text in the workbook globals: the `SST` record declares a total reference count and a unique string count, then the strings; each `LABELSST` cell carries an index into it. The table routinely exceeds one record, so it is the canonical continued structure. <xref:Bodu.IO.Biff.BiffSstReader> reads it one string at a time: a string that lies within one record is exposed as a span-backed `Current` view; a string whose characters straddle a `CONTINUE` boundary is stitched into a scratch buffer and served through `GetString()` — the only allocation forward traversal ever makes.

## Cell records

A cell record begins with the zero-based row, the zero-based column, and the index of the cell's extended format (`XF`), followed by its value: `NUMBER` (a double), `RK` (a packed number), `MULRK` / `MULBLANK` (a run of adjacent cells in one row), `BOOLERR` (a boolean or an error code), `LABEL` (inline text), `LABELSST` (a shared-string index, BIFF8), `RSTRING` (rich text, BIFF5), and `FORMULA` (the cached result plus the parsed expression, which the codec exposes as raw token bytes). A formula whose cached result is text is followed by a `STRING` record carrying it. Each has a typed accessor on the reader and a typed writer; the [Record reference](records.md) tabulates every record's identifier, per-version layout, decoded type, accessor, and writer.

## RK numbers

An **RK** value packs a number into 32 bits: the low two bits select between a signed 30-bit integer and the high 30 bits of an IEEE 754 double, optionally divided by 100. Not every double has an RK form — <xref:Bodu.IO.Biff.BiffRk.TryEncode*> reports when a value must be written as a `NUMBER` record instead, and <xref:Bodu.IO.Biff.BiffRk.Decode*> reverses it.

## Reader state and resumption

<xref:Bodu.IO.Biff.BiffReader> is a `ref struct` over a span and cannot be stored in a field. Its resumable state — the established version and code page — is <xref:Bodu.IO.Biff.BiffReaderState>: capture <xref:Bodu.IO.Biff.BiffReader.CurrentState> after a pass, slice the buffer at <xref:Bodu.IO.Biff.BiffReader.BytesConsumed>, and construct the next reader over the remainder. With `isFinalBlock: false`, an incomplete trailing record makes `Read()` return `false` without consuming it, so a caller feeding a stream chunk by chunk never splits a record. This is the pattern `Utf8TomlReader` uses, and the pattern the Excel reader's worksheet reader follows.

## Errors

- <xref:Bodu.IO.Biff.BiffFormatException> — the bytes are not a valid BIFF stream, or a known record does not match its layout. It derives from `FormatException` and carries the offset of the record where possible.
- <xref:Bodu.IO.Biff.BiffUnsupportedVersionException> — a well-formed stream in a version the codec does not process. It derives from `NotSupportedException` and carries the raw version marker.
- `InvalidOperationException` — a typed accessor called on the wrong record, or a writer asked for a record its version lacks.
- An unknown record identifier is never an error.

## Where to go next

- **[Record reference](records.md)** — every named record and what the version changes.
- **[Getting started](getting-started.md)** — install and minimal samples.
- **[Introduction](index.md)** — the package at a glance.
- **[Binary Formats & I/O concepts](../topics/binary-formats-concepts.md)** — the container-versus-format vocabulary shared across the topic.
