# Bodu TOML release-note fix catalogue

`fixes/` holds, for each of eight TOML libraries, every defect fix its release notes list, each turned into one or
more rows that run against `Bodu.Text.Toml`. A fix one library had to make is a scenario every TOML reader and
writer can get wrong, so each row asks whether Bodu gets it right, or records why the fix cannot apply to Bodu.

There is one file per library, `fixes/<library>-fixes.csv`, and for toml-rs one per crate. Each file's header names
the repository, what was read (a changelog at a commit with its SHA-256, or a GitHub Releases listing with the pages
and the date it was read), the versions covered and the licence, followed by notes on what was and was not
catalogued.

## Sources

| Library | Repository | Language | Read | Versions | Licence |
|---|---|---|---|---|---|
| `tomli` | https://github.com/hukkin/tomli | Python | `CHANGELOG.md` at `43a86ade8b` | 0.1.0 to 2.5.0 | MIT |
| `tomllib` | https://github.com/python/cpython | Python | `Misc/NEWS.d` at `cdfdf7bb73`, and `git log --grep=tomllib` | 3.11.0a7 to 3.15.0b1 | PSF-2.0 |
| `toml` | https://github.com/toml-rs/toml | Rust | `crates/toml/CHANGELOG.md` at `f72038c250`; before 0.5.8, the commit history | 0.1.0 to 1.1.7 | MIT OR Apache-2.0 |
| `toml_edit` | https://github.com/toml-rs/toml | Rust | `crates/toml_edit/CHANGELOG.md` at `f72038c250` | 0.2.0 to 0.25.16 | MIT OR Apache-2.0 |
| `toml_datetime` | https://github.com/toml-rs/toml | Rust | `crates/toml_datetime/CHANGELOG.md` at `f72038c250` | 0.5.0 to 1.1.2 | MIT OR Apache-2.0 |
| `toml_parser` | https://github.com/toml-rs/toml | Rust | `crates/toml_parser/CHANGELOG.md` at `f72038c250` | 1.0.0 to 1.1.4 | MIT OR Apache-2.0 |
| `toml_writer` | https://github.com/toml-rs/toml | Rust | `crates/toml_writer/CHANGELOG.md` at `f72038c250` | 0.1.0 to 1.1.3 | MIT OR Apache-2.0 |
| `serde_spanned` | https://github.com/toml-rs/toml | Rust | `crates/serde_spanned/CHANGELOG.md` at `f72038c250` | 0.6.0 to 1.1.2 | MIT OR Apache-2.0 |
| `burntsushi` | https://github.com/BurntSushi/toml | Go | GitHub Releases, pages 1 and 2; the commit history from v0.1.0 to v0.3.1, which have no notes | v0.1.0 to v1.6.0 | MIT |
| `go-toml` | https://github.com/pelletier/go-toml | Go | GitHub Releases, pages 1 to 6, the v1 and v2 lines | v0.2.1 to v2.4.3 | MIT |
| `tomlyn` | https://github.com/xoofx/Tomlyn | C# | the historical `changelog.md` at `b8b60d2c32`, and GitHub Releases, pages 1 to 6 | 0.1.1 to 2.10.1 | BSD-2-Clause |
| `tomlet` | https://github.com/SamboyCoding/Tomlet | C# | `CHANGELOG.md` at `723cdfd25b` (the project publishes no GitHub Releases) | 1.0.0 to 6.2.0 | MIT |
| `smol-toml` | https://github.com/squirrelchat/smol-toml | JavaScript | GitHub Releases, pages 1 to 3, and the advisories they cite | v1.0.0 to v1.9.0 | BSD-3-Clause |
| `tomlplusplus` | https://github.com/marzer/tomlplusplus | C++ | the Fixes sections of `CHANGELOG.md` at `1e8829b793`, Unreleased included | v0.2.0 to v3.4.0 | MIT |

The GitHub Releases listings were read on 2026-10-08. The SHA-256 of every changelog read is in its file's header.
Each fix's scenario was then found in its commit or pull request, usually as the regression test it added, read from
a blobless clone (`git show`); where a fix added no test, the issue it closes or the commit itself gave the scenario.

No upstream file is committed, and no upstream text is copied beyond a short summary in the catalogue's own words.
Inputs restate a few lines of a permissively licensed regression test, naming it in `reason`, or are written for the
row (`expectation` `derived`, with the working in `reason`). Every library here is permissively licensed.

## What is catalogued

Every entry, in every release, that fixes a defect or addresses an issue, security advisories included. New
features, TOML-version support, performance work, refactors and documentation fixes are not, and neither are CI and
build changes that never reached a published package. Each file's header says what it left out. Three rules decide
the remaining cases:

* **A port and its original are not independent evidence.** `tomllib` is `tomli` vendored into CPython (PEP 680):
  its file holds only the two changes CPython made itself, and the two tomllib entries that are tomli fixes are
  catalogued once, under `tomli`. None of the other libraries is a port of another. go-toml v2 is a rewrite of v1 in
  the same repository and both lines are one file; Tomlyn 1.0 is a redesign and its notes are catalogued like any
  other release's.
* **The toml-rs crates share one parser and writer.** A fix in several crates' changelogs is listed in each crate's
  file, as its notes list it, and counted once below: 36 fixes appear in more than one crate, 48 listings in all.
* **Shared test suites are one lineage.** Many fixes in many libraries are toml-test cases that started to pass. A
  row restating a toml-test case cites it; two libraries fixing the same toml-test case are one piece of evidence.

Unreleased fixes are catalogued only where a changelog lists them (`tomlplusplus`'s Unreleased section, version
`unreleased`). Fix commits that no release note lists yet (go-toml #1111, #1113 and #1114; Tomlyn #135;
tomlplusplus #242, #301 and #302) are not catalogued, and the headers say so. Neither are the fixes listed for a
library's first release (Tomlyn 0.1.0), since no earlier release had the defect.

## Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 716 | The row carries the fix's scenario and runs: Bodu must do what the fix established |
| `dialect` | 55 | The row runs, asserting Bodu's documented behaviour where it deliberately differs; `reason` cites the Bodu document or the TOML specification section |
| `n/a` | 345 | The fix concerns an API Bodu does not have, the library's language or runtime, a platform, packaging, performance only, or exception message text; `reason` says which |
| `unknown` | 3 | No scenario could be found; `reason` names what was read |

That is 797 fix listings, 749 distinct fixes, in 1,119 rows.

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| tomli | 27 | 61 | 47 | 6 | 8 | 0 |
| tomllib | 2 | 2 | 0 | 0 | 2 | 0 |
| toml | 115 | 161 | 119 | 12 | 28 | 2 |
| toml_edit | 104 | 130 | 70 | 9 | 51 | 0 |
| toml_datetime | 8 | 18 | 12 | 3 | 3 | 0 |
| toml_parser | 15 | 19 | 12 | 0 | 7 | 0 |
| toml_writer | 1 | 2 | 2 | 0 | 0 | 0 |
| serde_spanned | 1 | 1 | 0 | 0 | 1 | 0 |
| burntsushi | 64 | 103 | 73 | 4 | 26 | 0 |
| go-toml | 183 | 273 | 192 | 10 | 71 | 0 |
| tomlyn | 72 | 86 | 50 | 4 | 31 | 1 |
| tomlet | 43 | 54 | 43 | 1 | 10 | 0 |
| smol-toml | 34 | 64 | 49 | 4 | 11 | 0 |
| tomlplusplus | 128 | 145 | 47 | 2 | 96 | 0 |
| **Total** | **797** | **1,119** | **716** | **55** | **345** | **3** |

Some kinds of fix are classed the same way throughout:

* **Exception message wording** is `n/a`: the input was rejected before and after the fix. An **error position**
  that Bodu also reports is a `unit` row: `TomlFormatException.LineNumber`, `ColumnNumber` and the byte-true
  `Offset`, and `TomlSerializationException.LineNumber`, `ColumnNumber` and `Path`.
* **Format-preserving editing** (comments, whitespace, the radix or quoting an integer or string was written in) is
  `n/a`: Bodu keeps no trivia and writes one normalized layout. So is a writer's **layout choice** (blank lines,
  indentation, line wrapping) where the fix changes no data.
* **Language specifics** are `n/a`: Go struct tags, embedded structs and pointers; C++ compilers, linkage, memory
  safety and the exceptions-disabled mode; JavaScript numbers beyond 2^53 and Temporal objects; Python versions.
  Where Bodu's serializer has the same concern (a type mismatch, an ignored member, a null value, a recursive model),
  the row is a `unit` row in Bodu's terms.
* **A writer's choice between equivalent spellings** (literal or basic strings, inline or header tables, `+inf` or
  `inf`) is checked as Bodu documents it: `Utf8TomlWriter` writes basic-quoted strings, bare or basic-quoted keys,
  `inf`, `-inf` and `nan`, inline tables only as array elements, and every sub-table under a header. Where the fix is
  about a property of the output (the value survives), the row is a `roundtrip`.

## Row format

Each file is printable ASCII with LF line endings: the header lines, then
`library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason`, quoted as RFC 4180.
`reference` is an issue (`#123`), a pull request (`PR #123`), a commit (ten hex digits) or empty; a security
advisory's id is in the summary. A fix that needs several rows numbers them in `case`; the rows of one fix share its
version, reference and summary.

`input` and `expected` use the escapes `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and `\u{H...}` (a scalar's
UTF-8 bytes); every other character stands for itself. The decoded bytes are what the reader sees, so invalid UTF-8,
a byte order mark or a bare CR is written with `\x`. Tagged JSON that contains a JSON escape doubles its backslash in
the field (`"a\\nb"` in the CSV is the JSON string `a\nb`).

| Kind | `input` | `expected` |
|---|---|---|
| `parse` | the TOML document | its toml-test tagged JSON, compact |
| `reject` | the TOML document | `TomlFormatException` |
| `write` | the value as tagged JSON | the exact text `Utf8TomlWriter` writes |
| `write-reject` | the value as tagged JSON | the exception type the writer throws |
| `roundtrip` | the TOML document | empty, or the exact text written |
| `unit` | `<TestClass>.<MethodName>` of the typed test to write | empty |

| Option | Maps to | Values |
|---|---|---|
| `SpecVersion` | `TomlReaderOptions.SpecVersion` and `TomlDocumentOptions.SpecVersion` | `V1_0` (Bodu's default, used when the option is absent) or `V1_1` |
| `MaxDepth` | the reader, document and writer `MaxDepth` | an integer; no row needs it today |

`TomlWriterOptions.SpecVersion` is not mapped: the writer always emits text valid under both versions
(`TomlSpecVersion.cs` remarks). A fix that only TOML 1.1.0 allows has a `SpecVersion=V1_1` row and, where Bodu's
TOML 1.0.0 default answers differently, a `dialect` row for the default.

`expectation` says where `expected` comes from: `upstream` (the fix's own test), `spec` (TOML 1.0.0 or 1.1.0, or a
toml-test case), `derived` (worked out, with the working in `reason`) or `oracle:tomllib-3.11` (Python 3.11.15's
`tomllib`, a TOML 1.0.0 reader, run over the row's input).

## How the rows run

The rows are meant to run in the test project through one helper that reads these files. It was built and run as a
scratch harness while the catalogue was written; it is described here so it can be ported.

* **`parse`** reads `input` with `TomlDocumentReader`, builds a model of tables, arrays and typed leaves from its
  token stream, and compares it with `expected` semantically, as `TomlTestCorpusTests.AssertMatches` and
  `AssertLeaf` do: integers by value, floats by value with `nan`, `inf` and `-inf` by kind, offset date-times by
  instant, local kinds by value, every fraction truncated to the 100-nanosecond tick Bodu keeps. A `parse` or
  `reject` row was also run through `TomlDocument.Parse` and, at the default version, `TomlNode.Parse`; every surface
  agreed.
* **`reject`** passes when the reader throws exactly `TomlFormatException`; any other exception fails.
* **`write`** builds the tagged JSON with `Utf8TomlWriter` calls (`WriteStartTable`, `WritePropertyName`, the typed
  `Write*` methods, `WriteStartArray`) in the JSON's own order, and compares the bytes written with `expected`.
* **`roundtrip`** reads `input` with `TomlDocument.Parse`, writes `RootElement.WriteTo(Utf8TomlWriter)`, reads that
  again, and compares the two readings; a non-empty `expected` must also equal the text written.
* Each row has a 10-second timeout, so a parser that loops forever fails rather than hangs, and the nesting rows
  (up to 10,001 levels) check that Bodu rejects deep input cleanly instead of overflowing the stack.

Of the 1,119 rows, 690 run this way, and 81 are `unit` rows that describe a typed test. The `n/a` and `unknown`
rows run nothing but keep the record.

## What the catalogue found

**688 of the 690 runnable rows pass.** The two that fail share one cause:

* **A float literal that overflows binary64 is read as infinity.** toml 0.4.2 (`3322bcd086`, case 2) and toml_edit
  0.18.0 (`234025daea`) reject `a = 9e99999`; Bodu reads `+inf`, through `TomlDocumentReader`, `TomlDocument` and
  `TomlNode` alike. The TOML specification does not say what an overflowing float is, and Python's `tomllib` also
  reads `inf`. Bodu's own test `TomlDocumentReaderTests.Read_WhenFloatOverflowsDouble_ShouldYieldPositiveInfinity`
  pins the infinity, but no Bodu document states it, so the rows stay `applies` until one does or Bodu rejects such
  a float.

Four `unit` scenarios were checked by hand against the current build, because they are likely to fail when their
tests are written:

* **A float member silently saturates.** go-toml v1.8.0 (PR #388) errors when `f32 = 1e300` is read into a 32-bit
  float; Bodu's `float` converter returns infinity. `builtin-converters.md` documents saturation for `Half` only.
* **`[Include]` on a private property is ignored.** Tomlyn 2.4.0 (#121) binds `[TomlInclude] private bool
  MyProperty`; with Bodu's `[Include]` the property is neither written nor read, although `IncludeAttribute.cs` says
  the serializer binds through the declared accessors regardless of their visibility.
* **A key redefinition is reported at the value, not the key.** go-toml v2.4.0 (PR #1069) reports
  `a = 1\nb = 2\nb = 3\n` at line 3 column 1; Bodu reports line 3 column 5.
* **`Populate` does nothing for a member that is a plain object.** `TomlSerializerOptions.PreferredObjectCreationHandling`
  documents Populate for collection and dictionary members only, but `ObjectCreationHandlingAttribute` and
  `attributes.md` Pattern 10 describe populating the value already held without that limit, and a get-only
  object-typed member marked Populate silently drops its table. The Tomlyn 2.2.1 row (`2ea74fc`) therefore uses
  settable members.

Every other row passes, and every other difference from a fix is a documented dialect difference: leap seconds,
year 0000, offsets beyond 14 hours, the nesting limit of 64, integers outside the signed 64-bit range, the sign of
`nan`, TOML 1.1.0 syntax under the TOML 1.0.0 default, no integer-to-double conversion in the serializer, and the
writer's single layout.
