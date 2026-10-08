# Bodu YAML release-note fix catalogue

External evidence used to validate `Bodu.Text.Yaml`: every defect fix that nine other YAML libraries list in their
release notes, each turned into rows that run against Bodu. There is one file per library,
`fixes/<library>-fixes.csv`, with one row per fix, or one per case where a fix needs several. It repeats for YAML
what `corpus/recurrence/cron-fixes/` did for cron (see `corpus/recurrence/README.md`, "the release-note fix
catalogue").

A scenario is, first, the regression test the fix added, read from its commit or pull request, and otherwise the
scenario its issue describes. Each row records the release, the reference (an issue, a pull request or a commit) and
a summary in our own words, then a class and, for the classes that run, the input and the expected outcome. Entries
that fix a defect or address an issue are catalogued, security fixes included; new features, refactors,
documentation, build and packaging changes are not, and each file's header says what else was left out and why.

## Sources

| Library | Repository | What was read | Releases | Licence |
|---|---|---|---|---|
| `libyaml` | https://github.com/yaml/libyaml | `Changes` at master 90a56d4500 (SHA-256 `9b81db468ba6d1bf360a561e325260801b8c00dcc78708ef2fb72fd04a3138d5`) and at tag 0.2.6-rc.1 9aa92d0e2d (SHA-256 `1c649321a70ed36137162a203b35406be13be7d6b06ba4b004039e0518b5e57f`), `announcement.msg`, and the GitHub releases page (read 2026-10-08) | 0.1.1 to 0.2.6-rc.1 | MIT |
| `pyyaml` | https://github.com/yaml/pyyaml | `CHANGES` at f5e4504a92 (SHA-256 `acde4c3fd88aa9630bfb0b343978c8ab32f6ad76b9ff817a50501faed744ea0c`) and the GitHub releases page (read 2026-10-08) | 3.01 to 6.0.3 | MIT |
| `yamldotnet` | https://github.com/aaubry/YamlDotNet | `RELEASE_NOTES.md` (SHA-256 `53a6768cb21aed12579ad018611490b4b71eecb8ae510499216955cb22a2a85b`) and `releases/*.md` (concatenated in path order, SHA-256 `24a3aaa8cd8054e55540fe7d68d710eb47aa9feda56fe8a6769552f52fb4a72c`) at 5f7cc9e7c4, and the GitHub releases pages 1 to 10 (read 2026-10-08) | 2.0.0 to 18.1.0 | MIT |
| `eemeli-yaml` | https://github.com/eemeli/yaml | The GitHub releases pages 1 to 10 (read 2026-10-08) and the tag v1.10.3, which has no notes; commits and tests at 528ef30d6d | 1.0.0-beta.1 to 3.0.0-2 | ISC |
| `js-yaml` | https://github.com/nodeca/js-yaml | `CHANGELOG.md` at 78326247fa (SHA-256 `7dde31e3ff0cdd229c799f9bc5c1780207410fb997b1f297b4f93f7843b25cad`), with the maintenance changelogs at tags 4.3.2 and 3.15.2 | 0.2.0 to 5.4.3 | MIT |
| `go-yaml` | https://github.com/go-yaml/yaml (archived), continued at https://github.com/yaml/go-yaml | No release notes in either repository (both releases pages empty, read 2026-10-08), so the commit history: go-yaml/yaml at 944c86a7d2 and yaml/go-yaml at 823c494d84 | 2.0.0 to 4.0.0-rc.6 | MIT AND Apache-2.0 |
| `snakeyaml-engine` | https://bitbucket.org/snakeyaml/snakeyaml-engine | `src/changes/changes.xml` at 08833ea4ed (SHA-256 `959c9c0471ec85f812e4d58566ed23e92a3e864139865d7a1ecb8270d702b53f`) | 0.1 to 3.2 | Apache-2.0 |
| `yaml-cpp` | https://github.com/jbeder/yaml-cpp | The GitHub releases page (read 2026-10-08); the generic "bug fixes" entries from the commits at 1e0876c671 | 0.5.3 to 0.9.0 | MIT |
| `ruamel-yaml` | https://sourceforge.net/projects/ruamel-yaml/ | `CHANGES` from the PyPI sdist `ruamel_yaml-0.19.1.tar.gz` (sdist SHA-256 `53eb66cd27849eff968ebf8f0bf61f46cdac2da1d1f3576dd4ccee9b25c31993`, `CHANGES` SHA-256 `de894ed9cb85c0f4991eaa1b6769b211d087df41400ffcc72353f6a6d3b6882b`); tests from the SourceForge Mercurial repository at 18535afdf4 (read 2026-10-08) | 0.1 to 0.19.0 | MIT |

Where the notes say too little, the commits were read as well, and the file's header says so: go-yaml publishes no
notes at all (each fix commit is listed under the first tag that contains it), PyYAML's entries before 5.1 carry no
references, and libyaml 0.1.2, eemeli/yaml's early betas and release candidates and yaml-cpp's "bug fixes" entries
name no scenario. yaml-cpp's releases before 0.5.3 publish no notes and are not catalogued, nor are the commits after
yaml-cpp 0.9.0 and go-yaml v4.0.0-rc.6 that no notes list yet.

Rows that restate yaml-test-suite cases read them from the suite Bodu vendors (`Bodu.Text.Yaml/test/yaml-test-suite`,
release data-2022-01-17, commit 6e6c296a, MIT). No upstream file is committed: an input is a minimal reproduction, or
a short restatement of the fix's regression test, which `reason` names (every library here is MIT, ISC or
Apache-2.0), and a summary is a paraphrase. Where a reason says that PyYAML 6.0.1, or libyaml 0.2.5 through Ruby Psych
5.1.2, was run on the input, the installed implementation was used as a cross-check; no downloaded code was built or
run.

## Lineage: a port is not independent evidence

Several of these libraries descend from one another, so a fix that appears in two of them is often one fix, ported:

- libyaml and PyYAML share their author and design, and port fixes between them (the `\/` escape, colons in flow
  scalars).
- ruamel.yaml forked PyYAML 3.11 in 2014 (its 0.1), so what it inherited before the fork is not independent of the
  PyYAML catalogue; its 0.6 ports PyYAML's side of libyaml's CVE-2014-9130 fix, which is catalogued once, in
  `libyaml-fixes.csv`.
- SnakeYAML Engine is a YAML 1.2 rewrite of SnakeYAML, itself a Java port of PyYAML, and several of its entries port
  SnakeYAML fixes.
- js-yaml 0.x was a port of PyYAML; 2.0.0 rewrote it, so its later fixes are its own.
- go-yaml's scanner, parser and emitter are a Go port of libyaml, and YamlDotNet's began as a C# port of libyaml
  (2008).
- eemeli/yaml and yaml-cpp are independent implementations.

So a scenario that fails in several catalogues is one finding, not several (yaml-test-suite EB22 fails under libyaml,
YamlDotNet and SnakeYAML Engine), and the findings below are counted by root cause, not by row.

## One schema

Each file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII: non-ASCII
text, control characters and raw bytes are written with the escapes below. A file opens with `#` header lines
(`library`, `releases`, `licence`, then `note` lines on scope, exclusions and lineage), then the column line, then the
rows, quoted per RFC 4180: a field holding a comma or a double quote is enclosed in double quotes, with inner quotes
doubled, and no field holds a line break.

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes write it |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (its first 10 hex digits), a CVE id, or empty |
| `summary` | A short paraphrase of the entry |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it has several |
| `kind` | `parse`, `reject`, `write`, `write-reject`, `roundtrip` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` from the options below; empty for the defaults |
| `input` | The input, escaped; for `unit`, the proposed test method |
| `expected` | The expected result, escaped; see each kind |
| `expectation` | Where `expected` comes from: `upstream` (the fix's own test, including a yaml-test-suite case the fix names), `spec` (the YAML specification: its productions, examples and core schema) or `derived` (worked out; `reason` shows how) |
| `reason` | Required on every row: the upstream test or issue for a row that runs, the Bodu document or specification section for a `dialect` row, the exclusion for an `n/a` row, and what was read for an `unknown` row |

Escapes in `input` and `expected`: `\n` LF, `\r` CR, `\t` tab, `\0` NUL, `\\` a backslash, `\xHH` the single byte
0xHH, and `\u{H...}` the UTF-8 encoding of the scalar U+H...; any other backslash sequence is an error. A decoded field
is a byte sequence, and Bodu reads it as UTF-8.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 890 | The row carries the scenario of the fix and runs: Bodu must do what the fix established |
| `dialect` | 85 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix's; `reason` names the document |
| `n/a` | 526 | The fix concerns an API or feature Bodu does not have, the library's language or runtime, its platform, packaging, or performance only; `reason` says which |
| `unknown` | 4 | No scenario could be found; `reason` links what was read |

A row is `dialect` only when a Bodu document (the package README, the guides under `docs/`, the XML documentation) or
the YAML specification states Bodu's behaviour. `YamlTestCorpusReader.UnsupportedFeatureRejectedIds`, the corpus
tests' list of valid suite cases that Bodu rejects, is cited beside such a document but is never enough on its own:
where it lists a case whose features the documents support (P2AD, R4YG, A2M4, 2SXE, NJ66, UT92, and the escaped-tab
cases KH5V/01, 3RLN/01, 3RLN/04, DE56/02 and DE56/03), the row stays `applies`, and its failure is a finding. The
deliberate differences are: the YAML 1.2 core schema where a library resolves YAML 1.1 or a JSON-like schema; the
profile's rejections (complex and alias keys, duplicate keys under the default `Throw`, anchor override, cyclic
aliases, non-core tags, unsupported directives, tabs in a line's leading whitespace, non-printable characters, and
nesting beyond `MaxDepth`); `SpecVersion.V1_1` changing scalar typing only, never structure; and a few places where
Bodu follows the specification and the fix did not (a top-level block scalar's indentation indicator counts from a
parent indentation of -1, so `|1` means no indentation; `\L` and `\P` are escapes; anchor names may hold `.` and
`/`; and a flow collection may not hold an empty entry).

The common `n/a` reasons are that Bodu's DOM keeps values only (no comments, scalar styles, anchors or positions), that
its writer emits block collections with plain or double-quoted scalars and no tags, anchors or aliases, that it has no
event or token stream API, and that the other libraries' object models (Python, JavaScript, Java, Go, C++ and .NET
types) have no counterpart. A serializer fix with a `System.Text.Json`-shaped counterpart in `YamlSerializer` is a
`unit` row instead.

### Kinds

| Kind | Rows | Runs as |
|---|---:|---|
| `parse` | 486 | `YamlDocument.ParseAllDocuments(input, options)` succeeds, and its rendering matches `expected` |
| `reject` | 324 | `ParseAllDocuments` throws the exception `expected` names, always `YamlFormatException` |
| `write` | 31 | The JSON value in `input`, written through `Utf8YamlWriter`, produces exactly `expected` |
| `write-reject` | 0 | Writing `input` throws the exception `expected` names |
| `roundtrip` | 99 | Read `input`, write each document back, and read again: both readings render the same, and a non-empty `expected` is the exact written text |
| `unit` | 35 | A typed scenario (a `YamlSerializer` model, a converter, a callback): `input` names the test to write, `<TestClass>.<Member>_When<Condition>_Should<Result>`, and `reason` describes the model, input and outcome |

Every input is read with `ParseAllDocuments`, so single- and multi-document inputs take one path. The corpus tests use
`YamlDocument.Parse` instead, which differs in one way the rows show: it rejects content left over after a complete
document, where `ParseAllDocuments` starts another (below).

### Rendering

A `parse` row's `expected` is the yaml-test-suite `in.json` convention: one JSON value per document, the values of a
multi-document stream separated by `\n`. The comparison is the corpus tests' `CorpusCompare.Matches`:

- A JSON object matches a mapping with the same keys, in any order. Bodu's DOM keys mappings by text, so a key renders
  as its scalar's text (`1: a` is `{"1":"a"}`, `null: a` is `{"null":"a"}`) and an empty key as `""`.
- An array matches a sequence, item by item; a string matches a string scalar, `true` and `false` a Boolean, and
  `null` a null.
- A number matches an integer or float scalar of the same value, with one refinement: a number written without a
  fraction or an exponent matches only an integer, so a row can tell `1` from `1.0`.
- The non-finite floats, which JSON cannot hold, are written as the bare tokens `.inf`, `-.inf` and `.nan` wherever a
  number may stand.

A failing row reports Bodu's own rendering in the same notation, with each float in round-trip form and `.0` added to
an integral one.

Tags, anchors and scalar styles do not show in JSON. A `View=Events` rendering, the yaml-test-suite event notation,
was considered for fixes whose only effect is there; no row needs it, so it is not implemented.
Bodu has no event stream (`Utf8YamlReader` walks the composed tree, and the README names a streaming event reader as a
future extension), so a fix whose only effect is on events or tokens is `n/a`, and every other fix shows in the
values.

### Write notation and round trips

A `write` row's `input` is a JSON value, written by walking it with a `Utf8YamlWriter` that has the default
`YamlWriterOptions`: an object as a mapping in property order, an array as a sequence, a string with `WriteString`, a
number without a fraction or an exponent that fits a 64-bit integer with `WriteInteger` and any other number with
`WriteDouble`, a Boolean with `WriteBoolean`, and null with `WriteNull`. JSON cannot hold a non-finite number, so no
`write` row writes one.

A `roundtrip` row reads `input`, writes each document back through `YamlElement.WriteTo` into its own `Utf8YamlWriter`
with the default options, joins the documents of a stream with a `---` line, and reads the result again with the
row's options. The two readings must render identically, and, when `expected` is not empty, the written text must
equal it. Where a fix is about a property of the output (a string that looks like a number stays quoted), the round
trip shows that the value reads back unchanged, and exact text is pinned only where the fix pins it.

### Options

| Option | Maps to | Values | Rows |
|---|---|---|---:|
| `SpecVersion` | `YamlDocumentOptions.SpecVersion` | `V1_2` (the default) or `V1_1` | 73 |
| `DuplicateKeyBehavior` | `YamlDocumentOptions.DuplicateKeyBehavior` | `Throw` (the default), `UseFirst` or `UseLast` | 3 |
| `MaxDepth` | `YamlDocumentOptions.MaxDepth` | An integer; zero or less selects the default of 64 | 1 |

A fix whose answer depends on YAML 1.1 versus 1.2 has one row per version where the answers differ. Rows that expect
the YAML 1.1 reading, and every merge-key row, set `SpecVersion=V1_1`: the merge key is a YAML 1.1 feature, and every
Bodu document agrees that `MergeKeyBehavior.Expand` applies under `V1_1`, whereas they disagree about the default
`V1_2` (below). No row needs another `MergeKeyBehavior`, so it is not an option.

## Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| libyaml | 37 | 48 | 24 | 3 | 21 | 0 |
| pyyaml | 64 | 72 | 22 | 6 | 44 | 0 |
| yamldotnet | 142 | 230 | 148 | 14 | 66 | 2 |
| eemeli-yaml | 200 | 325 | 212 | 11 | 102 | 0 |
| js-yaml | 120 | 179 | 116 | 13 | 49 | 1 |
| go-yaml | 114 | 152 | 79 | 12 | 61 | 0 |
| snakeyaml-engine | 51 | 185 | 160 | 14 | 11 | 0 |
| yaml-cpp | 54 | 79 | 48 | 4 | 27 | 0 |
| ruamel-yaml | 212 | 235 | 81 | 8 | 145 | 1 |
| **Total** | **994** | **1,505** | **890** | **85** | **526** | **4** |

SnakeYAML Engine has many rows per fix because its 2.4, 3.1 and 3.2 entries each made a batch of yaml-test-suite
cases pass, and each case is a row.

## What the catalogue found

Of the 975 `applies` and `dialect` rows, 35 are `unit` rows whose tests are still to be written. Of the other 940,
run against `Bodu.Text.Yaml` at c7b500d66a, 845 pass and 95 fail: 92 `applies` rows, and 3 `dialect` rows whose
stated behaviour (the README's, or the specification's) Bodu does not deliver. The failures are recorded, not fixed;
each is to be fixed test first, and no row is weakened to pass. By root cause, reading:

- `ParseAllDocuments` starts a new document at any content left after a complete root node, and ignores text after
  `...` on its line, where `Parse` rejects both (`[ 42 ]\nhello`, `---\nkey: value\n... invalid`, EB22, 3HFZ): 16
  rows.
- A block scalar that is a block-sequence entry takes the entry's content column as its parent indentation, so
  `- |1\n  x` reads `x\n`, not ` x\n` (P2AD, R4YG): 4 rows, and part of 3 of the rows above.
- In a flow sequence, `?` is read as text: `[ ? ]` gives `["?"]`: 5 rows.
- An explicit key with no value is rejected (`? a\n? b`), and so is a compact sequence after an explicit `:`
  (`? a\n: - b`, A2M4), while `? "foo"\n : "bar"`, its `:` one column in, is accepted: 7 rows.
- Node properties: a second tag or anchor on one node is read as text (`! ! a`); an anchor or tag before a flow
  collection that holds `: ` misreads it (`- &a { x: 1 }`); empty anchor names are accepted (`- &`, `&] value`); an
  anchor name ending in `:` at the start of a document or entry is misread (2SXE); `&a{}` is accepted without a
  separating space; a tab after the properties of a root node or an entry is taken for indentation; a tag on a
  null-like plain scalar loses its text (`!!str null` gives `""`); tag URI escapes are not decoded (`!!%73tr`); and a
  verbatim tag holding a comma is rejected (`!<tag:yaml.org,2002:str>`): 18 rows.
- Keys and plain scalars: implicit keys beginning with `,`, `@` or a backquote are accepted, as are `]` and `}` as
  plain scalars and implicit keys over 1,024 characters; a flow collection used as a later implicit key is coerced to
  its text (`a: 1\n[3]: 4`); a multi-line plain key in a flow mapping is rejected (NJ66, UT92); and so is an adjacent
  value after a JSON-like key in a flow-sequence pair (`[ "a":b ]`): 12 rows.
- Flow collections: empty entries are accepted (`{ , }`, `{foo, bar,, baz}`); after a plain key, a `:1` that starts a
  later line is taken for the value indicator (`{a\n#c\n:1}`); a closing bracket less indented than its parent block
  is accepted; and the depth limit counts nodes rather than containers, so 64 nested flow sequences exceed the default
  of 64: 6 rows.
- Escapes: the escaped tab (a backslash and a literal tab, YAML 1.2.2 production ns-esc-horizontal-tab) and a
  JSON-style surrogate pair (`"\uD83D\uDC4D"`) are rejected: 7 rows.
- `-\ta: b` is accepted, a tab indenting a compact mapping: 1 row.
- A byte order mark at the start of a later document is rejected: 1 row.
- Merge keys: an invalid merge value (a scalar, a null, or a sequence holding one) is silently dropped; a quoted
  `"<<"` key is merged; and under `UseLast` a repeated `<<` keeps only the last merge: 5 rows.
- YAML 1.1 resolution: a signed leading-zero octal reads as decimal (`-02472256`, `+071_352`), signed binary is not
  resolved (`-0b111` stays a string), and leading underscores resolve as numbers (`_123`): 4 rows.
- A float out of range (`1e309`) reads as infinity where the fix keeps a string, while Bodu itself rejects
  `!!float 1e309`: 1 row.

And writing:

- A `<<` key is written plain, so it reads back as a merge: 1 row.
- Strings that read back as other values are written plain: `1:1` (sexagesimal to a YAML 1.1 reader), and decimal
  strings with a leading zero and an 8 or a 9 (`019`, `08`; `007` is quoted), which Bodu's own reader then reads as
  integers: 3 rows.
- Implicit keys over 1,024 characters are written implicitly, which a YAML 1.2 reader must reject: 4 rows.

### Where Bodu's documents and behaviour disagree

- Merge keys. `README.md` (Conformance profile) calls the merge key opt-in, and the guide's Pattern 8
  (`docs/guides/serialization/yaml/using.md`) says it is gated on `SpecVersion = V1_1`, a `<<` key being ordinary under
  the default `V1_2`; `docs/docs/serialization/yaml/index.md` and using.md's Pattern 7 say `V1_1` enables it. Bodu
  expands `<<` under `V1_2` too, as the README's compliance matrix, the `YamlMergeKeyBehavior` XML documentation and
  `docs/guides/formats/writer-options-and-dom-options.md` (Pattern 9) say: `Expand` is the default.
- Sexagesimal numbers. The README, index.md and using.md say `V1_1` resolves them; the `YamlSpecVersion` XML
  documentation says they are deliberately not resolved, and Bodu keeps `1:20` a string.
- Depth. The XML documentation of `YamlReaderOptions.MaxDepth` and `YamlDocumentOptions.MaxDepth` says "container
  nesting", but Bodu counts nodes: `MaxDepth = 1` rejects `[1]` and `- 1` but accepts `a: 1`. Pattern 9 says "node
  nesting", which matches.
- Complex keys. The README says a sequence or mapping used as a key is rejected, not coerced, but a flow collection
  used as a later implicit key is coerced to its text. It also names only sequences and mappings, while Bodu rejects
  alias keys as well (its complex-key message names them).
- Tabs. The README rejects tabs used as indentation and keeps them legal as separation, but Bodu rejects a tab used
  as separation after node properties (`--- !!str\ta`) and accepts `-\ta: b`, where a tab indents a compact mapping. It
  also treats any tab in a line's leading whitespace as indentation, after the indentation spaces (DK95/00) or inside
  a top-level flow collection (6CA3, Q5MG); the catalogue takes that as documented, but the README does not spell it
  out.
- Escapes. The README and using.md reject invalid escapes, but Bodu also rejects the valid escaped tab.
- Writer quoting. Pattern 9 of using.md says the writer quotes a string whose plain form would resolve as a
  non-string, but it writes the strings `019` and `08` plain, which Bodu reads back as integers, and a `<<` key plain,
  which Bodu reads back as a merge.

## Running the rows

The rows are not yet run by the test project. Every row that runs was checked with a scratch console harness outside
the repository, built for `net10.0` against `Bodu.Text.Yaml` at c7b500d66a, which also checks the files themselves:
column count, known classes, kinds, expectations and option names, a reason on every row, valid escapes, and printable
ASCII only. Its `Renderer.cs` holds the CSV reader and file checks, the escape decoder, the option mapping, the JSON
rendering and comparison, the write notation and the round trip; it depends only on `Bodu.Text.Yaml` and the BCL
(`System.Text.Json` included), and is written to be ported into `Bodu.Text.Yaml/test`, where a test per row would run
the catalogue, as `RecurrenceCorpusTests` does for cron, and hold each file's count in each class.
