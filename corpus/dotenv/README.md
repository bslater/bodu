# Bodu DotEnv validation corpus

External evidence used to validate `Bodu.Text.DotEnv`, kept in the repository so every reconciliation is reproducible
from committed artifacts. Today it holds one catalogue, `fixes/`.

## There is no dotenv specification

`.env` files have no standard. Every library documents, or only implements, its own dialect, and they disagree on
comments, quoting, escapes, whitespace, key names and multi-line values. Bodu documents its dialect in the guide
(`docs/guides/formats/dotenv.md`), the parser policies (`docs/docs/formats/parser-policies.md`), the package overview
(`docs/docs/formats/dotenv/index.md`) and the XML documentation of `Utf8DotEnvReader`, `DotEnvReaderOptions`,
`Utf8DotEnvWriter` and `DotEnvFormatException`. Those documents are the only authority a row may cite for the `dialect`
class: where Bodu differs from a library and no document states Bodu's behaviour, the row keeps the library's
expectation and is reported when it fails.

## `fixes/` - the release-note fix catalogue

The release notes of eight dotenv libraries were read in full, from the first release to the newest, and every fix
they list is catalogued (for compose-go, every fix to its `dotenv` package): one file per library,
`fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. Where the notes are missing
or incomplete, the commit history was read as well, and the file's header says so; a fix found only there says "from
the commit history" in its summary. Each row records the version, the reference (an issue, a pull request or a
commit) and a summary, then a class:

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 261 | The row carries the scenario of the fix and runs: Bodu must do what the fix established |
| `dialect` | 40 | The row runs, asserting Bodu's documented behaviour where it differs from the fix's; `reason` cites the document |
| `n/a` | 205 | The fix concerns something Bodu does not have (variable expansion, loading into the process environment, overriding existing variables, file discovery and loading, a CLI), packaging, performance, or the library's language; `reason` says which |
| `unknown` | 0 | No scenario could be found; `reason` would link what was read |

That is 314 fixes in 506 rows. A scenario is the regression test the fix added or changed, read at the fix commit and
restated as a short input with the test named in `reason`; where the fix added no test, it is the scenario its issue or
commit describes, the expectation is `derived`, and `reason` shows the working.

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| python-dotenv | 71 | 109 | 53 | 3 | 53 | 0 |
| dotenv-node | 45 | 67 | 25 | 11 | 31 | 0 |
| godotenv | 27 | 68 | 51 | 9 | 8 | 0 |
| dotenvy | 19 | 27 | 13 | 0 | 14 | 0 |
| dotnetenv | 13 | 37 | 28 | 2 | 7 | 0 |
| phpdotenv | 56 | 82 | 42 | 4 | 36 | 0 |
| dotenv-ruby | 59 | 68 | 20 | 1 | 47 | 0 |
| compose-go | 24 | 48 | 29 | 10 | 9 | 0 |
| **Total** | **314** | **506** | **261** | **40** | **205** | **0** |

### Sources

| File | Library | Licence | What was read |
|---|---|---|---|
| `python-dotenv-fixes.csv` | [theskumar/python-dotenv](https://github.com/theskumar/python-dotenv) (Python) | BSD-3-Clause | `CHANGELOG.md` at `0b28805917`, Unreleased to 0.4.0; the commit history to v0.10.2, for the releases without notes and the fixes the 0.6.0 to 0.10.2 notes omit |
| `dotenv-node-fixes.csv` | [motdotla/dotenv](https://github.com/motdotla/dotenv) (JavaScript, the npm package) | BSD-2-Clause | `CHANGELOG.md` at `5d7cc39e46`, 18.0.6 to 1.0.0; the commit history for 0.0.1 to 0.4.0 and 1.0.0 |
| `godotenv-fixes.csv` | [joho/godotenv](https://github.com/joho/godotenv) (Go) | MIT | GitHub Releases, pages 1 and 2 and the pages of v1.3.0 to v1.6.0-pre.4 (read 2026-10-08); the commit history at `97a2850142`, as v1's notes are empty and v1.1, v1.2.0 and v1.4.0 name only some of their pull requests |
| `dotenvy-fixes.csv` | [allan2/dotenvy](https://github.com/allan2/dotenvy) (Rust) | MIT | `CHANGELOG.md` at `cf125ddeb2`, Unreleased to 0.14.0; the commit history for the dotenv crate's releases before 0.14.0 and the fixes later notes omit |
| `dotnetenv-fixes.csv` | [tonerdo/dotnet-env](https://github.com/tonerdo/dotnet-env) (C#, the NuGet package DotNetEnv) | MIT | GitHub Releases, pages 1 and 2 and the v2.1.0, v3.0.0 and v3.2.0 pages (read 2026-10-08); the commit history at `09e217511b`, as the notes name almost no pull requests |
| `phpdotenv-fixes.csv` | [vlucas/phpdotenv](https://github.com/vlucas/phpdotenv) (PHP) | BSD-3-Clause | GitHub Releases, pages 1 to 7 (read 2026-10-08), V2.2.1 to v5.7.0; the commit history at `301c07936b` for v1.0.0 to v2.2.0, which have no notes |
| `dotenv-ruby-fixes.csv` | [bkeepers/dotenv](https://github.com/bkeepers/dotenv) (Ruby, the gem) | MIT | `Changelog.md` at `2840d9c408`, 3.1.7 to 0.3.0; GitHub Releases page 1 (read 2026-10-08) for 3.1.8 and 3.2.0; the commit history for 0.1.0 to 0.2.0 |
| `compose-go-fixes.csv` | [compose-spec/compose-go](https://github.com/compose-spec/compose-go) (Go), its `dotenv` package | Apache-2.0 AND MIT | The history of `dotenv/` at `32d8d5d602`, from its import to v2.8.0, with the GitHub release pages of every release that ships a fix to it (read 2026-10-08) |

Each file's header gives the full commit, the SHA-256 of the changelog it read, the pages and the date, and, in its
`note` lines, what was left out and why. No upstream file is committed: the rows restate short inputs and expected
values from the libraries' tests, with attribution. All eight libraries are permissively licensed (BSD, MIT,
Apache-2.0), so the inputs may restate their tests; compose-go's repository is Apache-2.0, and its `dotenv` package
keeps godotenv's MIT licence.

The two npm and gem packages named dotenv are filed as `dotenv-node` and `dotenv-ruby`. Where an entry is repeated
across a library's maintenance lines (phpdotenv's 2.x to 4.x), it is catalogued once, under the newest release that
lists it.

### Lineage

Some of these libraries descend from others, so their agreement is not independent evidence, and no fix is counted
twice:

- **dotenvy** is a 2022 fork of dotenv-rs (the `dotenv` crate, itself continued from slapresta/rust-dotenv). Its
  repository keeps the whole history, so its file covers the lineage once; references from before the fork are
  commits, because pull request numbers there belong to the earlier repositories.
- **compose-go**'s `dotenv` package is a fork of godotenv, imported on 2022-01-03 (`376d88f`, first released in
  v1.0.9). Its file catalogues only the fixes made after the import; everything before is in `godotenv-fixes.csv`.
- **godotenv** is a Go port of dotenv-ruby, and its parsing tests quote dotenv-ruby's parser spec, so the two agree on
  many scenarios by descent. phpdotenv describes itself as a PHP version of dotenv-ruby, but it is a separate
  implementation, as are python-dotenv, dotenv-node and DotNetEnv.
- Bodu's own conformance corpus (`Utf8DotEnvReaderTests.DotEnvCorpus.cs`) draws on python-dotenv and godotenv, so
  rows from those two can overlap it.

### One schema

Every file has the header lines `# library:`, `# releases:`, `# licence:` and any number of `# note:` lines, then the
columns `library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason`. The files are
printable ASCII with LF line endings and RFC 4180 quoting; no field holds a raw line break.

| Kind | Rows | What runs |
|---|---:|---|
| `parse` | 209 | Read `input` with `Utf8DotEnvReader`; the rendering of the entries equals `expected` |
| `reject` | 63 | Reading `input` throws `DotEnvFormatException` |
| `write` | 3 | Write the entries `input` describes with `Utf8DotEnvWriter`; the bytes equal `expected` |
| `write-reject` | 0 | Writing the entries `input` describes throws the exception `expected` names |
| `roundtrip` | 19 | Read `input`, write the entries back, and read that again; the two renderings agree, and the written text equals `expected` when it is not empty |
| `unit` | 7 | A typed test still to be written: `input` names it (`<TestClass>.<Method>`), and `reason` gives the model, the input and the outcome |

The **rendering** of a reading is a compact JSON array of the entries in source order: `["KEY","value"]`, or
`["KEY","value","export"]` when the line carried the `export` prefix, with no comments. `export A=1` then `B="x y"`
renders as `[["A","1","export"],["B","x y"]]`. Strings are escaped as JSON: `"` and `\` with a backslash, U+0008,
U+0009, U+000A, U+000C and U+000D as `\b`, `\t`, `\n`, `\f` and `\r`, any other character below U+0020 as `\u`
followed by four uppercase hex digits, and everything else as itself. The **write notation** is the same array, written
between `WriteStartObject` and `WriteEndObject`: each entry with `WritePropertyName(key, true)` when it carries
`"export"`, otherwise with `WritePropertyName(key)` (which applies `WriteExportPrefix`), then `WriteString(value)`.

`input` and `expected` are byte strings written with escapes: `\n` LF, `\r` CR, `\t` tab, `\0` NUL, `\\` a backslash,
`\xHH` the single byte 0xHH, and `\u{H...}` the UTF-8 encoding of the scalar U+H...; any other backslash sequence is an
error. `expectation` says where `expected` comes from: `upstream` (the fix's own test, 245 rows) or `derived` (worked
out in `reason`, 56 rows).

`options` is empty for the defaults, or `Name=Value` pairs separated by `;`. No row needs one today, but the names are
fixed:

| Option | Maps to | Values |
|---|---|---|
| `DisallowExportPrefix` | `DotEnvReaderOptions.DisallowExportPrefix` | `true` / `false` |
| `DisallowInlineComments` | `DotEnvReaderOptions.DisallowInlineComments` | `true` / `false` |
| `SkipComments` | `DotEnvReaderOptions.SkipComments` | `true` / `false` |
| `WriteExportPrefix` | `DotEnvWriterOptions.WriteExportPrefix` | `true` / `false` |

No test in the repository reads these files yet. Every runnable row was run against Bodu at `0e0e990182` by a scratch
harness that renders, writes and decodes exactly as described above, with a ten-second timeout per row; a test that
runs them should port that renderer.

### What the tables found

Of the 294 runnable rows, 269 pass and 25 fail. One cause is a defect against Bodu's own documentation; the other three
are behaviours no Bodu document states, where the libraries disagree with Bodu and, often, with each other. Each is to
be fixed or documented test first, the failing rows left as they are until then.

- **`KEY= # comment` reads the comment as the value** (8 rows). `DotEnvReaderOptions.DisallowInlineComments` says
  that a `#` preceded by whitespace ends an unquoted value as an inline comment (`DotEnvReaderOptions.cs:36`), and the
  parser policies that unquoted values end at an inline `#` comment (`docs/docs/formats/parser-policies.md:36`), so
  the value is empty, as python-dotenv 1.2.4 (PR #663, cases 1, 2, 7 and 8), dotenvy 0.10.0 (`057821523f`, case 3),
  DotNetEnv v3.0.0 (PR #82, case 2) and phpdotenv v2.5.1 (PR #277, case 2) and v2.5.0 (PR #272, case 1) expect.
  `ReadUnquoted` trims the whitespace after `=` before it looks for the comment, and its scan starts one byte past the
  value's first character (`Utf8DotEnvReader.cs:571-586`), so a `#` that opens the value is never taken for one.
- **Text after a closing quote is dropped without an error** (5 rows). `a='b',c` reads as `b` and `EV_DNE="a"b"` as
  `a`, where python-dotenv 0.10.5 (PR #222, case 2) and DotNetEnv v3.0.0 (PR #82, cases 13 and 14) reject the line, and
  dotenv-node 18.0.2 (PR #1056, cases 6 and 9) keeps the text in the value (`TOKEN="abc" oops` reads as `"abc" oops`,
  `KEY="a" "b"` as `a" "b`). Bodu's corpus test pins the drop ("trailing junk after quoted value dropped"), but no
  document states it. compose-go v2.0.0's `dialect` row (PR #511) also rests on it.
- **Only space and tab are whitespace** (4 rows). dotenv-node 18.0.2 (PR #1056, cases 3, 4, 5 and 13) treats form
  feed, vertical tab and U+00A0 around keys, `=` and values as whitespace; Bodu rejects them as key characters or
  malformed entries. No document says which characters are whitespace.
- **The escape set is not documented** (8 rows). In double quotes Bodu resolves `\"`, `\\`, `\n`, `\t`, `\r` and `\$`,
  joins a line ending in a backslash to the next, and keeps any other escape as written. The guide says only that
  double quotes resolve escape sequences (`docs/guides/formats/dotenv.md:63`). godotenv v1.2.0 (PR #34, cases 1 to 3)
  drops the backslash of an unknown escape; compose-go v1.6.0 (PR #308, cases 5 and 10 to 13) keeps it, as Bodu does,
  but resolves the XSI echo escapes `\a`, `\b`, `\f`, `\v` and octal `\0nnn`.

Of the seven `unit` rows, a probe of each scenario suggests that six would pass. The seventh,
`DotEnvDocumentTests.GetProperty_WhenKeyRepeated_ShouldReturnLastValue` (phpdotenv v3.4.0), would fail:
`DotEnvDocument` returns the first of two entries with the same key, while `DotEnvNode` and `DotEnvSerializer` take the
last, and no document says which should win.

Cataloguing also met two places where the documentation describes something the code does not do:

- `DotEnvFormatException.ColumnNumber` is documented as the 1-based column of the error within its line
  (`DotEnvFormatException.cs:20-21`, `:77`), but every error the reader raises passes column 1
  (`Utf8DotEnvReader.cs:688`, `:696`, `:704`, `:712`).
- `DotEnvLimits.MaxEntryLength` is documented as the bound a resumable streaming path applies to an entry
  (`DotEnvLimits.cs:15-20`), but the reader has no streaming path, nothing reads the constant, and its message
  (`Format_Invalid_DotEnvEntryTooLong`) is never used.
