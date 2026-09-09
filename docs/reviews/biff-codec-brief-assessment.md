# BIFF5 / BIFF8 Reader and Writer — Brief Assessment

**Date:** 2026-09-09
**Scope:** The proposed low-level BIFF5/BIFF8 record codec (`BiffReader` / `BiffWriter`), its placement in the solution, and the consequential rework of the internal BIFF8 layer inside `Bodu.Formats.Excel.Binary`.
**Subject:** The external brief *"BIFF5 / BIFF8 Reader and Writer — Brief and Requirements"*, which asks for a forward-only, `ref struct`, allocation-conscious BIFF record reader and writer in the style of `Utf8BencodeReader` / `Utf8TomlReader`, deliberately without a workbook, worksheet, or cell model.

## 1. Verdict

**Worth doing, with a narrower first cut than the brief describes, and placed as `Bodu.IO.Biff` rather than `Bodu.Biff`.**

The strongest case for the codec is not new demand for BIFF5 or for authoring `.xls` files — both are niche in 2026 — but that the solution already contains a BIFF8 codec that is internal, BIFF8-only, and partially duplicated. `Bodu.Formats.Excel.Binary` carries four separate implementations of BIFF record framing, a hand-rolled BIFF8 record *writer* in its test project, and an explicit rejection of BIFF5 streams. Lifting the codec into its own package does for Excel what `Bodu.IO.Pst` does for `Bodu.Formats.Outlook.Pst`: it separates "how the bytes are laid out" from "what the workbook means", makes the wire layer testable and reusable on its own, and turns BIFF5 read support into a small delta rather than a fork.

The brief's design principles are sound and match the repository's conventions almost everywhere. The recommendations below adjust four things: the package name and namespace, the writer's destination type, how the reader is resumed from a class-based consumer, and the depth of the initial typed-record catalogue.

## 2. What exists today

`Bodu.Formats.Excel.Binary/src/Formats.Excel.Biff8/` is a 12-file internal layer (namespace `Bodu.Formats.Excel.Biff8`, ~1,100 lines with documentation):

| File | Role | Observation |
|---|---|---|
| `Biff8RecordCursor` / `Biff8Record` | Forward record framing over a span | Already a `ref struct` pair; exactly the shape the brief asks for, but `internal`. |
| `Biff8Payload` | Bounds-checked field reads | Throws `ExcelBinaryFormatException` naming the record type. |
| `Biff8RecordType` | 20-member `ushort` enum | Only the records the cell reader needs. |
| `Biff8StringReader` | Single-record BIFF8 string decode | Allocates a `string` on every call; no span view. |
| `Biff8SharedStringTable` | SST + CONTINUE decode | Materializes `string[]`; block-list model; well hardened (see forensic review 02-parsers). |
| `Biff8SubstreamLoader` | Copies one BOF…EOF substream from a `Stream` to `byte[]` | Parses record headers itself (framing copy #2). |
| `Biff8DimensionsReader` | Scans a `Stream` for DIMENSIONS | Parses record headers itself (framing copy #3). |
| `Biff8WorkbookGlobals` | Parses the globals substream | Rejects any BOF version other than `0x0600` with `ExcelBinaryUnsupportedException`. |
| `Biff8CellDecoder` | Cell records → `ExcelCell` | Mixes wire decode (row/col/ixfe/RK) with Excel mapping (`Biff8FormatTable`). |
| `Biff8FormatTable`, `Biff8SheetDirectoryEntry` | Excel-level tables | Not codec concerns; stay in Excel. |
| `ExcelWorksheetReader.TryStep` (in `Formats.Excel/`) | Inline record framing | Framing copy #4, duplicated because the public class cannot hold the `ref struct` cursor as a field. |

The test project's `Biff8TestWorkbook.cs` (~350 lines) is, in effect, an unversioned BIFF8 *writer*: `Record(id, payload)`, `Bof`, `Eof`, `Number`, `Rk`, `LabelSst`, `Label`, `BoolErr`, `MulRk`, `Formula`, `Sst`, `Dimensions`, `BoundSheet`, plus a two-pass `BuildWorkbookStream` that fixes up `lbPlyPos` offsets. Every property the brief lists for `BiffWriter` is already needed and already hand-rolled here.

BIFF5 today: `ExcelBinaryWorkbook` resolves a `Book` stream name (the BIFF5 convention) and then fails on the BOF version check. So the reader opens Excel 5.0/95 files far enough to reject them with a clear message, and nothing else.

## 3. Value assessment

| Driver | Value | Notes |
|---|---|---|
| **Consolidation of the existing BIFF8 layer** | High | Four framing copies collapse to one; wire decode separates from `ExcelCell` mapping; the codec becomes testable without a compound file around it. This is the payoff that exists regardless of BIFF5 or writing. |
| **Architectural consistency** | High | `Bodu.IO.Compound → Bodu.Formats.Excel.Binary` and `Bodu.IO.Pst → Bodu.Formats.Outlook.Pst` are the documented layering (`docs/guides/topics/binary-formats.md`). Excel is the one format package that owns its record substrate privately. `Bodu.IO.Biff` closes that gap and matches the "container-free record codec" the brief describes. |
| **BIFF5 read support in the Excel reader** | Moderate | Legacy-archive and forensic use only; no evidence of user demand in the repository. But once the codec is version-aware the Excel-side delta is small (§6.3), so it is cheap to take. |
| **A public writer** | Moderate | Immediate consumer: test fixture authoring for both packages (replacing `Biff8TestWorkbook`). Future consumer: an `.xls` authoring package over `Bodu.IO.Compound`'s existing writer. External demand for producing `.xls` in 2026 is low, so the writer should be justified by fixture use and round-trip testing, not by an authoring roadmap. |
| **Inspection / transformation / forensics** | Low–moderate | The brief's "copy or rewrite streams preserving unknown records" scenario is real for the forensic workstream but has no current consumer. Raw `WriteRecord(id, payload)` gives it almost for free. |
| **Risk: catalogue creep** | — | MS-XLS defines several hundred record types. The typed surface must be curated (§5.4) or the package never reaches "done". |
| **Risk: BIFF5 test evidence** | — | The repository has no BIFF5 fixture (`test/Fixtures/sample-biff8.xls` only). BIFF5 vectors will be hand-authored from the MS-XLS record layouts and cross-checked by round trip; a genuine Excel 95 fixture should be sourced before the Excel reader advertises BIFF5. |
| **Risk: shipped API** | — | `Bodu.Formats.Excel.Binary` is in release wave 2 (`bld/release-manifest.txt`) with package-validation baselines. The rework must be internal-only or additive on the Excel side; the exception contract in particular must be preserved (§6.4). |

**Net:** proceed. Sequence the work so the consolidation lands first and pays for itself even if the later phases slip.

## 4. Decisions where the assessment departs from the brief

### 4.1 Package and namespace: `Bodu.IO.Biff`, not `Bodu.Biff`

The brief names the component `Bodu.Biff`. The solution's convention for a container/record substrate with no application semantics is `Bodu.IO.<Format>` (`Bodu.IO.Compound`, `Bodu.IO.Pst`), and the application-level reader is `Bodu.Formats.<Domain>.<Format>`. BIFF is exactly that substrate for Excel, so:

- Project `Bodu.IO.Biff/` (`src/` + `test/`), assembly and package `Bodu.IO.Biff`, `RootNamespace` `Bodu`, public namespace `Bodu.IO.Biff` (folder `IO.Biff/`).
- Dependencies: `Bodu.Core` only. No `Bodu.IO.Compound` reference, per the brief.
- Test project `Bodu.IO.Biff.Test` with `InternalsVisibleTo`, and a `samples/IO.Biff/` entry to match the sibling packages.
- Resource strings `BiffResourceStrings.resx` + `.Designer.cs` under `src/IO.Biff/`, keys `Format_Invalid_Biff*` / `Op_NotSupported_Biff*`.

### 4.2 Type names: `BiffReader` / `BiffWriter`, not `Utf8Biff*`

The `Utf8` prefix on the text readers describes their input encoding; it is meaningless for a binary format. `BiffReader` / `BiffWriter` follow the brief. Companion types: `BiffVersion`, `BiffRecordType`, `BiffRecordHeader`, `BiffReaderOptions`, `BiffReaderState`, `BiffWriterOptions`, `BiffFormatException`, `BiffUnsupportedVersionException`.

### 4.3 Writer destination: `IBufferWriter<byte>`, not `Span<byte>`

The brief proposes `new BiffWriter(Span<byte> destination, BiffVersion)`. Every Bodu writer (`Utf8BencodeWriter`, `Utf8TomlWriter`, `Utf8YamlWriter`, the quartet writers) targets `IBufferWriter<byte>`, and a fixed span cannot absorb an SST whose CONTINUE split is only known while writing. Use `IBufferWriter<byte>` and expose `BytesCommitted` so a caller can compute `lbPlyPos`-style absolute offsets. A caller that wants a span can pass an `ArrayBufferWriter<byte>` or `PooledBufferBuilder`.

### 4.4 Resumability: add `BiffReaderState`

`ExcelWorksheetReader` is a public `sealed class` and therefore cannot hold a `ref struct` reader as a field — the reason it duplicates the framing loop today. `Utf8TomlReader` already solves this shape with `TomlReaderState` + `isFinalBlock`. Mirror it:

```csharp
public ref struct BiffReader
{
    public BiffReader(ReadOnlySpan<byte> data);
    public BiffReader(ReadOnlySpan<byte> data, BiffReaderOptions options);
    public BiffReader(ReadOnlySpan<byte> data, bool isFinalBlock, BiffReaderState state);

    public readonly BiffReaderState CurrentState { get; }   // version, code page, bytes consumed
    public readonly int BytesConsumed { get; }
    ...
}
```

Constructing the struct is a handful of field copies and allocates nothing, so a class-based consumer reconstructs it per call from the saved state. The `isFinalBlock` form also lets a `Stream`-fed consumer (the substream loader, the dimensions scanner) run the same framing code over chunks instead of parsing headers by hand. A `ReadOnlySequence<byte>` constructor is not needed in the first cut: a physical BIFF record is at most 8,228 bytes, so chunked reading over a span with `isFinalBlock = false` covers streaming.

### 4.5 Version detection and explicit version

`BiffVersion { Unknown = 0, Biff5, Biff8 }`. `Version` is `Unknown` until the first BOF is read, then fixed for the reader's lifetime; a sheet substream BOF must agree with the globals BOF or the reader throws `BiffFormatException`. `BiffReaderOptions.Version` lets a caller that already knows the version (a sheet substream read after the globals) pre-seed it, as the brief allows. BOF identifiers for BIFF2–4 (`0x0009`, `0x0209`, `0x0409`) are recognised and rejected with `BiffUnsupportedVersionException` so an old file fails as "unsupported", never as "malformed".

### 4.6 Code page as reader state

The brief's "acceptable state" list (version, current record, continuation state) omits one thing BIFF5 needs: the active code page from the CODEPAGE record (`0x0042`), without which BIFF5 byte strings cannot be materialised. Treat it exactly like the version — a small scalar the reader records when it passes the record and exposes as `CodePage`. Materialisation uses `System.Text.Encoding.CodePages` (already a dependency of `Bodu.Formats.Outlook.Msg`/`.Pst`, so the provider registration pattern exists in-repo). BIFF8 ignores it except for the rare compressed-string edge cases the spec calls out.

## 5. Proposed design

### 5.1 Reader

```csharp
public ref struct BiffReader
{
    // Positioning
    public bool Read();                                   // next physical record; false at clean end
    public readonly int BytesConsumed { get; }
    public readonly BiffReaderState CurrentState { get; }

    // Current physical record
    public readonly ushort RecordId { get; }              // raw identifier, always valid
    public readonly BiffRecordType RecordType { get; }    // (BiffRecordType)RecordId; unknown ids are not errors
    public readonly int RecordLength { get; }
    public readonly ReadOnlySpan<byte> ValueSpan { get; } // payload slice, never copied
    public readonly BiffRecordHeader Header { get; }
    public readonly bool IsContinuation { get; }          // RecordId == CONTINUE

    // Stream facts
    public readonly BiffVersion Version { get; }
    public readonly int CodePage { get; }

    // Continuation (opt-in, structure-aware)
    public bool TryReadContinuation(out ReadOnlySpan<byte> payload);  // consumes the next record only if it is CONTINUE

    // Typed accessors: interpret ValueSpan for the current record only; InvalidOperationException on the wrong record
    public readonly BiffBofRecord GetBof();
    public readonly BiffBoundSheetRecord GetBoundSheet();
    public readonly BiffDimensionsRecord GetDimensions();
    public readonly BiffRowRecord GetRow();
    public readonly BiffNumberRecord GetNumber();
    public readonly BiffRkRecord GetRk();
    public readonly BiffMulRkRecord GetMulRk();           // enumerable over the run, no array
    public readonly BiffLabelRecord GetLabel();
    public readonly BiffLabelSstRecord GetLabelSst();
    public readonly BiffBoolErrRecord GetBoolErr();
    public readonly BiffBlankRecord GetBlank();
    public readonly BiffMulBlankRecord GetMulBlank();
    public readonly BiffFormulaRecord GetFormula();
    public readonly BiffStringRecord GetString();         // cached formula string result
    public readonly BiffXfRecord GetXf();
    public readonly BiffFormatRecord GetFormat();
    public readonly BiffFontRecord GetFont();
    public readonly BiffCodePageRecord GetCodePage();
    public readonly BiffDateModeRecord GetDateMode();
    public readonly BiffFilePassRecord GetFilePass();
    public readonly BiffSstHeader GetSstHeader();         // counts only; strings via BiffSstReader
}
```

Framing rules: a trailing fragment of 1–3 bytes and a payload that overruns the buffer both throw `BiffFormatException` (present behaviour). The reader tolerates any `ushort` declared length on input; the *writer* enforces the per-version maximum (8,224 bytes for BIFF8, 2,080 for BIFF5). With `isFinalBlock = false`, an incomplete trailing record returns `false` from `Read()` without consuming it, so the caller can supply more data.

Typed record structs are `public readonly record struct`s in the `Bodu.IO.Biff` namespace with a static `Read(ReadOnlySpan<byte>, BiffVersion)` used by the accessors and a `Write(IBufferWriter<byte>, BiffVersion)` used by the writer, so one layout definition serves both directions and the round-trip tests. Version differences live inside each struct (for example `BiffDimensionsRecord` reads 16-bit rows in BIFF5 and 32-bit rows in BIFF8; `BiffLabelRecord` reads a byte string in BIFF5 and a Unicode string in BIFF8).

### 5.2 Strings

Two span-backed views, so nothing allocates until the caller asks:

- `BiffUnicodeString` (BIFF8): `CharCount`, `IsHighByte`, `HasRichRuns`, `HasExtendedData`, `RawCharacters` (the compressed or UTF-16LE bytes), `EncodedLength`, `CopyTo(Span<char>)`, `GetString()`.
- `BiffByteString` (BIFF5): `Length`, `RawBytes`, `GetString(Encoding)`, `GetString(int codePage)`.

Both are `readonly ref struct`s over the record payload. `BiffUnicodeString` handles the two length widths (8-bit `cch` in BOUNDSHEET and FONT, 16-bit elsewhere). The typed record structs expose these views rather than `string`.

### 5.3 SST and CONTINUE

`Read()` is strictly physical; CONTINUE records are visible as records. For the one structure that requires continuation-aware decoding, provide a companion:

```csharp
public ref struct BiffSstReader        // created at an SST record: new BiffSstReader(ref reader)
{
    public readonly uint TotalStringCount { get; }
    public readonly uint UniqueStringCount { get; }
    public bool Read();                                   // next string; pulls CONTINUE records from the parent as needed
    public readonly BiffUnicodeString Current { get; }    // may be a stitched view (see below)
    public readonly int Index { get; }
}
```

It carries a `ref BiffReader` field (C# 11 `ref` fields, available under the C# 14 toolchain) and advances the parent past each CONTINUE it consumes, so the parent's `BytesConsumed` stays truthful. A string whose characters straddle a CONTINUE boundary cannot be a single span; `Current` reports `IsFragmented`, and `GetString()` / `CopyTo` stitch across the boundary re-reading the option-flags byte per the BIFF8 rule — the logic already proven in `Biff8SharedStringTable.ReadCharacters`. That existing hardening (unique count bounded by available bytes, zero-length CONTINUE rejected, rich-run and extended-data skips bounded) transfers verbatim. Other continuable records (TXO, OBJ, NOTE) are left to `TryReadContinuation` and raw payloads in the first cut.

### 5.4 Curated record catalogue

`BiffRecordType : ushort` starts with roughly 45 members: the 20 the Excel reader already names, plus the structural and cell-adjacent records a stream walker needs to make sense of a workbook (ROW, INDEX, DBCELL, CODEPAGE, CODENAME, WINDOW1, WINDOW2, COLINFO, DEFCOLWIDTH, DEFAULTROWHEIGHT, MERGEDCELLS, NOTE, TXO, OBJ, HLINK, NAME, EXTERNSHEET, SUPBOOK, STYLE, PALETTE, FONT, FORMAT, XF, WRITEACCESS, INTERFACEHDR, MMS, PROTECT, PASSWORD, RSTRING, ARRAY, SHRFMLA, TABLE). Everything else is reachable through `RecordId` and `ValueSpan`. Typed accessors cover the 21 listed in §5.1; extending the catalogue is additive and should be demand-driven.

### 5.5 Writer

```csharp
public ref struct BiffWriter
{
    public BiffWriter(IBufferWriter<byte> output, BiffVersion version);
    public BiffWriter(IBufferWriter<byte> output, BiffWriterOptions options);

    public readonly BiffVersion Version { get; }
    public readonly long BytesCommitted { get; }

    public void WriteRecord(ushort recordId, ReadOnlySpan<byte> payload);   // enforces the per-version maximum length
    public void WriteRecord(BiffRecordType recordType, ReadOnlySpan<byte> payload);
    public void WriteContinuedRecord(ushort recordId, ReadOnlySpan<byte> payload);  // splits into CONTINUE frames at the maximum

    public void WriteBof(BiffSubstreamType type);
    public void WriteEof();
    public void WriteBoundSheet(in BiffBoundSheetRecord record);
    public void WriteDimensions(in BiffDimensionsRecord record);
    public void WriteRow(in BiffRowRecord record);
    public void WriteNumber(int row, int column, ushort xfIndex, double value);
    public void WriteRk(int row, int column, ushort xfIndex, uint rk);
    public void WriteLabel(int row, int column, ushort xfIndex, ReadOnlySpan<char> text);
    public void WriteLabelSst(int row, int column, ushort xfIndex, uint sstIndex);
    public void WriteBoolErr(int row, int column, ushort xfIndex, byte value, bool isError);
    public void WriteBlank(int row, int column, ushort xfIndex);
    public void WriteFormula(in BiffFormulaRecord record);
    public void WriteString(ReadOnlySpan<char> text);
    public void WriteSst(ReadOnlySpan<string> strings);                  // BIFF8 only; splits at string boundaries or, when a string exceeds a frame, at a character boundary with the flags byte repeated
    public void WriteXf(in BiffXfRecord record);
    public void WriteFormat(ushort formatIndex, ReadOnlySpan<char> code);
    public void WriteFont(in BiffFontRecord record);
    public void WriteCodePage(int codePage);
    public void WriteDateMode(bool is1904);
}
```

Version enforcement: a BIFF8-only record (SST, LABELSST) written under `Biff5` throws `InvalidOperationException` with a resource-backed message. Text written under `Biff5` is encoded with the writer's configured code page (`BiffWriterOptions.CodePage`, default 1252). The writer buffers a single record's payload in a stack or pooled scratch (max 8,228 bytes) so the length prefix is known before the header is emitted; it never buffers more than one record. Sequence validation is deliberately minimal (a BOF/EOF balance counter) — BIFF record ordering is a workbook concern, not a codec one.

### 5.6 Errors

- `BiffFormatException : FormatException` — the bytes are not a valid BIFF stream or a known record is malformed.
- `BiffUnsupportedVersionException : NotSupportedException` — a BIFF2/3/4 BOF, or a version-specific structure the codec does not implement.
- `InvalidOperationException` — a typed accessor called on the wrong record, a writer misuse (BIFF8 record under BIFF5, payload over the maximum).
- An unknown record identifier is never an error.

## 6. Reworking `Bodu.Formats.Excel.Binary`

### 6.1 Dependency and namespace

`Bodu.Formats.Excel.Binary.csproj` gains `<ProjectReference Include="..\..\Bodu.IO.Biff\src\Bodu.IO.Biff.csproj" />`. The internal namespace `Bodu.Formats.Excel.Biff8` becomes `Bodu.Formats.Excel.Biff` (folder `Formats.Excel.Biff/`) because what remains is no longer BIFF8-specific. CLAUDE.md's `Bodu.Formats.Excel.Binary` rows are updated accordingly.

### 6.2 File-by-file disposition

| Today | After |
|---|---|
| `Biff8RecordCursor`, `Biff8Record` | Deleted; `BiffReader` replaces them. |
| `Biff8Payload` | Deleted; the typed record structs in `Bodu.IO.Biff` carry the bounds checks. |
| `Biff8RecordType` | Deleted; `BiffRecordType` from `Bodu.IO.Biff`. |
| `Biff8StringReader` | Deleted; `BiffUnicodeString` / `BiffByteString`. |
| `Biff8SharedStringTable` | Deleted; `BiffSstReader` drives it, and `BiffWorkbookGlobals` materialises the `string[]` it still wants. The hardening tests move to `Bodu.IO.Biff.Test`. |
| `Biff8SubstreamLoader` | Kept as `BiffSubstreamLoader`; its header parsing is replaced by `BiffReader` over a chunk buffer with `isFinalBlock = false`, stopping after EOF. |
| `Biff8DimensionsReader` | Kept as `BiffDimensionsScanner`; same treatment, using `GetDimensions()` (which already knows the BIFF5 vs BIFF8 layout). |
| `Biff8WorkbookGlobals` | Kept as `BiffWorkbookGlobals`; version gate accepts `Biff5` and `Biff8`, reads BOUNDSHEET / FORMAT / XF / LABEL through the version-aware structs, skips the SST path under BIFF5, tracks `CodePage`. Exposes `Version` for a new additive `ExcelBinaryWorkbook.BiffVersion` property. |
| `Biff8CellDecoder` | Shrinks to `ExcelCellMapper`: takes the typed record structs (`BiffNumberRecord`, …) plus `Biff8FormatTable` and produces `ExcelCell`. RK decoding and the FORMULA cached-result discriminator move down into `Bodu.IO.Biff` (`BiffRk.Decode`, `BiffFormulaRecord.CachedResultKind`). |
| `Biff8FormatTable`, `Biff8SheetDirectoryEntry` | Renamed `BiffFormatTable` / `BiffSheetDirectoryEntry`; otherwise unchanged. |
| `ExcelWorksheetReader.TryStep` | Deleted; the class holds a `BiffReaderState` and reconstructs `new BiffReader(_data, isFinalBlock: true, _state)` at each `TryReadCell`. The FORMULA→STRING look-ahead uses the state snapshot in place of the saved `_position`. |
| `ExcelNumberFormat`, everything else under `Formats.Excel/` | Unchanged. |

### 6.3 BIFF5 support in the Excel reader

Once the codec is version-aware, the Excel-side delta is confined to `BiffWorkbookGlobals` and the cell mapper:

- Globals BOF `0x0500` is accepted; the substream type check is unchanged.
- BOUNDSHEET name, FORMAT code, LABEL text, and the cached STRING result are byte strings decoded with the CODEPAGE-derived encoding (default 1252 when no CODEPAGE record precedes them).
- XF: `ifmt` is at offset 2 in both versions; the BIFF5 record is 16 bytes rather than 20, which the existing `payload.Length >= 4` guard already tolerates.
- DIMENSIONS: 10-byte record with 16-bit rows; handled by `BiffDimensionsRecord`.
- RSTRING (`0x00D6`, rich-text label) is mapped as a text cell alongside LABEL.
- No SST / LABELSST under BIFF5; `LabelSst` under BIFF5 is a format error.
- `ExcelBinaryUnsupportedException` remains for BIFF2–4 and continues to carry the version in its message.

Publicly this is additive: the reader description, `docs/guides/excel/*`, and the package `Description` change from "BIFF8" to "BIFF5 and BIFF8", and `ExcelBinaryWorkbook.BiffVersion` is a new property. No existing member changes shape.

### 6.4 Exception contract

`Bodu.Formats.Outlook.Pst` lets `PstFileFormatException` propagate unwrapped from `Bodu.IO.Pst`. Excel cannot follow that precedent without a behavioural change: the package is shipped, and its tests (`ExcelWorksheetReaderTests.Malformed`, `MalformedRecordKat`) pin `ExcelBinaryFormatException` for every malformed-record case. Recommended: catch `BiffFormatException` at the two boundaries (`BiffWorkbookGlobals.Parse` and `ExcelWorksheetReader.TryReadCell`) and rethrow `ExcelBinaryFormatException` with the codec exception as `InnerException`. The existing `Format_Invalid_Biff8*` resource keys are kept (renamed to `Format_Invalid_Biff*`) for the wrapper messages.

### 6.5 Test migration

- `Biff8TestWorkbook` becomes a thin façade over `BiffWriter` (keeping `BuildWorkbookStream`'s two-pass `lbPlyPos` fix-up, which is a workbook concern) — so the Excel tests exercise the real writer, and a `Biff5` variant of the builder plus a `Book`-named stream gives the BIFF5 read tests their fixtures.
- `Biff8SharedStringTableTests` / `...HardeningTests` move to `Bodu.IO.Biff.Test` as `BiffSstReaderTests.*`.
- `MalformedRecordKat` sweeps stay in Excel to pin the wrapper contract; equivalent sweeps against `BiffReader` directly are added in `Bodu.IO.Biff.Test` asserting `BiffFormatException`.
- The `ExcelCellDecodeContractTests` vectors are unchanged (they test the `ExcelCell` mapping).

## 7. Test plan for `Bodu.IO.Biff`

Member-named backbone per the repository rule:

```text
IO.Biff/
  BiffReaderTests.cs                 // shared builders, [DynamicData] providers
  BiffReaderTests.Ctor.cs
  BiffReaderTests.Read.cs            // framing: clean end, trailing fragment, overrun, isFinalBlock=false
  BiffReaderTests.Version.cs         // BOF detection, BIFF2-4 rejection, mismatched sheet BOF
  BiffReaderTests.CodePage.cs
  BiffReaderTests.ValueSpan.cs
  BiffReaderTests.TryReadContinuation.cs
  BiffReaderTests.GetBof.cs … GetSstHeader.cs   // one file per typed accessor, BIFF5 and BIFF8 rows
  BiffReaderTests.Allocation.cs      // traversal allocates nothing (mirrors BencodeAllocationTests)
  BiffSstReaderTests.Read.cs / .Current.cs / .Hardening.cs
  BiffUnicodeStringTests.* / BiffByteStringTests.*
  BiffWriterTests.Ctor.cs / .WriteRecord.cs / .WriteContinuedRecord.cs / .WriteSst.cs / .Write<Record>.cs
  BiffWriterTests.Version.cs         // BIFF8-only records rejected under BIFF5; maximum length per version
  BiffRoundTripTests.cs              // write → read → compare for every typed record, both versions
  BiffMalformedRecordKat.cs          // InvalidKat<byte[]> rows
IO.Biff.Contracts/
  BiffRecordKat.cs                   // (Name, Version, Id, Payload, expected struct) rows for the accessor tests
Fixtures/
  sample-biff8.xls workbook stream (extracted), a sourced Excel 95 BIFF5 stream
```

Known-answer rows are transcribed from the MS-XLS record layouts (§2.4) and, for RK and the FORMULA cached-result discriminator, from the vectors already in `ExcelCellDecodeVectors`. Full record sweeps are tagged `Regression`; one `Smoke` test per primary type (`BiffReader`, `BiffWriter`, `BiffSstReader`).

## 8. Phased plan

| Phase | Deliverable | Notes |
|---|---|---|
| **1. Reader core** | `Bodu.IO.Biff` project, `BiffReader` framing + state + version/code-page tracking, `BiffRecordType`, `BiffRecordHeader`, exceptions, resources, the string views, the 21 typed record structs with `Read`, `BiffSstReader`. Tests for all of it. | Lifts the existing code; no behavioural change. Largest phase. |
| **2. Excel migration (BIFF8 parity)** | Excel references `Bodu.IO.Biff`; §6.2 dispositions applied; exception wrapping per §6.4; all 109 Excel tests green unchanged. | Proves the codec against the shipped reader before adding anything. |
| **3. Writer** | `BiffWriter` with raw, continued, and typed writes; round-trip tests; `Biff8TestWorkbook` re-based on it. | Typed `Write` on the record structs from phase 1 makes this mostly mechanical. |
| **4. BIFF5** | BIFF5 rows for every typed record; `BiffByteString` code-page decode; Excel globals accept `0x0500`; BIFF5 fixture and tests; docs and package descriptions updated. | Needs a sourced Excel 95 file before the Excel reader claims support publicly. |
| **5. Docs and samples** | `docs/docs/io-biff/index.md`, guides entry under *Binary Formats & I/O*, `docs/apidoc/Bodu.IO.Biff.md`, `samples/IO.Biff/`, CLAUDE.md rows, release-manifest wave entry. | Per `docs/reviews/docfx-coverage-audit-and-plan.md`'s navigation-parity requirement. |

Rough sizing: phase 1 is ~25 source files and ~35 test files; phase 3 adds ~10 and ~15; phases 2 and 4 are mostly edits. Phases 1–2 are the must-have; 3–4 can follow independently.

## 9. Open items to confirm with the owner

1. **Package name** — `Bodu.IO.Biff` (recommended, matches `IO.Compound` / `IO.Pst`) versus the brief's `Bodu.Biff`.
2. **Exception contract for the Excel reader** — wrap `BiffFormatException` in `ExcelBinaryFormatException` (recommended, preserves the shipped contract) versus letting it propagate as the PST reader does.
3. **BIFF5 fixture provenance** — whether a genuine Excel 95 workbook can be sourced for `test/Fixtures`; without one, BIFF5 support ships as "spec-conformant, round-trip-verified" rather than "verified against Excel output".
4. **Release wave** — whether `Bodu.IO.Biff` joins wave 2 with the Excel package it underpins (so the Excel package's new dependency ships alongside it) or waits for a later wave.
