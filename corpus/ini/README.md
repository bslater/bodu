# Bodu INI validation corpus

External evidence used to validate `Bodu.Text.Ini`'s reader and writer, kept in the repository so that every row
is reproducible from committed artifacts. INI has no specification: Bodu documents its own dialect
([Parser policies](../../docs/docs/formats/parser-policies.md), section INI), and every library below reads its own.
What this tree holds is therefore evidence, never authority:

| Directory | Source class | What it is |
|---|---|---|
| `fixes/` | `third-party-comparison` | Every fix eight INI libraries have shipped, with its scenario restated in Bodu's dialect |

## `fixes/` - the release-note fix catalogue

The release notes of eight INI readers and writers were read in full, and every fix they list is catalogued: one
file per library, `fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. No
library's notes cover its whole history, so every library's commit history was read as well, for the releases
without notes and for the fixes the notes omit; each file's header says what was read. Each row records the
version, the reference (an issue, a pull request, a commit or a CVE) and a summary, then a class:

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 110 | The row carries the scenario of the fix, in Bodu's dialect, and runs: Bodu must do what the fix established |
| `dialect` | 36 | The row runs, asserting Bodu's documented behaviour where it differs from the fix's; `reason` cites the document |
| `n/a` | 177 | The fix concerns an API or feature Bodu does not have, file loading, or the library's language (memory safety in C, Python 2); `reason` says which |
| `unknown` | 0 | No scenario could be found; `reason` links the issue or commit |

That is 282 fixes in 323 rows. A scenario is, first, the regression test the fix added, read from its commit or
pull request, and otherwise the scenario its issue describes. Of the 146 `applies` and `dialect` rows, 14 are
`unit` rows (a scenario that needs typed code) and 132 run as data.

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| configparser | 64 | 76 | 22 | 7 | 47 | 0 |
| inih | 28 | 36 | 11 | 7 | 18 | 0 |
| go-ini | 69 | 74 | 19 | 7 | 48 | 0 |
| ini-parser | 35 | 37 | 21 | 1 | 15 | 0 |
| npm-ini | 18 | 21 | 8 | 4 | 9 | 0 |
| rust-ini | 26 | 28 | 14 | 4 | 10 | 0 |
| iniparser | 34 | 40 | 10 | 5 | 25 | 0 |
| microsoft-extensions-configuration-ini | 8 | 11 | 5 | 1 | 5 | 0 |
| **Total** | **282** | **323** | **110** | **36** | **177** | **0** |

### Sources

| Library | Repository | What was read | Licence |
|---|---|---|---|
| configparser | [python/cpython](https://github.com/python/cpython) (`Lib/configparser.py`) | `Misc/NEWS.d` (118 release files, 3.5.0a1 to 3.15.0b1, SHA-256 of their concatenation in name order `26d210790da0eb30c0afea8d6c2b39a3e771278e14922403bf5705996a4cdec1`; 1,054 `next/` entries, SHA-256 `71f8f349be2ae3d479450b6a6567cb8524ff288d4d3bd1ff2445774fb97ca2e3`) and `Misc/HISTORY` (0.9.0 to 3.4.6, SHA-256 `a4c1489f93a38989ad0fed9f2faf7dc5c12429c0be633602ff4bb3e018522258`) at main `b22a8c9fd44855d7cc013eb49f689dfd396639a7`; `Misc/NEWS.d` at v2.7.18 `8d21aa21f2cbc6d50aab3f420bb23be1d081dac4` (51 release files, 2.6a1 to 2.7.18, SHA-256 `069795a4cac53d1824fc316fd9bd6d3ca632f74629063d078fe5292eb811a4ce`); `git log -i --grep=configparser` at main; read 2026-10-08 | PSF-2.0 |
| inih | [benhoyt/inih](https://github.com/benhoyt/inih) | GitHub releases, pages 1-4 (r30 to r62), read 2026-10-08; the commit history before r30 (the Google Code era) and after r62, from a blobless clone at `2bbdec4a366c8c39746ee0982e7ca0febbb044b6` | BSD-3-Clause |
| go-ini | [go-ini/ini](https://github.com/go-ini/ini) | GitHub releases, pages 1-3 (v1.51.0 to v1.67.3, 31 of the 108 tags), read 2026-10-08; the whole commit history at `e2db55b0e088fa4ee0c128aa4ade263cdc1d7f08` | Apache-2.0 |
| ini-parser | [rickyah/ini-parser](https://github.com/rickyah/ini-parser) | GitHub releases, pages 1-2 (v2.0.3 to 2.5.2), read 2026-10-08; the commit history at `d300241a75bec4dcabc0751c8690f3b12b3eaf43`; the Google Code archive's ini-parser issues 2-32 | MIT |
| npm-ini | [npm/ini](https://github.com/npm/ini) | `CHANGELOG.md` at `3c96c74fd42584bd655e17a4e63e2ef0a3b406ee` (SHA-256 `6a2b97e6b670d44edcac072a0caa2bd99392a7cec9506ca7ecde04de03d50471`, 3.0.0 to 7.0.0); the commit history of 1.0.0 to 2.0.1 at the same commit; read 2026-10-08 | ISC |
| rust-ini | [zonyitoo/rust-ini](https://github.com/zonyitoo/rust-ini) | GitHub releases, pages 1-2 (v0.14.0 to v0.21.3), read 2026-10-08; the commit history at `a0aa25c2c4626611113c56d31e20874f6f854644`; the crates.io version list (`https://crates.io/api/v1/crates/rust-ini/versions`, read 2026-10-08) for the versions before v0.14.0 | MIT |
| iniparser | [ndevilla/iniparser](https://github.com/ndevilla/iniparser) (a read-only mirror) and [gitlab.com/iniparser/iniparser](https://gitlab.com/iniparser/iniparser) | GitHub releases v4.2 and v4.2.1; the GitLab releases (`https://gitlab.com/api/v4/projects/iniparser%2Finiparser/releases`, SHA-256 `c4a020400c1b99fb35cfdc2884203dcc9812b15eaa150161a4d8d3f916189441`, v4.2 to v4.3.2, none for v4.3.1); both read 2026-10-08; the commit history at `4fff3f22b63819d30ad5c2e4d7c7df9ec272cffc` for v3.1 to v4.1 and v4.3.1, which have no notes | MIT |
| microsoft-extensions-configuration-ini | [dotnet/runtime](https://github.com/dotnet/runtime) (`src/libraries/Microsoft.Extensions.Configuration.Ini`) and the archived [aspnet/Configuration](https://github.com/aspnet/Configuration) | No release notes: the directory's commit history on dotnet/runtime main at `44a22e252219383664c19ce0351333965ac51c6d` (November 2018 on, the dotnet/Extensions history included), and the provider's earlier paths on every branch of aspnet/Configuration at `f64994e0655659faefccead7ccb5c1edbfd4d4ba` (2014 to 2018); read 2026-10-08 | MIT (aspnet/Configuration: Apache-2.0) |

No upstream file is committed. Every library is permissively licensed, so an input may restate a few lines of the
fix's own regression test, which `reason` names; otherwise the input is a minimal reproduction written for the row.

None of the eight is a port of another, so each is independent evidence. Two histories span several repositories,
and each is catalogued once: Microsoft.Extensions.Configuration.Ini moved from aspnet/Configuration through
dotnet/Extensions into dotnet/runtime, and iniparser moved from GitHub to GitLab, its 4.0 rewrite arriving from
the separate iniparser4 repository as one squashed commit.

### File format

Each file starts with `#` header lines (`library`, `releases`, `licence`, and `note` lines on scope and on what was
not catalogued), then the column line
`library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason`, then the rows, with
RFC 4180 quoting. Every character is printable ASCII: in `input`, `expected` and option values, `\n`, `\r`, `\t`,
`\0` and `\\` stand for LF, CR, tab, NUL and a backslash, `\xHH` for one byte, and `\u{H...}` for the UTF-8
encoding of a Unicode scalar. `expectation` says where `expected` comes from: `upstream` (the fix's own test) or
`derived` (worked out, with the working in `reason`): of the 146 `applies` and `dialect` rows, 65 are `upstream`
and 81 `derived`.

### How a row runs

| Kind | Rows | What runs |
|---|---:|---|
| `parse` | 96 | Read `input`; its rendering must equal `expected` |
| `reject` | 13 | Reading `input` must throw; `expected` is the exception's type name, compared exactly (`IniFormatException`) |
| `write` | 0 | Write the triples in `input`; the bytes must equal `expected` |
| `write-reject` | 8 | Writing the triples in `input` must throw; `expected` is the exception's type name, compared exactly |
| `roundtrip` | 15 | Read `input`, write it back, and read the written text: both readings must render the same, and when `expected` is given the written text must equal it |
| `unit` | 14 | `input` names a proposed test method; `reason` describes the model, the input and the outcome |

The renderings, chosen with the `View` option:

- **`Entries`** (the default) is a compact JSON array of `[section, key, value]` triples, in source order, read with
  `Utf8IniReader`, with `""` as the section of the keys before the first header: `a=1\n[s]\nb=2\n` renders as
  `[["","a","1"],["s","b","2"]]`.
- **`Document`** is a compact JSON object of objects read with `IniDocumentReader`, after the duplicate policies.
  The global keys form the object `""`, present only when the document has global keys and always first; the
  sections follow in the order they first appear: `{"":{"a":"1"},"s":{"b":"2"}}`.

JSON strings escape `"` and `\`, write U+0008, U+0009, U+000A, U+000C and U+000D as `\b`, `\t`, `\n`, `\f` and
`\r`, and other control characters and U+007F as `\u00XX`; every other character is written as itself (and then
escaped as `\u{...}` in the file when it is not ASCII).

The write notation is the `Entries` shape: a JSON array of string triples, written with `Utf8IniWriter`, a section
header each time the section changes; the triples of the global section `""` come first and have no header.
A `roundtrip` row in the `Entries` view writes back every token of `Utf8IniReader` in source order (section
headers, comments with the writer's comment prefix, entries); in the `Document` view it writes back the normalized
document (the global entries, then each section's header and entries).

| Option | Maps to | Values |
|---|---|---|
| `DisallowHashComments`, `SkipComments` | `IniReaderOptions` | `true` / `false` |
| `DuplicateSectionBehavior`, `DuplicateKeyBehavior` | `IniDocumentOptions` (needs `View=Document`) | the enum member names |
| `CommentPrefix` | `IniWriterOptions` | one character |
| `View` | the rendering | `Entries` (default), `Document` |

`options` is `Name=Value` pairs separated by `;`, split on `;` first and then each pair at its first `=`; values
use the escapes above, so a semicolon inside a value is `\x3B`.

### Decisions

- **Bodu's dialect wins where it is documented.** A fix that strips quotes or reads escapes inside them, strips an
  inline `;` or `#` comment, or accepts `:` as a delimiter becomes a `dialect` row asserting Bodu's documented
  reading: a value is literal to the end of the line apart from the spaces and tabs around it, which are trimmed,
  quotes are preserved, an inline `;` or `#` is content, and `=` is the only delimiter (Parser policies, INI).
- **What the dialect cannot write, the writer must refuse.** Bodu has no quoting, escaping or continuation lines,
  so a value holding a line break, a value with leading or trailing spaces or tabs (which the reader trims), and a
  key that contains `=` or starts with `[` cannot be written so that they read back unchanged. Where a fix made
  its writer handle such a value, the row is `applies`, kind `write-reject`, expecting `ArgumentException`, the
  repository's argument-validation convention: refusing is the only faithful behaviour left, and writing a line
  that reads back as something else is the defect the fix removed. These expectations are `derived`, and
  `Utf8IniWriter` now meets them (below).
- **A header with `]` inside it.** configparser, go-ini and iniparser fixed their readers to end a section name at
  a later `]` on the line; rust-ini (by default) and inih end it at the first. Bodu's documents did not say which,
  so these rows are `applies` with the fixes' reading; Bodu now documents that reading (below).
- **Special names are data.** `__proto__`, `constructor` and other names that reached object prototypes in npm ini
  are ordinary section and key names to Bodu, so those rows are `applies`.
- **Typed and DOM scenarios are `unit` rows**: mapping-protocol and editing fixes against `IniObject` and
  `Utf8IniWriter`, binding fixes against `IniSerializer`, and error positions against `Utf8IniReader`.
- **Out of scope** (`n/a`, with the reason in the row): interpolation, type coercion helpers, case-folding APIs,
  file loading and reloading, the configparser mapping protocol where Bodu has no counterpart, struct reflection,
  arrays built from duplicate keys, platform line endings (Bodu's writer always writes LF), memory safety and
  allocation failures in C, Python 2, and compiler warnings.
- **Versions** are the release whose notes list the fix, else the first tag (or published version) containing
  the commit; a fix repeated in several release lines is catalogued once, at the main line. New features,
  refactors, build and packaging changes, tests, CI and documentation are not catalogued, and each file's header
  says what it left out.

### What the catalogue found

When the catalogue first ran, 120 of the 132 rows that run as data passed and 12 failed, and of the 14 `unit` tests
the go-ini 1.34.0 one failed. Each cause became an issue and was fixed test first, so all 132 rows and all 14 `unit`
tests now pass. No row was re-classed: every fix made Bodu do what the release-note fix established.

- **A section header ended at its first `]`, and the rest of the line was dropped without an error** (issue #845;
  4 rows: configparser 3.11.0a1 bpo-38741, go-ini 1.16.0 #46 and iniparser 4.2 PR #159 cases 1-2). `[foo]bar]`
  named the section `foo`. A section name now runs to the first `]` that only whitespace or a comment follows, so
  `[foo]bar]` names `foo]bar` and `[123]45]` names `123]45`; other text after the header throws
  `IniFormatException`, and a comment after it is still skipped rather than reported as a comment token. The rule
  is stated in [Parser policies](../../docs/docs/formats/parser-policies.md) (INI), the INI guide and the remarks
  of `Utf8IniReader`.
- **The writer wrote a line break inside a value or a comment** (issue #841; 4 rows: configparser 3.15.0b4 #143927
  cases 1-3 and configparser 2.3 `00824ed733` case 1, and the go-ini 1.34.0 `5e9692864e` `unit` test). `key1` =
  `a\nb` was written as `key1=a` and a bare line `b`. `WriteString` now throws `ArgumentException` for a value
  containing CR or LF, and `WriteComment` writes text holding line breaks as one comment line per line, each with
  the comment prefix.
- **The writer wrote a value's leading and trailing spaces, which the reader trims** (issue #843; 1 row: go-ini
  1.60.1 #260 case 2). `  val ue1 ` was written as `bar1=  val ue1 ` and read back as `val ue1`. `WriteString` now
  refuses a value that begins or ends with a space or tab; an empty value is still written.
- **The writer wrote keys that read back as something else** (issue #842; 3 rows: configparser 3.14.0a6 #65697
  cases 1-2 and npm-ini 1.0.2 `b80890bf43` case 2). The key `a=b` was written as `a=b=c`, read back as key `a`, and
  the keys `[this parses back as a section]` and `[disturbing]` were written as lines that read back as section
  headers. `WritePropertyName` now refuses a key that is empty, begins or ends with a space or tab, contains `=` or
  a line break, or begins with `[`, `;` or `#`, and `WriteSectionHeader` refuses a name that is empty, begins or
  ends with a space or tab, contains a line break, or holds a `]` that a comment marker follows.

The documents now match the code. Five of them said an INI value runs literally to the end of the line, although
the reader trims the whitespace around keys and values (issue #844, documentation only); they now say so. The
remarks of `Utf8IniWriter` said that each call writes one line, and the summary of `IniNode` that authoring and
round-tripping are faithful, while text the dialect cannot hold was written as other lines; the writer now
refuses that text, and its rules are documented in its remarks and the INI guide (Writing). The error-handling
guide (`docs/guides/formats/error-handling.md`) now says that only whitespace or a comment may follow a header,
and lists the new message.
