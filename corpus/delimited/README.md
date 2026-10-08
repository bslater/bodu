# Delimited release-note fix catalogue

`fixes/` holds one file per CSV library, `fixes/<library>-fixes.csv`: every defect fix the library's release notes
list (or, where the notes are missing or incomplete, its fix commits), turned into a row that states the scenario
of the fix and what `Bodu.Text.Delimited` must do with it. A row that can run is a parse, reject, write or round-trip
case against `Utf8DelimitedReader` and `Utf8DelimitedWriter`; a scenario that needs typed code (a record type, a
stream that returns a few bytes at a time) is a `unit` row naming the test to write; a fix that cannot touch Bodu
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
| `unit` | (not run here) `input` names the test to write, `<TestClass>.<Member>_When<Condition>_Should<Result>`, and `reason` describes its model, input and outcome |

`expectation` says where `expected` comes from: `upstream` (the fix's own test), `spec` (RFC 4180), or `derived`
(worked out, with the working in `reason`).

### Options

`options` is `Name=Value` pairs separated by `;`. The field is split on `;` first, then each pair at its first `=`,
and each value is decoded with the escapes above, so a semicolon in a value is written `\x3B`.

| Option | Maps to | Values |
|---|---|---|
| `Delimiter`, `Quote`, `CommentChar` | the reader's and the writer's option of that name (`CommentChar` the reader's only) | one character; `\0` selects the default |
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
gets the row's `Delimiter`, `Quote` and `NoHeader`, and `expected` is the exact bytes, which end every record with
CRLF.

### Round trip

A `roundtrip` row reads `input` with the row's reader options, writes the records it read with the write notation's
calls and the row's `Delimiter`, `Quote` and `NoHeader`, and reads the written bytes again with the same reader
options. The two renderings must be equal.

## Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown | Runnable | Pass | Fail |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| commons-csv | 116 | 128 | 34 | 5 | 89 | 0 | 34 | 29 | 5 |
| cpython-csv | 66 | 76 | 16 | 2 | 58 | 0 | 17 | 15 | 2 |
| go-csv | 12 | 19 | 13 | 4 | 2 | 0 | 13 | 11 | 2 |
| rust-csv | 34 | 40 | 16 | 3 | 21 | 0 | 14 | 12 | 2 |
| csvhelper | 208 | 214 | 44 | 4 | 166 | 0 | 26 | 24 | 2 |
| sylvan-csv | 57 | 71 | 40 | 6 | 25 | 0 | 33 | 31 | 2 |
| sep | 16 | 20 | 10 | 1 | 9 | 0 | 7 | 6 | 1 |
| csv-parse | 101 | 108 | 31 | 5 | 72 | 0 | 29 | 20 | 9 |
| papaparse | 106 | 113 | 32 | 4 | 71 | 6 | 31 | 31 | 0 |
| **Total** | **716** | **789** | **236** | **34** | **513** | **6** | **204** | **179** | **25** |

Runnable rows are the `applies` and `dialect` rows of every kind but `unit`; the other 66 `applies` and `dialect`
rows are `unit` rows, whose tests are still to be written. The counts are those of Bodu at `0e0e990182`. The `n/a`
rows are dominated by API surface Bodu does not offer (class maps, type converters, data readers, callbacks, network
and browser streaming), the libraries' languages and platforms (Python 2, JavaScript prototypes, TypeScript
declarations, Rust memory safety), packaging and performance.

## What the catalogue found

The 25 failing rows have five causes. Each is reported, not reclassified: a failing `applies` row is a scenario the
fix established that Bodu does not meet, and a failing `dialect` row is one where Bodu does not do what its own
documentation says.

1. **Text after a closing quote starts a new record** (8 rows). `docs/docs/formats/parser-policies.md` (line 29)
   documents characters after a closing quote as a structural error, subject to `MalformedRecordBehavior`; the reader
   instead ends the record there and reads the rest of the line as a new one, so `"a"b,c` gives `[a]` and `[b,c]`,
   and `SkipRecord` never sees the error. Rows: commons-csv CSV-147 case 1 and PR #303 case 1; csv-parse 1.2.2
   `e28757c351`, 2.1.0 #214, 2.4.0 `40352e8272` case 2 and 4.2.0 `4faa65ef98`; sylvan-csv 0.4.2 cases 1 and 3. With
   `TrimFields`, white space after a closing quote splits the line into several records, some of them empty.
2. **A record of one empty field does not survive a round trip** (5 rows). The writer writes it as an empty line,
   which the reader skips, although the writer's remarks (`Utf8DelimitedWriter.cs` lines 25 and 26) promise that
   its output round-trips. Rows: commons-csv PR #629, cpython-csv #115712 case 3 and bpo-32255 case 1, rust-csv
   `920684f32d` case 1, sep PR #17 case 2. Commons CSV, CPython and rust-csv quote the field (`""`) for exactly
   this reason; Sep instead reads an empty line back as a row of one empty field.
3. **A value that starts with the comment character is written bare** (3 rows). Read back with `AllowComments`, the
   record becomes a comment line and disappears. The writer has no comment setting, so it cannot know; the same
   round-trip promise applies. Rows: commons-csv PR #610 case 2, csvhelper `179c1a853a` case 2, rust-csv #283 case 2.
4. **Trimming** (7 rows). `TrimFields` is documented as trimming "surrounding whitespace" from unquoted fields
   (`DelimitedReaderOptions.cs` line 45) but trims only space and tab (`Utf8DelimitedReader.cs` lines 393 to 395),
   so U+3000, U+00A0 and U+000B stay (csv-parse 7.0.0 #482 cases 1 to 3). A field whose quote follows white space is
   read as unquoted, so its quotes stay in the value (csvhelper 28.0.0 #1954, csv-parse 2.4.0 `40352e8272` case 1 and
   `67fd873923`); no document says which reading applies. And the writer does not quote leading or trailing spaces,
   so a trimming reader loses them on a round trip (commons-csv PR #621 case 2).
5. **A non-ASCII delimiter or comment character is cast to one byte** (2 rows). `parser-policies.md` (line 22)
   allows any character for `Delimiter` and `Quote`, but the options are narrowed to a byte without validation, so
   `U+00A3` matches the second byte of its own UTF-8 encoding and splits it (go-csv 1.10 #19410 cases 2 and 3).

### Documentation that disagrees with the behaviour or with itself

- `docs/docs/formats/parser-policies.md` line 29: text after a closing quote is a structural error; the reader ends
  the record (cause 1).
- `docs/docs/formats/parser-policies.md` line 20 calls `SkipRecord` "truncate the record at the structural error",
  while `DelimitedMalformedRecordBehavior.cs` line 20 says "A malformed record is silently skipped".
- `DelimitedDuplicateHeaderBehavior.cs` line 20: under `TakeFirst` later columns with the name are "unnamed", but
  they keep it (`a,b,a` reads `[a,1],[b,2],[a,3]`); it is `TakeLast` that blanks the earlier name.
- `Utf8DelimitedWriter.cs` lines 25 and 26: the output round-trips through the reader, except for causes 2 and 3
  and the trimming case of 4.
- `docs/docs/formats/parser-policies.md` line 22: any character for `Delimiter` and `Quote` (cause 5).
- `DelimitedReaderOptions.cs` line 45: `TrimFields` trims "surrounding whitespace", only space and tab in fact.
- `docs/guides/formats/delimited.md` line 43: `Serialize` takes the header row "from the record type", but an empty
  collection writes nothing, not even the header (csvhelper 2.8.3 `unit` row).

Two behaviours the rows rely on are documented only in private XML documentation: a lone CR ends a line
(`Utf8DelimitedReader.SkipLineEnding`), and blank lines are skipped (`LoadRecord` and `SkipBlankAndCommentLines`,
`Utf8DelimitedReader.cs` lines 295 and 445), although RFC 4180's grammar reads an empty line as a record of one
empty field. A third is not documented at all: under `Ragged` an extra field is named by its zero-based index, a
name that can collide with a real header. The `dialect` rows that rely on them are go-csv 1.10 #22937 case 3 and
papaparse 4.1.3 (a lone CR), sep 0.2.0 #10 case 3 and sylvan-csv 1.1.16 (blank lines), and csv-parse 4.0.1 (the
extra field's name).

### `unit` rows likely to fail when written

These tests are not run here, but reading Bodu's source suggests they will fail:

- Option validation: cpython-csv #113796 and go-csv #22404 and `2cc15b18db` expect a delimiter equal to the quote,
  a line break, or a delimiter equal to the comment character to be rejected; Bodu validates none of them.
- Serializer conversions: white space in a nullable column is not read as null (csvhelper 1.1.1, sylvan-csv 0.8.3);
  a `DateTime` is written with the invariant general format, which drops fractional seconds and the kind
  (papaparse 5.5.5); an empty collection writes no header (csvhelper 2.8.3); a property hidden with `new` may map
  twice (csvhelper 12.2.2).
- The streaming deserializer parses each read from its own start, so an error after the first 16 KiB may report a
  line number relative to the read (papaparse 4.5.0 PR #509).

## Ports and shared lineage

None of the nine libraries is a port of another, so every catalogue is independent evidence. Sylvan.Data.Csv's 1.3.2
notes credit Sep for the ideas behind its vectorised parser, which is shared technique, not shared code. Ports of
these libraries (Papa Parse's Baby Parse, for one) are not catalogued.

## Validation

Every runnable row was run against Bodu at the commit above with a scratch harness outside the repository, which also
checks the files themselves (column count, classes, kinds, option names, escapes, case numbering, a reason on every
row, ASCII only). Its `Renderer.cs` holds the file reader, the escape decoder, the option mapping, the rendering and
the row runner, depends only on Bodu and the BCL, and is meant to be ported into `Bodu.Text.Delimited.Test`, so that
the catalogue runs as data-driven tests, each named after its library, version, reference, case and summary.
