---
title: Bodu.IO.Biff — Record reference
---

# Bodu.IO.Biff — Record reference

The catalogue of every record the codec decodes and writes, with its identifier, the differences between the BIFF5 and BIFF8 layouts, the decoded type, and the reader accessor and writer method that pair with it. Read [Core concepts](concepts.md) first for the vocabulary (*record*, *substream*, *CONTINUE*, *code page*, *RK*); this page is the lookup table you return to while coding against the API.

Every record starts with the four-byte <xref:Bodu.IO.Biff.BiffRecordHeader> — a 16-bit identifier and a 16-bit payload length. The tables below describe the **payload** only. A *cell prefix* is the six bytes that open every cell record: the zero-based row, the zero-based column, and the extended-format (`XF`) index, each 16 bits. A *string* is the version's text structure described under [Strings](#strings): a BIFF8 Unicode string or a BIFF5 code-page byte string, with an 8-bit or 16-bit length prefix as noted.

## Workbook globals

Records that appear in the first substream and describe the workbook as a whole.

| Record | Identifier | Payload | Decoded type | Reader | Writer |
|---|---|---|---|---|---|
| `BOF` | `0x0809` | Version marker, substream type, build, year — 8 bytes in BIFF5; BIFF8 adds file-history flags and the lowest saving version for 16. Any payload of at least 4 bytes is decoded, with zero for absent fields. | <xref:Bodu.IO.Biff.BiffBofRecord> | <xref:Bodu.IO.Biff.BiffReader.GetBof> | <xref:Bodu.IO.Biff.BiffWriter.WriteBof*> |
| `EOF` | `0x000A` | Empty. Closes the substream the last `BOF` opened. | — (<xref:Bodu.IO.Biff.BiffRecordType.Eof>) | `RecordType` | <xref:Bodu.IO.Biff.BiffWriter.WriteEof> |
| `CODEPAGE` | `0x0042` | One 16-bit value. Reading it updates <xref:Bodu.IO.Biff.BiffReader.CodePage>; the private markers `0x8000` (Apple Roman) and `0x8001` (legacy ANSI) are normalized to Windows code pages. | <xref:Bodu.IO.Biff.BiffCodePageRecord> | <xref:Bodu.IO.Biff.BiffReader.GetCodePage> | <xref:Bodu.IO.Biff.BiffWriter.WriteCodePage*> |
| `DATEMODE` | `0x0022` | One 16-bit value; non-zero selects the 1904 date system. | <xref:Bodu.IO.Biff.BiffDateModeRecord> | <xref:Bodu.IO.Biff.BiffReader.GetDateMode> | <xref:Bodu.IO.Biff.BiffWriter.WriteDateMode*> |
| `FILEPASS` | `0x002F` | BIFF5: the XOR key and hash (4 bytes). BIFF8: a type word (<xref:Bodu.IO.Biff.BiffEncryptionType>), then the XOR pair for `Xor` or an uninterpreted RC4 header. The codec reports the scheme; it does not decrypt. | <xref:Bodu.IO.Biff.BiffFilePassRecord> | <xref:Bodu.IO.Biff.BiffReader.GetFilePass> | `WriteRecord` (raw) |
| `FONT` | `0x0031` | 14 fixed bytes (height, attribute flags, color index, weight, escapement, underline, family, character set, reserved) then the face name as an 8-bit-length string. | <xref:Bodu.IO.Biff.BiffFontRecord> | <xref:Bodu.IO.Biff.BiffReader.GetFont> | <xref:Bodu.IO.Biff.BiffWriter.WriteFont*> |
| `FORMAT` | `0x041E` | The 16-bit format index, then the code — a 16-bit-length Unicode string in BIFF8, an 8-bit-length byte string in BIFF5. | <xref:Bodu.IO.Biff.BiffFormatRecord> | <xref:Bodu.IO.Biff.BiffReader.GetFormat> | <xref:Bodu.IO.Biff.BiffWriter.WriteFormat*> |
| `XF` | `0x00E0` | 16 bytes in BIFF5, 20 in BIFF8. The codec decodes the first six — font index, format index, and the type-and-protection word — and tolerates a record carrying only the two indices. | <xref:Bodu.IO.Biff.BiffXfRecord> | <xref:Bodu.IO.Biff.BiffReader.GetXf> | <xref:Bodu.IO.Biff.BiffWriter.WriteXf*> |
| `BOUNDSHEET` | `0x0085` | The 32-bit absolute stream offset of the sheet's `BOF`, a visibility byte (<xref:Bodu.IO.Biff.BiffSheetState>, low two bits), a sheet-type byte (<xref:Bodu.IO.Biff.BiffSheetType>), then the name as an 8-bit-length string. | <xref:Bodu.IO.Biff.BiffBoundSheetRecord> | <xref:Bodu.IO.Biff.BiffReader.GetBoundSheet> | <xref:Bodu.IO.Biff.BiffWriter.WriteBoundSheet*> |
| `SST` | `0x00FC` | **BIFF8 only.** The total reference count and unique string count (two 32-bit values), then the strings, which overflow into `CONTINUE` records by the rules under [Shared string table](#shared-string-table). | <xref:Bodu.IO.Biff.BiffSstHeader> (counts) and <xref:Bodu.IO.Biff.BiffSstReader> (strings) | <xref:Bodu.IO.Biff.BiffReader.GetSstHeader>, then `new BiffSstReader(ref reader)` | <xref:Bodu.IO.Biff.BiffWriter.WriteSst*> |
| `CONTINUE` | `0x003C` | The overflow of the record before it. Reported as a physical record with <xref:Bodu.IO.Biff.BiffReader.IsContinuation> set. | — | <xref:Bodu.IO.Biff.BiffReader.TryReadContinuation*> or <xref:Bodu.IO.Biff.BiffSstReader> | <xref:Bodu.IO.Biff.BiffWriter.WriteContinuedRecord*> or `WriteSst` |

## Sheet substream

Records that describe a sheet's extent and rows.

| Record | Identifier | Payload | Decoded type | Reader | Writer |
|---|---|---|---|---|---|
| `DIMENSIONS` | `0x0200` | The used range as first row, one-past-last row, first column, one-past-last column. Rows are 16-bit in BIFF5 (10 bytes with the reserved word) and 32-bit in BIFF8 (14 bytes); the version selects the layout. | <xref:Bodu.IO.Biff.BiffDimensionsRecord> | <xref:Bodu.IO.Biff.BiffReader.GetDimensions> | <xref:Bodu.IO.Biff.BiffWriter.WriteDimensions*> |
| `ROW` | `0x0208` | 16 bytes in both versions: row, first column, one-past-last column, the height field, two reserved words, the option flags, and the format field. Derived properties mask the height, outline level, hidden and custom-height flags, and the 12-bit XF index. | <xref:Bodu.IO.Biff.BiffRowRecord> | <xref:Bodu.IO.Biff.BiffReader.GetRow> | <xref:Bodu.IO.Biff.BiffWriter.WriteRow*> |

## Cell records

Every cell record opens with the six-byte cell prefix. The layouts are identical in BIFF5 and BIFF8 except where the value is text.

| Record | Identifier | Payload after the prefix | Decoded type | Reader | Writer |
|---|---|---|---|---|---|
| `NUMBER` | `0x0203` | A little-endian IEEE 754 double (14 bytes total). | <xref:Bodu.IO.Biff.BiffNumberRecord> | <xref:Bodu.IO.Biff.BiffReader.GetNumber> | <xref:Bodu.IO.Biff.BiffWriter.WriteNumber*> |
| `RK` | `0x027E` | A 32-bit RK value (10 bytes total), decoded by <xref:Bodu.IO.Biff.BiffRk>. | <xref:Bodu.IO.Biff.BiffRkRecord> | <xref:Bodu.IO.Biff.BiffReader.GetRk> | <xref:Bodu.IO.Biff.BiffWriter.WriteRk*> |
| `MULRK` | `0x00BD` | No prefix: the row and first column, then six bytes per cell (XF index and RK value), then the declared last column. Cells are exposed by index as <xref:Bodu.IO.Biff.BiffRkCell> values. | <xref:Bodu.IO.Biff.BiffMulRkRecord> | <xref:Bodu.IO.Biff.BiffReader.GetMulRk> | <xref:Bodu.IO.Biff.BiffWriter.WriteMulRk*> |
| `BLANK` | `0x0201` | Nothing (6 bytes total): a formatted cell with no value. | <xref:Bodu.IO.Biff.BiffBlankRecord> | <xref:Bodu.IO.Biff.BiffReader.GetBlank> | <xref:Bodu.IO.Biff.BiffWriter.WriteBlank*> |
| `MULBLANK` | `0x00BE` | No prefix: the row and first column, then a 16-bit XF index per cell, then the declared last column. | <xref:Bodu.IO.Biff.BiffMulBlankRecord> | <xref:Bodu.IO.Biff.BiffReader.GetMulBlank> | <xref:Bodu.IO.Biff.BiffWriter.WriteMulBlank*> |
| `BOOLERR` | `0x0205` | A value byte and a kind byte (8 bytes total): kind zero is a boolean, non-zero an error code. | <xref:Bodu.IO.Biff.BiffBoolErrRecord> | <xref:Bodu.IO.Biff.BiffReader.GetBoolErr> | <xref:Bodu.IO.Biff.BiffWriter.WriteBoolean*>, <xref:Bodu.IO.Biff.BiffWriter.WriteError*>, <xref:Bodu.IO.Biff.BiffWriter.WriteBoolErr*> |
| `LABEL` | `0x0204` | An inline 16-bit-length string; bytes after the string are ignored. | <xref:Bodu.IO.Biff.BiffLabelRecord> | <xref:Bodu.IO.Biff.BiffReader.GetLabel> | <xref:Bodu.IO.Biff.BiffWriter.WriteLabel*> |
| `LABELSST` | `0x00FD` | **BIFF8 only.** A 32-bit index into the shared string table (10 bytes total). The writer rejects it under BIFF5. | <xref:Bodu.IO.Biff.BiffLabelSstRecord> | <xref:Bodu.IO.Biff.BiffReader.GetLabelSst> | <xref:Bodu.IO.Biff.BiffWriter.WriteLabelSst*> |
| `RSTRING` | `0x00D6` | **BIFF5** rich text: a 16-bit-length byte string, then a run count byte and two-byte runs (character index, font index). Decoded as a byte string in either version. | <xref:Bodu.IO.Biff.BiffRStringRecord> | <xref:Bodu.IO.Biff.BiffReader.GetRString> | `WriteRecord` (raw) |
| `FORMULA` | `0x0006` | The eight-byte cached result, the option flags, a reserved 32-bit field, the token length, and the parsed-expression tokens (22 bytes plus tokens). A record shorter than 22 bytes decodes with empty tokens. | <xref:Bodu.IO.Biff.BiffFormulaRecord> (with <xref:Bodu.IO.Biff.BiffCachedResultKind>) | <xref:Bodu.IO.Biff.BiffReader.GetFormula> | <xref:Bodu.IO.Biff.BiffWriter.WriteFormula*> (numeric and special-result overloads) |
| `STRING` | `0x0207` | No prefix: a 16-bit-length string holding the cached text of the `FORMULA` immediately before it. | <xref:Bodu.IO.Biff.BiffStringRecord> | <xref:Bodu.IO.Biff.BiffReader.GetString> | <xref:Bodu.IO.Biff.BiffWriter.WriteString*> |

### The cached result of a formula

The eight-byte result field of a `FORMULA` record holds a double unless its last two bytes are `0xFFFF`; then its first byte selects the <xref:Bodu.IO.Biff.BiffCachedResultKind>:

| First byte | Kind | Value |
|---|---|---|
| `0` | `String` | The text is in the `STRING` record that follows. |
| `1` | `Boolean` | The third byte is zero or one — <xref:Bodu.IO.Biff.BiffFormulaRecord.BooleanValue>. |
| `2` | `Error` | The third byte is the error code — <xref:Bodu.IO.Biff.BiffFormulaRecord.ErrorCode>. |
| `3` (any other) | `Empty` | The formula produced an empty value. |

The writer's special-result overload takes the kind and the value byte and lays the field out accordingly.

## Strings

The version selects how every text field is stored; <xref:Bodu.IO.Biff.BiffString> is the one view over both forms.

| | BIFF8 Unicode string | BIFF5 byte string |
|---|---|---|
| Length prefix | 8-bit in `BOUNDSHEET` and `FONT`, 16-bit elsewhere — a **character** count. | 8-bit in `BOUNDSHEET`, `FONT`, and `FORMAT`, 16-bit elsewhere — a **byte** count. |
| Flags byte | Follows the length: bit 0 selects 16-bit code units over compressed 8-bit characters, bit 2 announces extended (phonetic) data, bit 3 announces rich-text runs. | None. |
| Characters | UTF-16LE code units, or one low byte per character when every character fits (`IsHighByte` tells which). | Bytes in the code page the stream's `CODEPAGE` record declared (Windows-1252 when none has been read). |
| Trailers | A 16-bit run count before the characters and four bytes per run after them; a 32-bit extended size before the characters and that many bytes after the runs. | None; `RSTRING` carries its own run table after the string. |
| Writer | Chooses the compressed form when every character fits a byte, 16-bit code units otherwise. | Encodes with the writer's code page; a character the code page cannot represent becomes its replacement character. |

## Shared string table

The BIFF8 `SST` record is the canonical continued structure. <xref:Bodu.IO.Biff.BiffSstReader> reads it by these rules, and <xref:Bodu.IO.Biff.BiffWriter.WriteSst*> writes it by the same:

- A string's header — length, flags, and any run count or extended size — never straddles a record boundary; when fewer than three bytes remain, the header opens the next `CONTINUE` record.
- A string's characters may straddle a boundary, but only at a character boundary — a 16-bit code unit is never split.
- Each continued run of characters restarts with its own flags byte, which may change the character width mid-string.
- Rich-run and extended-data trailers may straddle a boundary without a flags byte; the reader skips them and reports them as empty when they do.

A string that lies within one record is exposed as a span-backed <xref:Bodu.IO.Biff.BiffSstReader.Current> view; one that straddles a boundary has <xref:Bodu.IO.Biff.BiffSstReader.IsFragmented> set and is served through <xref:Bodu.IO.Biff.BiffSstReader.GetString> and <xref:Bodu.IO.Biff.BiffSstReader.CopyTo*> from a reusable scratch buffer.

## What the version changes

| Concern | BIFF5 | BIFF8 |
|---|---|---|
| `BOF` version marker | `0x0500` | `0x0600` |
| Maximum record payload | 2,080 bytes (<xref:Bodu.IO.Biff.BiffLimits.Biff5MaxPayloadLength>) | 8,224 bytes (<xref:Bodu.IO.Biff.BiffLimits.Biff8MaxPayloadLength>) |
| Text | Code-page byte strings | Unicode strings |
| `BOF` payload | 8 bytes | 16 bytes |
| `DIMENSIONS` rows | 16-bit (10-byte payload) | 32-bit (14-byte payload) |
| `XF` payload | 16 bytes | 20 bytes |
| `FILEPASS` | XOR key and hash | Type word, then XOR pair or RC4 header |
| Shared strings | None — every text cell is a `LABEL` or `RSTRING` | `SST` in the globals, `LABELSST` cells |
| Rich text | `RSTRING` cells | Runs inside the Unicode string |
| Compound-file stream name | `Book` | `Workbook` |

The reader establishes the version from the first `BOF` it reads, or from <xref:Bodu.IO.Biff.BiffReaderOptions.Version>; the writer is created with it. The version-dependent accessors — `GetBoundSheet`, `GetDimensions`, `GetLabel`, `GetString`, `GetFormat`, `GetFont`, `GetFilePass` — refuse to decode until it is known.

## Named identifiers without an accessor

<xref:Bodu.IO.Biff.BiffRecordType> also names identifiers the codec frames but does not decode — among them `INDEX`, `DBCELL`, `WINDOW1` / `WINDOW2`, `COLINFO`, `DEFCOLWIDTH`, `DEFAULTROWHEIGHT`, `MERGECELLS`, `NAME`, `EXTERNSHEET`, `SUPBOOK`, `SHRFMLA`, `ARRAY`, `TABLE`, `NOTE`, `OBJ`, `TXO`, `HLINK`, `PALETTE`, `STYLE`, `WRITEACCESS`, `CODENAME`, and the calculation and print settings. They exist so a `switch` on <xref:Bodu.IO.Biff.BiffReader.RecordType> can name them; their payloads are read through <xref:Bodu.IO.Biff.BiffReader.ValueSpan> and written back through <xref:Bodu.IO.Biff.BiffWriter.WriteRecord*>. The three legacy identifiers <xref:Bodu.IO.Biff.BiffRecordType.Biff2Bof>, <xref:Bodu.IO.Biff.BiffRecordType.Biff3Bof>, and <xref:Bodu.IO.Biff.BiffRecordType.Biff4Bof> are recognized so a pre-BIFF5 stream fails with <xref:Bodu.IO.Biff.BiffUnsupportedVersionException> rather than being mis-parsed.

## Vocabulary and configuration types

| Type | Role |
|---|---|
| <xref:Bodu.IO.Biff.BiffVersion> | `Biff5`, `Biff8`, or `Unknown` before a `BOF` has been read. |
| <xref:Bodu.IO.Biff.BiffRecordType> | The curated catalogue of record identifiers. |
| <xref:Bodu.IO.Biff.BiffSubstreamType> | The kinds of substream a `BOF` opens: workbook globals, worksheet, chart, macro sheet, and so on. |
| <xref:Bodu.IO.Biff.BiffSheetState> / <xref:Bodu.IO.Biff.BiffSheetType> | The visibility and kind a `BOUNDSHEET` record declares. |
| <xref:Bodu.IO.Biff.BiffCachedResultKind> | The kind of value in a `FORMULA` record's result field. |
| <xref:Bodu.IO.Biff.BiffEncryptionType> | The scheme a `FILEPASS` record declares. |
| <xref:Bodu.IO.Biff.BiffReaderOptions> | Seeds a known version and code page so a sheet substream can be read without its `BOF` first. |
| <xref:Bodu.IO.Biff.BiffReaderState> | The established version and code page, carried between readers when reading incrementally. |
| <xref:Bodu.IO.Biff.BiffWriterOptions> | The version the writer emits and the BIFF5 code page it encodes text in. |
| <xref:Bodu.IO.Biff.BiffRecordHeader> | The four-byte header: `TryParse` and `WriteTo` for callers framing records themselves. |
| <xref:Bodu.IO.Biff.BiffLimits> | Header size, per-version maximum payload, the default and Unicode code pages. |
| <xref:Bodu.IO.Biff.BiffRk> | `Decode` and `TryEncode` for the RK number representation. |

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary behind these tables.
- **[Getting started](getting-started.md)** — install and minimal samples.
- **[Introduction](index.md)** — the package at a glance.
- **API reference** — [Bodu.IO.Biff](xref:Bodu.IO.Biff).
