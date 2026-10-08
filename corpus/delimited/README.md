# Delimited release-note fix catalogue

`fixes/` holds one file per CSV library, `fixes/<library>-fixes.csv`: every defect fix the library's release notes
list (or, where the notes are missing or incomplete, its fix commits), turned into a row that states the scenario
of the fix and what `Bodu.Text.Delimited` must do with it. A row that can run is a parse, reject, write or round-trip
case against `Utf8DelimitedReader` and `Utf8DelimitedWriter`; a scenario that needs typed code (a record type, a
stream that returns a few bytes at a time) is a `unit` row naming the test that covers it; a fix that cannot touch Bodu
is recorded as `n/a` with the reason. Nothing here is copied from the libraries: each row restates the fix's
scenario in a few words and, from these permissively licensed projects, at most a short test input, with the test
named in `reason`.

## Sources

| Library | File | Language, licence | What was read | Versions |
|---|---|---|---|---|
| [Apache Commons CSV](https://github.com/apache/commons-csv) | `commons-csv-fixes.csv` | Java, Apache-2.0 | `src/changes/changes.xml` at `26f2e86962` (SHA-256 `4412dea8...cbb6eb`) | 1.0 to the unreleased 1.15.0 |
| [CPython `csv`](https://github.com/python/cpython) | `cpython-csv-fixes.csv` | C and Python, PSF-2.0 | the `Misc/NEWS.d` and `Misc/HISTORY` entries that name the module, at `b22a8c9fd4` (main) and on the 2.7, 3.1, 3.2 and 3.3 branches; the SHA-256 of each file is in the header | 2.3b1 to the unreleased 3.16.0a1 |
| [Go `encoding/csv`](https://github.com/golang/go) | `go-csv-fixes.csv` | Go, BSD-3-Clause | the release notes go.dev/doc/go1 to go1.27 and go.dev/doc/devel/release (read 2026-10-08), and the package's git log at `3b98eddbcd` | weekly.2011-10-18 to 1.27 |
| [rust-csv](https://github.com/BurntSushi/rust-csv) (with `csv-core`) | `rust-csv-fixes.csv` | Rust, MIT OR Unlicense | no release notes exist, so the git log at `05612e87e6` (475 commits) | 0.2.0 to 1.4.0, csv-core 0.1.13 |
| [CsvHelper](https://github.com/JoshClose/CsvHelper) | `csvhelper-fixes.csv` | C#, MS-PL OR Apache-2.0 | the website change log at `33970e5183` (SHA-256 `1d2115e3...0c7fb`) | 0.12.0 to 33.1.0 |
| [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan) | `sylvan-csv-fixes.csv` | C#, MIT | `docs/Csv/Sylvan.Data.Csv.Releases.md` at `6d14b7b5b8` (SHA-256 `c5d46631...152895`) | 0.1.0 to 1.4.4 |
| [Sep](https://github.com/nietras/Sep) | `sep-fixes.csv` | C#, MIT | GitHub Releases pages 1 to 6 (read 2026-10-08), checked against the git log between tags at `ea71450b88` | 0.1.0-rc.1 to 0.17.1 |
| [csv-parse](https://github.com/adaltas/node-csv) | `csv-parse-fixes.csv` | JavaScript, MIT | `packages/csv-parse/CHANGELOG.md` at `1500f7ce00` (SHA-256 `eb964429...97118f`) | 1.0.0 to 7.0.3 |
| [Papa Parse](https://github.com/mholt/PapaParse) | `papaparse-fixes.csv` | JavaScript, MIT | GitHub Releases pages 1 and 2 (read 2026-10-08), `CHANGELOG.md` at `4843fc2021` (SHA-256 `1adeea59...8670e`) for 5.4.1 on, and the fix commits of the releases whose notes only say "bug fixes" or are missing | 0.5.2 to 5.7.0 |

Each file's header lines give the full commit and digest, every page read, and the file's own scope notes. Every
scenario was taken from the fix's regression test, read with `git show` from a blobless clone, or, without a test,
from its issue or commit; the issue pages were read with WebFetch.

## What is catalogued

Every entry that fixes a defect or addresses an issue, security fixes included, in every release from the first to
the newest, and entries listed for an unreleased version (`unreleased (1.15.0)`). Features, refactors, performance
work, documentation, build, CI and test-only changes are not, and neither are feature requests or questions closed
by a feature; when an entry mixes a feature and a fix, the fix is catalogued. Where a library has a change log, only
its entries are catalogued; where the notes are missing or say only "bug fixes" (rust-csv, Go, Papa Parse), the fix
commits are read and each is catalogued under the first release that contains it. A fix listed twice (csv-parse's
6.0.0 notes repeat 5.x) is catalogued once.

A fix becomes one row, or one row per case when its test has several distinct inputs; adjacent rows with the same
version, reference and summary are one fix, numbered `1`, `2`, and so on.

## Row format

The columns are `library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason`, in
canonical RFC 4180 quoting, one row per line, every character printable ASCII. `input` and `expected` use the
escapes `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and `\u{H...}` (the UTF-8 encoding of a scalar value); a
decoded field is a byte sequence, read as UTF-8.

| Class | Meaning |
|---|---|
| `applies` | The row runs, and Bodu must do what the fix established. |
| `dialect` | The row runs, asserting Bodu's documented behaviour where it deliberately differs; `reason` cites the document. |
| `n/a` | The fix concerns something Bodu does not have or cannot be affected by; `reason` names it. |
| `unknown` | No scenario could be found; `reason` links what was read. |

| Kind | The row passes when |
|---|---|
| `parse` | reading `input` succeeds and its rendering equals `expected` |
| `reject` | reading `input` throws `DelimitedFormatException` |
| `write` | writing the records described by `input` produces exactly the bytes of `expected` |
| `write-reject` | writing the records described by `input` throws the exception named in `expected` |
| `roundtrip` | reading `input`, writing the records back and reading the result renders the same twice (and the written bytes equal `expected` when it is given) |
| `unit` | (not run as data) `input` names the test that covers the scenario, `<TestClass>.<Member>_When<Condition>_Should<Result>`, and `reason` describes its model, input and outcome |

`expectation` says where `expected` comes from: `upstream` (the fix's own test), `spec` (RFC 4180), or `derived`
(worked out, with the working in `reason`).

### Options

`options` is `Name=Value` pairs separated by `;`. The field is split on `;` first, then each pair at its first `=`,
and each value is decoded with the escapes above, so a semicolon in a value is written `\x3B`.

| Option | Maps to | Values |
|---|---|---|
| `Delimiter`, `Quote`, `CommentChar` | the reader's and the writer's option of that name | one character; `\0` selects the default |
| `NoHeader` | the reader's and the writer's `NoHeader` | `true`, `false` |
| `TrimFields`, `AllowComments` | the reader's options of that name | `true`, `false` |
| `FieldCountBehavior`, `MalformedRecordBehavior`, `DuplicateHeaderBehavior` | the reader's policies | the enum member name |
| `View` | what a `parse` row renders | `Records` (the default), `Headers` |

### Rendering

A `parse` row renders the records as compact JSON: an array of records, each record as `Utf8DelimitedReader` frames
it. In header mode (the default) a record is an array of `[name, value]` pairs in field order, the names being the
`PropertyName` tokens and the values the `String` tokens, so duplicate, empty and synthesized names stay visible; with
`NoHeader=true` a record is an array of values. `a,b\n1,2\n` renders as `[[["a","1"],["b","2"]]]`, and with
`NoHeader=true` as `[["a","b"],["1","2"]]`. With `View=Headers` the row renders the reader's `Headers` list after the
whole input is read, as a JSON array of strings.

JSON strings escape only `"`, `\` and the C0 controls (`\b`, `\f`, `\n`, `\r`, `\t`, others as `\u00XX` with
uppercase hex); all other text, non-ASCII included, is itself. The rendering is then written in the catalogue's
escapes, so a value holding a line feed appears as `\\n` in `expected`, and U+00E9 as `\u{E9}`.

### Write notation

A `write` or `write-reject` row's `input` is the same JSON shape. With headers, each record is an array of
`[name, value]` pairs written as an object record (`WriteStartObject`, `WritePropertyName` and `WriteString` per
pair, `WriteEndObject`), the writer emitting the header row from the first record; with `NoHeader=true`, each record
is an array of values written as a positional record. A JSON `null` is passed to the writer as `null`. The writer
gets the row's `Delimiter`, `Quote`, `CommentChar` and `NoHeader`, and `expected` is the exact bytes, which end every
record with CRLF.

### Round trip

A `roundtrip` row reads `input` with the row's reader options, writes the records it read with the write notation's
calls and the row's `Delimiter`, `Quote`, `CommentChar` and `NoHeader`, and reads the written bytes again with the
same reader options. The two renderings must be equal.

## Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown | Runnable | Pass | Fail |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| commons-csv | 116 | 128 | 35 | 4 | 89 | 0 | 34 | 34 | 0 |
| cpython-csv | 66 | 76 | 16 | 2 | 58 | 0 | 17 | 17 | 0 |
| go-csv | 12 | 19 | 10 | 7 | 2 | 0 | 11 | 11 | 0 |
| rust-csv | 34 | 40 | 17 | 2 | 21 | 0 | 14 | 14 | 0 |
| csvhelper | 208 | 214 | 45 | 3 | 166 | 0 | 26 | 26 | 0 |
| sylvan-csv | 57 | 71 | 40 | 6 | 25 | 0 | 33 | 33 | 0 |
| sep | 16 | 20 | 10 | 1 | 9 | 0 | 7 | 7 | 0 |
| csv-parse | 101 | 108 | 30 | 6 | 72 | 0 | 29 | 29 | 0 |
| papaparse | 106 | 113 | 32 | 4 | 71 | 6 | 31 | 31 | 0 |
| **Total** | **716** | **789** | **235** | **35** | **513** | **6** | **202** | **202** | **0** |

Runnable rows are the `applies` and `dialect` rows of every kind but `unit`; the other 68 `applies` and `dialect` rows
are `unit` rows, each naming a test in `Bodu.Text.Delimited.Test`, and every one of those tests passes too. Pass and
Fail count the runnable rows with the fixes below in place. The `n/a` rows are dominated by API surface Bodu does not
offer (class maps, type converters, data readers, callbacks, network and browser streaming), the libraries' languages
and platforms (Python 2, JavaScript prototypes, TypeScript declarations, Rust memory safety), packaging and performance.

## What the catalogue found

The first run, against Bodu at `0e0e990182`, failed 25 runnable rows, and 12 of the 66 `unit` tests failed when they
were written. Each cause became an issue, fixed test first:

- **#846. Text after a closing quote.** The reader ended the record at a closing quote and read the rest of the line as
  a new record, so `"a"b,c` gave `[a]` and `[b,c]` and `SkipRecord` never saw an error, although
  `docs/docs/formats/parser-policies.md` documents such text as a structural error. Only the delimiter, a line break or
  the end of the input may now follow a closing quote, after spaces and tabs under `TrimFields`; anything else makes the
  record malformed. `Throw` reports the offending byte, and `SkipRecord` skips the whole record and goes on with the
  next line, where `parser-policies.md` had said that it truncates the record. Rows: commons-csv CSV-147 cases 1 and 2
  and PR #303 case 1, csv-parse 1.2.2 `e28757c351` and 2.1.0 #214, and sylvan-csv 0.4.2 cases 1 and 3.
- **#847. Fields the writer left bare.** The writer wrote a record of one empty field as an empty line, which the reader
  skips; a first field that begins with the comment character bare, so that a reader allowing comments skipped the
  record; and a field with leading or trailing spaces bare, so that a trimming reader trimmed them; yet its remarks
  promise that its output round-trips. It now quotes all three, and `DelimitedWriterOptions` has a `CommentChar`, `#` by
  default, which `DelimitedSerializerOptions.CommentChar` sets for the writer and the reader alike. Rows: commons-csv
  PR #629, PR #610 case 2 and PR #621 case 2, cpython-csv #115712 case 3 and bpo-32255 case 1, rust-csv `920684f32d`
  case 1 and #283 case 2, csvhelper `179c1a853a` case 2, and sep PR #17 case 2.
- **#848. Trimming around quotes.** Under `TrimFields` the reader looked for an opening quote before trimming, so
  `1, 2, "test"` kept the quotes of `"test"`. Spaces and tabs before an opening quote and after a closing one are now
  skipped, so the field is read as quoted and the text between its quotes is kept whole. Rows: csvhelper 28.0.0 #1954,
  and csv-parse 2.4.0 `40352e8272` case 1 and `67fd873923`.
- **#849. Dialect characters.** The reader and the writer took any `char` as the delimiter, quote or comment character
  and cut it to one byte, so U+00A3 matched the second byte of its own UTF-8 encoding, a line-feed delimiter merged
  records, and a delimiter equal to the quote or to the comment character misread records. Their constructors, and the
  serializer and DOM methods that create them, now throw `ArgumentException` unless each of the three is an ASCII
  character other than CR and LF and no two are the same. Rows: the `unit` tests of cpython-csv #113796, go-csv #22404
  and go-csv `2cc15b18db`.
- **#850. Where a record's error is reported.** A field-count error was reported at the line and offset after the
  record, and a duplicate header name at those after the header row. Both now report where the record starts (csv-parse
  1.1.1 case 2).
- **#851 and #852. The streaming deserializer.** `DeserializeAsyncEnumerableAsync` held back a record whose line ending
  was the last byte read until more input arrived, and counted its errors' positions from the start of each read, or
  gave none. It now yields a record as soon as its LF or complete CRLF is read (csv-parse 1.1.2), and reports every
  error at its line and offset in the whole stream (papaparse 4.5.0 PR #509).
- **#853 to #856. The serializer.** A field of white space binds `null` to a nullable value-type member, as an empty
  field does, except a `char?` member, which reads a single space as a space (csvhelper 1.1.1, sylvan-csv 0.8.3).
  `DateTime` and `DateTimeOffset` are written with the round-trip `O` format, and `DateOnly`, `TimeOnly` and `TimeSpan`
  in invariant round-trip forms, so that every tick and the kind or offset survive (papaparse 5.5.5). An empty
  collection writes the header row alone in header mode, from every `Serialize` and `SerializeAsync` overload (csvhelper
  2.8.3). And a member hidden with `new` maps to one column, its most derived declaration (csvhelper 12.2.2).

Fixing these turned up three more defects, each fixed the same way. The streaming deserializer dropped a U+FEFF that
started a record in a later buffered segment, as if it were a byte-order mark (#912). The source generator's delimited
factories kept the general temporal forms and the empty-only `null` rule that #853 and #854 had replaced in the
reflection binder (#913). And the writer's API overview showed LF line endings where the writer writes CRLF (#914).

The fixes moved eleven rows between classes. Six became `dialect` rows, citing the documents the fixes wrote. go-csv
1.10 #19410 cases 2 and 3, where Go matches a non-ASCII delimiter or comment character as a whole rune, are now `unit`
rows naming `Utf8DelimitedReaderTests.Ctor_WhenTheDelimiterOrCommentCharIsNotAscii_ShouldThrowArgumentException`
(`parser-policies.md` lines 22 and 27). csv-parse 7.0.0 #482 cases 1 to 3, which trim U+3000, U+00A0 and U+000B, assert
that `TrimFields` keeps them (`DelimitedReaderOptions.cs` line 73 and `parser-policies.md` line 24). And go-csv 1.4
`6ad2749dcd` case 2, where Go writes a lone empty field as an empty line, expects `""` (`Utf8DelimitedWriter.cs` lines
28 and 32). Five `dialect` rows became `applies` rows asserting the upstream result, which Bodu now produces: csv-parse
2.4.0 `40352e8272` case 2 and 4.2.0 `4faa65ef98`, formerly `reject` rows, read the white space after a closing quote as
trimmed (#846); and commons-csv PR #610 case 1, now with the comment character `;`, csvhelper 3.0.0 `179c1a853a` case 1
and rust-csv 1.3.0 #283 case 1 expect a first field that begins with the comment character to be quoted (#847).

### Documentation that disagrees with the behaviour or with itself

The first run listed seven places where the documentation disagreed with the behaviour or with itself. The fixes above
corrected six of them, and #911 the seventh: `DelimitedDuplicateHeaderBehavior.TakeFirst` said that later columns with a
duplicated name are unnamed, though the reader keeps every column's name; it is `TakeLast` that reports the earlier
columns under an empty name.

Three behaviours that `dialect` rows rely on are not in the reader's public documentation. A lone CR ends a line, which
only the private `Utf8DelimitedReader.SkipLineEnding` documents. Blank lines are skipped, although RFC 4180's grammar
reads an empty line as a record of one empty field: the reader documents that only in private XML documentation
(`LoadRecord` and `SkipBlankAndCommentLines`, `Utf8DelimitedReader.cs` lines 357 and 560), and the writer's remarks
mention it (`Utf8DelimitedWriter.cs` lines 32 and 35). And under `Ragged` an extra field is named by its zero-based
index, a name that can collide with a real header, which nothing documents. The `dialect` rows that rely on them are
go-csv 1.10 #22937 case 3 and papaparse 4.1.3 (a lone CR), sep 0.2.0 #10 case 3 and sylvan-csv 1.1.16 (blank lines), and
csv-parse 4.0.1 (the extra field's name).

## Ports and shared lineage

None of the nine libraries is a port of another, so every catalogue is independent evidence. Sylvan.Data.Csv's 1.3.2
notes credit Sep for the ideas behind its vectorised parser, which is shared technique, not shared code. Ports of
these libraries (Papa Parse's Baby Parse, for one) are not catalogued.

## Validation

The harness is `DelimitedReleaseNoteCorpusTests`, in `Bodu.Text.Delimited/test/Text.Delimited/`, over copies of these
files embedded from `Bodu.Text.Delimited/test/Fixtures/ReleaseNotes/`. Its governance tests, in the BVT tier, check the
files themselves (column count, classes, kinds, option names, escapes, case numbering, canonical quoting, a reason on
every row, ASCII only), pin the counts above, and check that each `unit` row names a test in the test assembly and that
the copies match these files byte for byte. One Regression test per kind runs the runnable rows, each named after its
library, version, reference, case and summary, so `test.runsettings` runs them and `bvt.runsettings` does not.
