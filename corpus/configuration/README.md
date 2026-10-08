# Bodu configuration validation corpus

External evidence used to validate `Bodu.Text.Configuration`, kept in the repository so that every reconciliation can
be reproduced from committed files. The `EditorConfigCompatible` profile documents alignment with the EditorConfig
specification 0.17.2 (https://spec.editorconfig.org), so that specification is the reference for the rows here, and
Bodu's own documents are the reference for the `Bodu`, `Strict` and `Relaxed` profiles. The EditorConfig cores share one
conformance suite, editorconfig-core-test, and a fix usually adds a case to it, so the evidence here is what six cores
learned by fixing their own defects, most of it restated from the cases those fixes added.

No fix concerns the bridge package `Bodu.Extensions.Configuration.Text`: no core has a counterpart of
`Microsoft.Extensions.Configuration`, so every row runs against `Bodu.Text.Configuration` itself.

## `fixes/` - the release-note fix catalogue

The release notes of six EditorConfig cores were read in full, and every fix they list is catalogued: one file per
library, `fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. Two cores publish no
release notes at all (editorconfig-core-py and ec4j), and the others leave releases out or sum fixes up in a phrase
("Other misc fixes"), so the commit history was read as well, and each file's header says for which releases. A fix
found only there is catalogued under the first release that contains it. A scenario is, first, the core-test case or
regression test the fix added, read from its commit, and otherwise the scenario its issue or commit describes. Each row
records the version, the reference (an issue, a pull request or a commit) and a short summary in our own words, then a
class, and, for a runnable row, the input and the result Bodu must give.

### Sources

| File | Library | Language | What was read | Licence |
|---|---|---|---|---|
| `editorconfig-core-c-fixes.csv` | [editorconfig-core-c](https://github.com/editorconfig/editorconfig-core-c) | C | `CHANGELOG` at 7a42c65078121bb3578a19941b6b50713e42f6bf on master (SHA-256 7788d12f565ec9604fe2fea99fdd973f21c0c6ed892e793aa6294dc04e3e8518), v0.12.0 to v0.12.11; the GitHub releases (one page, v0.12.2 to v0.12.11), read 2026-10-08, whose notes for v0.12.4 to v0.12.9 differ from the `CHANGELOG`'s; the commit history at the same commit for v0.8.0 to v0.11.5, which have no notes, and for the commits behind the terse entries | BSD-2-Clause |
| `editorconfig-core-js-fixes.csv` | [editorconfig-core-js](https://github.com/editorconfig/editorconfig-core-js) | JavaScript | `CHANGELOG.md` at 71d4a0a5ee9b3ee3e07c82c456965697395fbbc0 on main (SHA-256 e960aa21534cf41ecb35fbff01ca5a2302d93185ec445f28833bb0a7fd6c655f), 0.15.0 to 2.0.0; the GitHub releases, pages 1-2 (v1.0.0 to v3.0.2), read 2026-10-08; the commit history at the same commit and on the 1.0.x branch, for 0.11.0 to 0.14.2 and 1.0.4, which have no notes, and for fixes the 1.0.0 notes leave out; npm publish dates for the releases without a git tag | MIT |
| `editorconfig-core-py-fixes.csv` | [editorconfig-core-py](https://github.com/editorconfig/editorconfig-core-py) | Python | No release notes exist (no changelog, and the GitHub releases page lists none, read 2026-10-08), so the commit history at e1298e0b8c66041f2e32e84d7c7bbe99ffdba339 on master, v0.9.0 (the first tag) to 0.17.1 and the unreleased commits after it; the git tags and the PyPI upload dates place each commit in its release | BSD-2-Clause AND PSF-2.0 |
| `editorconfig-core-net-fixes.csv` | [editorconfig-core-net](https://github.com/editorconfig/editorconfig-core-net) | C# | The GitHub releases, pages 1-2 (0.12.0 to 0.18.0), read 2026-10-08; only 0.12.0 to 0.13.0 have notes, so the commit history at b793e9b5aa015eabf821f46ee302205fb92ebbe6 on master as well, for 0.14.0 to 0.18.0 and for the fixes the 0.12.2 notes leave out; there is no changelog file | MIT |
| `editorconfig-core-go-fixes.csv` | [editorconfig-core-go](https://github.com/editorconfig/editorconfig-core-go) | Go | `CHANGELOG.md` at 410d66489641197f03eacb3dd6c65b4e1ff899a8 on master (SHA-256 6f3f7ecd35c627e69a07cdc9d0994f7eb2329d7abadc2777818f2e239ee796a3), v2.0.0 to v2.6.5; the GitHub releases, pages 1-4, read 2026-10-08, whose commit lists name fixes the changelog leaves out; the commit history at the same commit for v1.0.0 to v1.3.1, which have no notes | MIT |
| `ec4j-fixes.csv` | [ec4j](https://github.com/ec4j/ec4j) | Java | No release notes exist (no changelog file in any branch or tag, and the GitHub releases page lists none, read 2026-10-08), so the commit history at 64a1f1a0ef1ac78d4f849de34a1c596e3fc87f91 on main, 0.0.1 to 1.2.0, each commit placed in the first tag that contains it | Apache-2.0 |

Two shared references were read alongside them:

| Reference | What was read | Licence |
|---|---|---|
| [EditorConfig specification](https://github.com/editorconfig/specification) | `index.rst` at tag v0.17.2 (329077a5649b013903c2aeaa5617afb70fcc9eaf, SHA-256 841bfdbd71b13c68e57ae89a81e78e34042dba0e09dedcd4bc2520007afcd961), the version the `EditorConfigCompatible` profile names; the `spec` rows and many `reason`s cite its sections | BSD-2-Clause |
| [editorconfig-core-test](https://github.com/editorconfig/editorconfig-core-test) | The cases at 895b3a65d0d823dbd0acf2bc402376381995d1b1 on master, and the commit that added each case a fix relies on | BSD-2-Clause |

Each file's header repeats its sources and records what was left out and why.

### What is catalogued

- Fixes only. New features, refactors, documentation, CI, test-suite bumps and dependency updates are not catalogued.
  A fix to packaging, the build, a platform, a core's own API or command line tool, or the library's language (memory
  safety and undefined behaviour in C, JavaScript value coercion, Go error wrapping, linter findings) is catalogued, as
  `n/a`. So is the one dependency update the notes list as a fix, editorconfig-core-js 1.0.3's move to a semver release
  without a published security flaw.
- Fixes made before a library's first release, and fixes to code first added in the same release cycle, are not
  catalogued; each header names the commits this leaves out. A line of the notes that names several fixes
  (editorconfig-core-c's "Memory leaks and crash fixes" and "Other misc fixes") is catalogued as one fix per commit, as
  is a commit that fixes several defects (editorconfig-core-py's b107472).
- Bodu resolves one document for one target path and interprets no property, so two kinds of fix are `n/a` wherever
  they appear: the search for `.editorconfig` files up the directory tree, `root = true` ending that search included,
  and EditorConfig property semantics (lowercasing the values of the properties the specification lists, the
  `indent_size` and `tab_width` defaults, charset handling).
- Every library here and editorconfig-core-test are permissively licensed, so a short input from a regression test or
  a core-test case is restated, and the test is named in `reason`. An input that restates a core-test case keeps only
  the sections the case is about, so where the case's file would add other pairs, `expected` holds only what the kept
  sections give. No upstream file is committed.
- No core is a port of another. editorconfig-core-py adapts the `fnmatch` and `ConfigParser` modules of Python 2.6.
  editorconfig-core-net was written in C# to replace a .NET wrapper around the C core (3fad57f calls it "a .NET port
  of editorconfig-core"); it matches with a C# port of minimatch, the library editorconfig-core-js's first matcher was
  based on, but none of its catalogued fixes concerns matching. ec4j takes a few portions of code from
  editorconfig-core-java, which is not catalogued.
- The cores do share editorconfig-core-test, so a scenario that two cores fixed is one scenario, catalogued under each
  library and not independent evidence. editorconfig-core-c and editorconfig-core-py both hold the byte order mark case
  (`bom_at_head`), the slash-in-brackets cases (`brackets_slash_inside1` to `brackets_slash_inside4`),
  `question_slash`, and the length cases (`max_property_name`, `max_section_name_ok`, `max_section_name_ignore` and the
  three `min_supported_*` cases). editorconfig-core-go holds the `max_*` length cases too and ec4j the `min_supported_*`
  ones, and editorconfig-core-go and editorconfig-core-py both hold `braces_nested_start1` and
  `braces_nested_start3`. editorconfig-core-js's directory-name rows are the scenario of editorconfig-core-c's #23
  (`path_with_special_chars`), with inputs of their own.

### Columns

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes or tags write it |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (first 10 hex digits) or empty |
| `summary` | A short paraphrase of the entry |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it needs several |
| `kind` | `parse`, `reject`, `write`, `write-reject`, `roundtrip` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` (below); empty for the defaults |
| `input`, `expected` | The `.editorconfig` text and the expected result, escaped (below) |
| `expectation` | Where `expected` comes from: `upstream` (the fix's own test, or the core-test case it added or began to pass), `spec` (the EditorConfig specification 0.17.2), `derived` (worked out, with the working in `reason`) or `oracle:<tool>` (not used here) |
| `reason` | Required on every row: the upstream test or issue for a runnable row, the Bodu document or specification section a `dialect` row relies on, the exclusion an `n/a` row falls under |

Every file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII.
Anything else in `input`, `expected` and `options` is written with an escape: `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH`
(one byte) and `\u{H...}` (the UTF-8 encoding of one Unicode scalar). Fields follow RFC 4180 quoting.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 93 | The row runs, and Bodu must do what the fix established |
| `dialect` | 11 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix; `reason` names the document or specification section |
| `n/a` | 91 | The fix concerns something Bodu does not have (the search for `.editorconfig` files, EditorConfig property semantics, a core's API or command line tool), the library's language, a platform, packaging or the build; `reason` says which |
| `unknown` | 0 | No scenario could be found |

The `dialect` rows rest on five stated behaviours. Bodu rejects a section glob whose `[` or `{` has no partner
(`UnbalancedBracket`, `UnbalancedBrace` in `docs/guides/text-configuration/diagnostics.md`) where the cores read it
literally (editorconfig-core-c #23 and PR #54, editorconfig-core-py PR #32). It rejects a glob longer than 4096
characters (`PatternTooLong`) where the cores skip the section or, since ec4j 0.3.0, match it (editorconfig-core-c
649b6c19c0, editorconfig-core-py PR #32, editorconfig-core-go PR #35, ec4j 6111e87994); the specification leaves the
limit beyond 1024 characters to each implementation. The `Strict` profile rejects a repeated key (`DuplicateKey`), of
which editorconfig-core-net 0.13.0 keeps the last value (#15 case 3). Bodu normalises the target path to forward
slashes before matching (`docs/docs/text-configuration/concepts.md`, Glob pattern and target path), so a file name
holding a backslash cannot match as core-test `backslash_not_on_windows` expects (editorconfig-core-go PR #170
case 4). And the specification reads a brace group
without a comma literally, so editorconfig-core-go 1.0.1's early expectation that `{foo}.go` and `{}.go` match `foo.go`
gives way to it (0ec8275ddc cases 4 and 5), as it did in that core from 2.1.0.

### How a row runs

`input` is decoded as UTF-8, a byte order mark kept as the character U+FEFF, parsed with
`ConfigurationDocument.Parse(text, ConfigurationParseOptions.For(profile))`, and resolved with
`document.Resolve(target, ConfigurationResolveOptions.For(profile))`. The view is rendered as a compact JSON object of
its pairs in enumeration order, each key and value a JSON string that escapes only `"`, `\` and control characters, and
`null` standing for a null value: `{"indent_size":"4","indent_style":"space"}`, or `{}` when nothing applies. Under
`EditorConfigCompatible`, the profile almost every row names, the view leaves out the preamble, so `root = true` is not
in it, just as a core's output leaves it out. A glob fix is a `parse` row: a section header, and a target that must or
must not match it.

| Kind | Passes when |
|---|---|
| `parse` | Parsing and resolving `input` succeed and the rendering equals `expected` |
| `reject` | Parsing or resolving `input` throws an exception whose type name is `expected` (`ConfigurationParseException`) |
| `write` | `ConfigurationDocument.Save` with `ConfigurationWriteOptions.For(profile)` writes exactly `expected` for the parsed document |
| `write-reject` | That save throws an exception whose type name is `expected` |
| `roundtrip` | Parsing, saving and parsing the written text again gives the same rendering both times, and the written text equals `expected` when `expected` is not empty |
| `unit` | Not run from the file: `input` names a test to be written in `Bodu.Text.Configuration.Test` (`<TestClass>.<Member>_When<Condition>_Should<Result>`), and `reason` gives its model, input and expected outcome |

The catalogues use `parse` (95 rows), `reject` (8) and `roundtrip` (1).

| Option | Maps to | Values |
|---|---|---|
| `Profile` | the parse, resolve and write presets (`ConfigurationParseOptions.For`, `ConfigurationResolveOptions.For`, `ConfigurationWriteOptions.For`) | `Bodu` (default), `EditorConfigCompatible`, `Strict` or `Relaxed` |
| `Target` | the path passed to `Resolve` | a relative path, escaped; default `a.txt` |

### Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| editorconfig-core-c | 47 | 57 | 20 | 3 | 34 | 0 |
| editorconfig-core-js | 26 | 31 | 10 | 0 | 21 | 0 |
| editorconfig-core-py | 26 | 39 | 24 | 2 | 13 | 0 |
| editorconfig-core-net | 10 | 12 | 3 | 1 | 8 | 0 |
| editorconfig-core-go | 18 | 40 | 25 | 4 | 11 | 0 |
| ec4j | 8 | 16 | 11 | 1 | 4 | 0 |
| **Total** | **135** | **195** | **93** | **11** | **91** | **0** |

All 104 `applies` and `dialect` rows are runnable; there are no `unit` rows.

### Status

`ConfigurationReleaseNoteCorpusTests` in `Bodu.Text.Configuration.Test` runs every runnable row from byte-identical
copies of these files embedded under `Fixtures/ReleaseNotes/`, pins the counts above, and checks that the copies match
these files. All 104 runnable rows pass on net8.0 and net10.0, and no row was re-classed.

The first run against Bodu failed 13 rows, with four causes, each since fixed test first:

- **#864.** A bracket expression holding a `/` was read as a character class (editorconfig-core-c v0.12.0 cases 1
  to 3, editorconfig-core-py 0.12.0 edf2cb3e76 cases 1 to 3). The glob compiler now reads it as literal text, brackets
  included, so `[ab[e/]cd.i]` matches only `ab[e/]cd.i`, as core-tests `brackets_slash_inside1` to
  `brackets_slash_inside3` require.
- **#865.** `ConfigurationDocument.Parse(string)` kept a leading byte order mark, so a file opening with a comment
  failed with `MissingEquals` (editorconfig-core-c v0.9.1 12697755ea, editorconfig-core-py 0.10.0 b278104f25).
  `Parse`, `TryParse`, `ParseWithDiagnostics` and `Load(TextReader)` now ignore one leading U+FEFF under every profile,
  as `Load` ignores a stream's mark.
- **#866.** The `EditorConfigCompatible` profile reported keys in the case they were written in (editorconfig-core-go
  v2.1.0 PR #15 case 1, core-test `lowercase_names`). It now lowercases them with the invariant culture, through the
  new `ConfigurationKeyOptions.LowercaseKeys`; the other profiles keep keys as written.
- **#867.** A brace group without a comma was read as a choice (editorconfig-core-go v2.1.0 PR #15 cases 5, 6 and 9,
  and the `dialect` row v1.0.1 0ec8275ddc case 4). A group now expands only on a top-level comma or a range of two
  integers, and anything else is literal text, so `[{single}.b]` matches only `{single}.b`, and the `dialect` row
  passes as written.

The research behind the catalogue found more, outside the rows, fixed the same way:

- **#868.** A section glob starting with `/` matched only a target that itself started with `/`. A relative target now
  matches it as if it began with `/` (core-test `leading_slash_relevance`).
- **#869.** The `EditorConfigCompatible` profile mapped dotted keys to colons, though documented as identity mapping.
  It now keeps the dots, and its documentation says that the whole preamble, `root` included, stays out of the view.
- **#870.** The `Bodu` profile resolved the preamble's `root` pair as a property. No profile does now; the pair stays
  in the document's global section.
- **#871** to **#874** corrected documentation only: the `Normalized` write preset sorts nothing, `InvalidEscape` is
  reserved and never raised, four passages of the configuration documentation contradicted the code, and the bridge
  README named registration methods the package does not have.
- **#915.** `ConfigurationDocument.Load(path)` recorded no root, though six passages of the documentation said it did. A
  document loaded by path now resolves its globs against the full path of the file's directory when no `PathRoot` is
  set, as EditorConfig defines a glob, while an explicit `PathRoot` still wins and a parsed document has no root.
- **#916.** The `Identity` mapping split a key and rejoined it on the first separator, so `a:b` became `a.b`. It now
  keeps every key exactly as written, apart from the lowercasing `LowercaseKeys` asks for, so under
  `EditorConfigCompatible` a dotted key and a colon-delimited one are two keys, and a key such as `a..b` is kept rather
  than rejected.
- **#921.** `ConfigurationKey` said whitespace in segments was trimmed, and none was. Each segment is now trimmed, so
  the key written `a . b` is `a:b`, a segment of whitespace alone counts as empty, and under `Identity`, which does not
  split a key, only the ends of the key are trimmed.
- **#922.** A resolved entry's `SourceLocation.Path` was always `null`, though two passages said a loaded document's
  carried the path. A document loaded by path now records the full path of its file, and every entry resolved from it
  reports that path.
