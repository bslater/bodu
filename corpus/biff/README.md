# Bodu BIFF validation corpus

External evidence used to validate `Bodu.IO.Biff` and `Bodu.Formats.Excel.Binary`, kept in the repository so that every
reconciliation can be reproduced from committed files. [MS-XLS] is the specification Bodu follows for the record
streams, and [MS-CFB] for the compound file that carries them; Microsoft publishes no conformance vectors for either, so
the evidence here is what other `.xls` readers learned by fixing their own defects.

## `fixes/` - the release-note fix catalogue

The release notes of six `.xls` readers were read in full, and every fix they list that touches the BIFF record streams,
the compound file around them, or the values a reader takes from them is catalogued: one file per library,
`fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. A scenario is, first, the
regression test the fix added, read from its commit, and otherwise the scenario its issue, commit or released code change
describes. Each row records the version, the reference and a short summary in our own words, then a class, and, for a
runnable row, the input and the result Bodu must give.

### Sources

| File | Library | Language | What was read | Licence |
|---|---|---|---|---|
| `xlrd-fixes.csv` | [xlrd](https://github.com/python-excel/xlrd) | Python | `CHANGELOG.rst` at 60c4cff7b003234ea88fe933cf98a86588e830fb (SHA-256 eac5b790c7c8c4679c14c8f5fbb2a9152fe1b7c5b60fc81c7b0a652c48ac37e3; `docs/changes.rst` includes it), 0.3a1 to 2.0.2; the commit history at the same commit for the releases whose entries are vague and for 0.7.9, which the changelog omits | BSD-3-Clause |
| `exceldatareader-fixes.csv` | [ExcelDataReader](https://github.com/ExcelDataReader/ExcelDataReader) | C# | The GitHub releases, pages 1-3, read 2026-10-08 (3.0.0-develop00019 to 3.9.0, pre-releases included); the commit history at 64a0c2a2ed29454f954797913fc9202854d7a7d1, since the notes list only highlights | MIT |
| `calamine-fixes.csv` | [calamine](https://github.com/tafia/calamine) | Rust | `Changelog.md` at 0af05f4f6030351e3b8a999ea0810c8618368776 (SHA-256 252b2f7374f59f398457199a38d091992abc098ece475dfd07619958fafe1e80), 0.1.0 to 0.36.1, each entry read with its commits | MIT |
| `libxls-fixes.csv` | [libxls](https://github.com/libxls/libxls) | C | The GitHub releases, pages 1-2, read 2026-10-08 (0.2.0 to 1.6.3); the changelog section of `doc/homepage/index.html` as shipped from 0.3.0 to 1.4.0 (SHA-256 e913a984608e0bf4b6fd6e494b2674e4887e25e5e6e8153f06bd58b0637e2606); the list of changes since 1.4 in `README.md` at 9421df70d01aecf75381d2b2ab6128fbbadbe3d2 (SHA-256 4c0a8dc2de8d82f8333933e140c8ee757e4adf7a2b0c494cc7f7683593835c6d), which stands for the 1.5.0 notes; the commit history at that commit for 0.3.0 to 1.5.0 | BSD-2-Clause |
| `spreadsheet-parseexcel-fixes.csv` | [Spreadsheet::ParseExcel](https://metacpan.org/dist/Spreadsheet-ParseExcel) | Perl | `Changes` as shipped in the 0.66 distribution on metacpan (SHA-256 3b6ff9128e8ac1fcbd9ed852cee251286ba9387a3117e291e9e94cd6b63a0a8f), 0.05 to 0.66, read 2026-10-08; the diffs between consecutive released distributions that metacpan's diff API serves, for the released code change of each entry | Artistic-1.0-Perl OR GPL-1.0-or-later |
| `apache-poi-fixes.csv` | [Apache POI](https://github.com/apache/poi) (HSSF) | Java | The History of Changes pages [changes.html](https://poi.apache.org/changes.html) (SHA-256 0160cc45b13a338988d64dc6863f72c1ecef9ff133d8b2c8d5c771ee9d6bdca3), [changes-3x.html](https://poi.apache.org/devel/history/changes-3x.html) (SHA-256 3332f9f5bf4288220d1da148f39aa8b72085628c9748d52dde2543a1dc37ab5f) and [changes-pre3x.html](https://poi.apache.org/devel/history/changes-pre3x.html) (SHA-256 b9dea0044cb83d867f1177d074026538dec234879f2af088f7d9361790cc9aac), read 2026-10-08, 0.1 to 5.5.1 and the unreleased 6.0.0 | Apache-2.0 |

Each file's header repeats its sources and records what was left out and why.

### What is catalogued

- Fixes only. New features, refactors, performance work, documentation, tests, build and dependency changes are not
  catalogued. A fix to packaging, a platform, the library's language or performance is catalogued, as `n/a`.
- Where the notes are vague or missing (xlrd before 1.0, ExcelDataReader, calamine, libxls), the commit history was read
  as well. A fix found only there is catalogued under the first release that contains it, with the issue it names or the
  commit as its reference, unless it fixes code first added in the same release cycle.
- ExcelDataReader and calamine: only entries that reach the `.xls` reader (its BIFF code, its compound-file reader, and
  the number-format, date and code-page code it shares with the other readers). Apache POI: only entries filed under the
  HSSF module, with three filed elsewhere that concern reading `.xls` record streams. Each file's header names what this
  leaves out.
- libxls: memory-safety fixes (overflows, leaks, NULL dereferences, undefined behaviour) are `n/a` unless their input
  also shows a format rule.
- Spreadsheet::ParseExcel is copyleft, so every input in its file was written for this catalogue from the entry, the
  released code change and [MS-XLS], and none of its test data is restated. From the permissive libraries a short input
  from a regression test is restated, and the test is named in `reason`.
- The trackers of two libraries could not be read: rt.cpan.org (Spreadsheet::ParseExcel) returned empty responses, and
  the Apache Bugzilla (POI) now requires a login. Their scenarios come from the entries, the released code (ParseExcel)
  and the specifications.
- **NPOI is not catalogued.** [NPOI](https://github.com/nissl-lab/npoi) is a .NET port of Apache POI, so its HSSF fixes
  are POI's, and cataloguing it would count them twice. No other library here is a port of another.
- No upstream sample workbook is used: every input is generated (below), so no `file:` input and no upstream file is in
  the repository.

### Columns

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes write it; `unreleased` for an entry not yet released |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (first 10 hex digits), `RT #123` (an rt.cpan.org ticket), `Bug 123` (an Apache Bugzilla bug), another tracker's id, or empty |
| `summary` | A short paraphrase of the entry |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it needs several |
| `kind` | `parse`, `reject`, `roundtrip` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` (below); empty for the defaults |
| `input`, `expected` | The input and the expected result, escaped (below) |
| `expectation` | Where `expected` comes from: `upstream` (the fix's own test), `spec` ([MS-XLS] or [MS-CFB]), `derived` (worked out, with the working in `reason`) |
| `reason` | Required on every row: the upstream test, issue or code change for a runnable row, the Bodu document or specification section a `dialect` row relies on, the exclusion an `n/a` row falls under |

Every file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII.
Anything else in `input` and `expected` is written with an escape: `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and
`\u{H...}` (the UTF-8 encoding of one Unicode scalar). Fields follow RFC 4180 quoting.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 369 | The row runs, and Bodu must do what the fix established |
| `dialect` | 44 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix (Bodu does not decrypt, reads BIFF5, compares sheet names ordinally, rejects an out-of-range shared-string index, reads the strings an SST holds whatever count it declares, lets the last FORMAT record for an index decide it, built-in index or not); `reason` names the document or section |
| `n/a` | 443 | The fix concerns something Bodu does not have (editing, styles, formula parsing and evaluation, drawings, text formatting, xlsx), memory safety, a platform, the library's language, logging or performance; `reason` says which |
| `unknown` | 11 | No scenario could be found; `reason` says what was read |

### Inputs

Every input is a generator expression, `gen:Stream(...)` or `gen:Sheet(...)`. A generator builds a BIFF record stream
byte by byte from [MS-XLS], independently of `BiffWriter`, and the Workbook surface stores it as a stream of a compound
file that `Bodu.IO.Compound` writes (`CompoundFile.Create`, one stream in the root storage, then `Commit`). The layout
version is BIFF8 unless the row's options say `Version=Biff5`; the stream is named `Workbook` for BIFF8 and `Book` for
BIFF5 unless a setting names it. Everything is deterministic.

- `gen:Stream(A | B | ...)` writes the substreams `A`, `B`, ... in order. Each is a list of record constructors and
  settings separated by spaces; `|` separates substreams at the top level (not inside parentheses or quotes).
- `gen:Sheet(G | S)` is shorthand for `gen:Stream(BOF(globals) G SHEET(1,'Sheet1') EOF | BOF(sheet) S EOF)`, and
  `gen:Sheet(S)` for the same with no globals items.

**Settings** may appear anywhere among the items, each as one token:

| Setting | Effect |
|---|---|
| `@stream=NAME` | Names the record stream `NAME` in the compound file |
| `@cp=N` | The code page BIFF5 text without a `cpN` prefix is encoded in (default 1252) |
| `@pad=N` | Zero-pads the record stream to `N` bytes |
| `@extra=NAME:LENGTH` | Adds a stream `NAME` of `LENGTH` bytes, each 0x5A, beside the record stream |
| `@cfb=PATCH` | Applies a container patch (below) after the compound file is written; patches apply in order |
| `@container=none` | Uses the record stream itself as the file, with no compound file |

**Record constructors.** Arguments are separated by commas; `r` and `c` are a 0-based row and column, `xf` an optional
XF index (default 0), numbers are decimal or `0x` hexadecimal, and text is a text argument (below). BIFF8 strings are
written as XLUnicodeString (compressed when every character is at most U+00FF, else 16-bit) and BIFF5 strings as byte
strings in the code page.

| Constructor | Record written |
|---|---|
| `BOF(type)` | BOF 0x0809: version 0x0600 (BIFF8) or 0x0500 (BIFF5), the type (`globals` 0x0005, `vb` 0x0006, `sheet` 0x0010, `chart` 0x0020, `macro` 0x0040, `workspace` 0x0100, or a number), build 0x0DBB, year 0x07CC, and in BIFF8 the history flags 0x000040C1 and lowest version 0x00000206 (16 bytes; 8 in BIFF5) |
| `EOF` | EOF 0x000A, no data |
| `SHEET(k, name[, state[, type]])` | BOUNDSHEET 0x0085 whose offset is the start of substream `k` (0-based); `state` is `visible`, `hidden`, `veryhidden` or a number, `type` is `worksheet`, `macro`, `chart`, `vb` or a number; the name is a ShortXLUnicodeString (BIFF8) or a byte string with a one-byte length (BIFF5) |
| `BOUNDSHEET(offset, name[, state[, type]])` | The same with an explicit offset |
| `CODEPAGE(n)`, `DATEMODE(n)` | CODEPAGE 0x0042 and DATEMODE 0x0022 holding the 16-bit value `n` |
| `DIMENSIONS(r1, r2, c1, c2)` | DIMENSIONS 0x0200: first row and one-past-last row (32-bit in BIFF8, 16-bit in BIFF5), first column and one-past-last column (16-bit), then a reserved zero word |
| `XF(font, format)` | XF 0x00E0: the font and format indexes, then zeros (20 bytes in BIFF8, 16 in BIFF5) |
| `FORMAT(index, code)` | FORMAT 0x041E: the 16-bit index, then the code as an XLUnicodeString with a 16-bit length (BIFF8) or a byte string with a one-byte length (BIFF5) |
| `SST(text, ...)` | SST 0x00FC: total and unique counts equal to the number of strings, then each as an XLUnicodeRichExtendedString with no runs or phonetic data |
| `SSTN(n[, prefix])` | An SST of `n` compressed strings `prefix0` to `prefix(n-1)` (prefix `s` by default), continued in CONTINUE records so that no record holds more than 8,224 bytes; no string is split |
| `NUMBER(r, c, value[, xf])` | NUMBER 0x0203 with an IEEE double (`NaN` and `Infinity` are accepted) |
| `RK(r, c, rk[, xf])` | RK 0x027E holding the 32-bit RK value `rk` as given |
| `MULRK(r, c, xf:rk, ...)` | MULRK 0x00BD: one XF and RK value per cell from column `c`, then the last column |
| `BLANK(r, c[, xf])`, `MULBLANK(r, c, xf, ...)` | BLANK 0x0201 and MULBLANK 0x00BE |
| `BOOLERR(r, c, value, isError[, xf])` | BOOLERR 0x0205 with the two bytes of a Bes |
| `LABEL(r, c, text[, xf])` | LABEL 0x0204 with an XLUnicodeString (16-bit length) or a BIFF5 byte string with a 16-bit length |
| `LABELSST(r, c, index[, xf])` | LABELSST 0x00FD with a 32-bit index |
| `RSTRING(r, c, text, runs[, xf])` | RSTRING 0x00D6 in the BIFF5 layout: a 16-bit byte length, the code-page bytes, a one-byte run count, and `runs` runs of two bytes (character index `i`, font 0) |
| `FORMULA(r, c, result[, xf[, tokens]])` | FORMULA 0x0006: the cached result (`num:X`, `bool:B`, `err:E`, `str`, `empty`, or `raw:` eight payload bytes), flags 0, a zero chn, then `cce` and the tokens given as payload items |
| `STRING(text)` | STRING 0x0207 with an XLUnicodeString (16-bit length) or a BIFF5 byte string |
| `ROW(r, c1, c2)` | ROW 0x0208: the row, first column and one-past-last column, then ten zero bytes |
| `REC(id[, payload])` | Any record: `id` is a `BiffRecordType` name (any case) or a number, the payload is a list of payload items |
| `HDR(id, length)` | A bare four-byte record header with no data after it |
| `RAW(payload)` | The payload bytes with no header |

**Payload items** are separated by spaces: an even run of hex digits; `u8:N`, `u16:N`, `i16:N`, `u32:N`, `i32:N` and
`f64:X` (little-endian); `z:N` (`N` zero bytes); and a quoted text item, which writes characters only (no length or
flags): `'...'` one byte per character (each at most U+00FF), `w'...'` UTF-16LE, `cpN'...'` the bytes of code page `N`.
A quoted item may be followed by `*N` to repeat its text `N` times.

**Text arguments** are `'...'`, `w'...'` (16-bit characters in BIFF8) or `cpN'...'` (bytes of code page `N`, written in
the BIFF5 layout even in a BIFF8 stream). Their contents use the catalogue escapes, so `\u{416}` is U+0416.

**Container patches** (`@cfb=`) locate everything through the written file's own header, FAT and directory, so they do
not depend on where `CompoundFile.Create` placed the sectors. A `VALUE` is a number, `end` (0xFFFFFFFE), `free`
(0xFFFFFFFF), `fatsect` (0xFFFFFFFD), `difsect` (0xFFFFFFFC) or `@NAME` (the named entry's first sector).

| Patch | Effect |
|---|---|
| `hdr:OFF:HEX` | Overwrites the header bytes from offset `OFF` with the hex bytes, in the order written (`hdr:0x2C:FFFFFF7F` writes FF FF FF 7F) |
| `dir:NAME:FIELD:VALUE` | Sets a field of the directory entry `NAME` (`root` is entry 0; `#N` is the entry at index `N`, used or free): `type` (1 byte at 66), `color` (67), `left` (68), `right` (72), `child` (76), `start` (116), `size` (8 bytes at 120), `namelen` (2 bytes at 64), `ctime` or `mtime` (the FILETIME at 100 or 108, a signed 64-bit value) |
| `fat:NAME:K:VALUE` | Sets the FAT entry of the `K`-th sector of the entry's chain (from 0; -1 is the last); `NAME` `directory` names the directory's own chain |
| `size:N`, `cut:N`, `add:N` | Truncates or zero-extends the file to `N` bytes, removes `N` bytes from its end, or appends `N` zero bytes |
| `move:NAME:K` | Copies the `K`-th sector of the entry's chain to a new sector at the end of the file, links the chain through the copy (the entry's start sector when `K` is 0) and frees the old sector, so the chain is no longer in file order |
| `difat:VALUE` | Appends one DIFAT sector that names no FAT sector (every slot FREESECT) and whose next-sector field is `VALUE`, points the header at it (first DIFAT sector, count 1) and marks it DIFSECT in the FAT |
| `difmove:K` | Copies the `K`-th DIFAT sector to a new sector at the end of the file, links the DIFAT chain through it, marks it DIFSECT and frees the old one |
| `difnext:K:VALUE` | Sets the next-sector field of the `K`-th DIFAT sector; `VALUE` may also be `self` |
| `difslot:K:I:VALUE` | Sets slot `I` (a FAT sector number) of the `K`-th DIFAT sector |

A row that needs more than 109 FAT sectors (and so DIFAT sectors) pads its record stream with `@pad`, for example
`@pad=8000000`.

### How a row runs

The **Workbook** surface (the default) opens the compound file with `ExcelBinaryWorkbook.Open` (with
`ExcelBinaryReaderOptions.DetectDateFormats` from the `DetectDates` option) and renders, separated by LF:

- with `Book=true`, a first line `#book<TAB><BiffVersion><TAB><DateSystem>`;
- for each sheet in `Worksheets`, a line `#sheet<TAB><index><TAB><name><TAB><Visibility><TAB><Type><TAB>r<first>+<count>:c<first>+<count>`
  from its `ExcelWorksheetInfo` and `Dimensions`,
- then one line per cell that `OpenWorksheet(index).ReadCells()` returns, in row-major order (cells at the same
  position keep their record order): `<A1 reference><TAB><ExcelCellKind><TAB><value>`, the value a number in invariant
  round-trip form (`R`), `true` or `false`, an `ExcelErrorCode` name (or `0xHH`), or the text. With `Formats=true` each
  cell line also carries `<TAB><FormatIndex><TAB><code><TAB><date or ->`, the code from
  `ExcelBinaryWorkbook.GetNumberFormatCode` (`(null)` when there is none) and `date` when `ExcelCell.IsDateFormatted`.

Text in a line escapes a backslash, tab, line feed and carriage return as `\\`, `\t`, `\n` and `\r`, and an unpaired
surrogate as `\u{XXXX}`; the line is then written into the `expected` field with the catalogue escapes.

The **Records** surface reads the record stream with `BiffReader` (its `BiffReaderOptions.Version` from the `Version`
option when given) and renders one line per record: the `BiffRecordType` name or `0xNNNN`, the record length, and for
these records the decoded fields, tab-separated: `Bof` (version, substream type), `CodePage` (raw value, code page),
`DateMode` (`1900` or `1904`), `BoundSheet` (offset, state, sheet type, name), `Dimensions` (first row, row after the
last, first column, column after the last), `Row` (row, first column, column after the last), `Number` and `Rk` (row,
column, XF, value), `MulRk` (row, first and last column, then `xf:value` per cell), `Blank` (row, column, XF),
`MulBlank` (row, first and last column, count), `BoolErr` (row, column, XF, `true`, `false` or `error:0xHH`), `Label`
(row, column, XF, text), `LabelSst` (row, column, XF, index), `RString` (row, column, XF, text, run count), `Formula`
(row, column, XF, the cached result kind and its value), `String` (text), `Xf` (font, format), `Format` (index, code),
`Font` (height, weight, escapement, name), `FilePass` (encryption type) and `Sst` (total and unique counts, followed by a
line `#sst<TAB><index><TAB><text>` per string, read with `BiffSstReader` across its CONTINUE records).

| Kind | Passes when |
|---|---|
| `parse` | Rendering `input` through the row's surface succeeds and equals `expected` |
| `reject` | Rendering `input` throws an exception whose type name is `expected` (`ExcelBinaryFormatException`, `ExcelBinaryUnsupportedException`, `ExcelBinaryEncryptedWorkbookException`, `ExcelBinaryWorkbookStreamNotFoundException` or `CompoundFileFormatException` on the Workbook surface; `BiffFormatException` or `BiffUnsupportedVersionException` on the Records surface) |
| `roundtrip` | The record stream is read with `BiffReader` and every record written back through `BiffWriter` (the typed writers for the records the Records surface decodes, `WriteSst` for an SST and its CONTINUE records, `WriteRecord` for the rest), with the version of the first BOF and the first CODEPAGE value as `BiffWriterOptions`; the Records listing of the written stream, without lengths, must equal that of the input, and when `expected` is not empty the written bytes must equal it |
| `unit` | Not run from the file: `input` names a test to be written (`<TestClass>.<Member>_When<Condition>_Should<Result>`), and `reason` gives its input and expected outcome |

| Option | Maps to | Values |
|---|---|---|
| `Surface` | the rendering | `Workbook` (default) or `Records` |
| `Version` | `BiffReaderOptions.Version` and the generators' layout | `Biff5` or `Biff8` (default) |
| `Book` | a first `#book` line on the Workbook surface | `true` or `false` (default) |
| `Formats` | the format index, code and date flag on each cell line | `true` or `false` (default) |
| `DetectDates` | `ExcelBinaryReaderOptions.DetectDateFormats` | `true` (default) or `false` |

### Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown | unit | runnable |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| xlrd | 124 | 136 | 69 | 9 | 57 | 1 | 1 | 77 |
| exceldatareader | 70 | 88 | 63 | 14 | 10 | 1 | 3 | 74 |
| calamine | 47 | 55 | 42 | 5 | 8 | 0 | 3 | 44 |
| libxls | 52 | 58 | 37 | 5 | 16 | 0 | 2 | 40 |
| spreadsheet-parseexcel | 110 | 120 | 51 | 1 | 65 | 3 | 3 | 49 |
| apache-poi | 404 | 410 | 107 | 10 | 287 | 6 | 6 | 111 |
| **Total** | **807** | **867** | **369** | **44** | **443** | **11** | **18** | **395** |

### Status

The catalogue runs in `Bodu.Formats.Excel.Binary.Test` as `ExcelBinaryReleaseNoteCorpusTests`, with the same results on
net8.0 and net10.0: of the 395 runnable rows, 391 pass and 4 fail, and the tests the 18 `unit` rows name all pass.
Before the fixes below, a harness outside the repository found 373 passing and 22 failing at 378a6099db. A failing row
stays as it is and is listed here; a row changed class only where the decision on its issue said so, as recorded below.

Fixed, each after a commit of its regression tests:

- **String flags (#934).** BOUNDSHEET and FONT names (ShortXLUnicodeString) and LABEL, FORMAT and STRING text
  (XLUnicodeString) read only bit 0 of their flags byte and ignore the reserved bits, as [MS-XLS] 2.5.240 and 2.5.294
  require (calamine 0.18.0 #195 case 3).
- **Shared string table, a flags-only CONTINUE (#935).** A CONTINUE holding only a continued string's flags byte
  contributes no characters, and the string goes on in the next one (exceldatareader 3.7.0-develop00310 #477 case 1).
- **BOOLERR as OpenOffice.org wrote it (#936).** A nine-byte BOOLERR is read in that layout, so an error no longer reads
  as `TRUE` (apache-poi 3.5-final Bug 47479 case 2).
- **Shared string table, the declared count (#937).** The table ends where its records end, even when `cstUnique`
  declares more strings. As decided, apache-poi 5.2.4 Bug 65543, apache-poi 3.12-beta1 Bug 57456, calamine 0.30.1 #548
  and exceldatareader 3.7.0-develop00310 #477 case 4 moved from `dialect` (reject) to `applies` (parse), citing
  docs/docs/io-biff/records.md:89 and BiffSstReader.cs:243, and libxls 1.5.0 #22 case 2, which expected an SST declaring
  16,777,217 strings to be rejected, moved from `applies` (reject) to `dialect` (parse, A1 reading the one string it
  holds), citing docs/docs/io-biff/records.md:89.
- **Exception translation (#933).** A sheet whose BOF declares BIFF4 raises `ExcelBinaryUnsupportedException`, with the
  codec's exception inside (apache-poi 3.11-beta1 Bug 53984).
- **A STRING record continued in CONTINUE records (#929)** is read whole (xlrd 0.7.4 5a75bb5224 cases 1 and 2,
  exceldatareader 3.0.0-develop00086 c0218db807).
- **A STRING after an ARRAY, TABLE or SHRFMLA record (#930)** is taken as the formula's text (xlrd 0.6.0a2).
- **Chart sheets (#926).** A sheet that is not a worksheet yields no cells (spreadsheet-parseexcel 0.51 RT #44009).
- **Embedded charts (#927).** A worksheet ends at the EOF that matches its own BOF, and a chart substream nested in it
  is skipped, so the chart's data cache is not read as cells (spreadsheet-parseexcel 0.47 RT #13365).
- **A missing EOF (#928).** A sheet without its EOF ends where the next sheet's substream begins, at the nearest
  BOUNDSHEET offset past its own, or at the end of the stream (exceldatareader 3.4.1 #321).
- **Thai built-in date formats (#931).** Indexes 71 to 81 are dates, and 59 to 62 and 67 to 70 stay numbers (xlrd 0.7.2
  a1cc4aaada case 1, exceldatareader 3.9.0 PR #736 cases 2 to 4).
- **FORMAT records (#932).** A FORMAT record decides its index's code and its date classification, built-in index or
  not, and of several records for one index the last applies (xlrd 0.5.2a3). As decided, calamine 0.27.0 PR #479 case 2
  (index 1 keeps its built-in code) and exceldatareader 3.7.0 PR #419 (the first record wins) are now `dialect`, citing
  docs/docs/excel/concepts.md:63 and ExcelBinaryWorkbook.cs:365, with `expected` set to what Bodu produces.
- **Documentation (#938).** The guides say that a malformed record in the workbook globals fails the open and one in a
  sheet fails when the sheet is read, and the package README lists RSTRING.

Still failing:

- **Compound file tolerance, pending.** xlrd 1.0.0 #85 (a DIFAT chain ending in DIFSECT), xlrd 0.7.2 7404fa2117 case 1
  (a header whose first DIFAT sector is 0 with a count of 0), exceldatareader 3.4.0 #312 case 1 (a leaf directory entry
  with a name length of 200, rejected as a cyclic link although `CompoundValidationLevel.Compatible` documents skipping
  such an entry) and xlrd 0.9.0 14b25cb1d7 (a stream that declares 1,000,000 bytes in a small file is not rejected) fail
  in `Bodu.IO.Compound`. The compound branch fixes them under #944, #947, #950 and #951, and they pass once it merges.
