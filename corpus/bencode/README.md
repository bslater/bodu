# Bodu bencode validation corpus

External evidence used to validate `Bodu.Text.Bencode`, kept in the repository so that every reconciliation can be
reproduced from committed files. BEP 3 is the specification Bodu follows; it states the grammar in prose and publishes
no test vectors, so the evidence here is what other implementations learned by fixing their own defects.

## `fixes/` - the release-note fix catalogue

The release notes of seven bencode libraries were read in full, and every fix they list that touches bencode is
catalogued: one file per library, `fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs
several. A scenario is, first, the regression test the fix added, read from its commit, and otherwise the scenario its
issue or commit describes. Each row records the version, the reference (an issue, a pull request or a commit) and a
short summary in our own words, then a class, and, for a runnable row, the input and the result Bodu must give.

### Sources

| File | Library | Language | What was read | Licence |
|---|---|---|---|---|
| `bencodenet-fixes.csv` | [BencodeNET](https://github.com/Krusen/BencodeNET) | C# | `CHANGELOG.md` at 161e817295b6938237f22a19d1be28ea1944ee62 (SHA-256 d0a0f3f90af134d568f0e5db847a74c0e5e3edac2ccd3dbc250ee7bec074d3a0), 1.0.0 to 5.0.0; the GitHub releases, pages 1-2, read 2026-10-08; the commit history at the same commit | Unlicense |
| `bencode-py-fixes.csv` | [bencode.py](https://github.com/fuzeman/bencode.py) | Python | The GitHub releases (one page, 2.0.0 to v4.1.0), read 2026-10-08; there is no changelog file, so the commit history at a1109e5b1c6765a2297854b97acc4210edf4a726 as well, 1.0.0 to 4.1.0 | BitTorrent-1.1 |
| `node-bencode-fixes.csv` | [node-bencode](https://github.com/webtorrent/node-bencode) | JavaScript | `CHANGELOG.md` at 33b5377f86cac205f8923b99e1d684af13831e0d (SHA-256 4784b3f364ff051dce62aaeebf074ac79d20f5710fcb9cf0870bf51c119b0861), 0.0.1 to 4.0.1; the commit history at the same commit; npm publish dates for the releases without a git tag | MIT |
| `bendy-fixes.csv` | [bendy](https://github.com/P3KI/bendy) | Rust | `CHANGELOG.md` at 94cef149a70c45404ff757a03d4876e8f11aac75 (SHA-256 1703b1c72e9e4999658c965903cfccb1d0e76ddd800bb06a1163f574e29a77e3), 0.1.0 to 0.6.1; the GitHub releases (one page), read 2026-10-08; the commit history at the same commit | BSD-3-Clause |
| `serde-bencode-fixes.csv` | [serde_bencode](https://github.com/toby/serde-bencode) | Rust | The GitHub releases (v0.1.1 to v0.2.0) and the crates.io version list, read 2026-10-08; 0.2.1 to 0.2.4 have no notes, so the commit history at f36b82c6d528a4f84e7627c6fb06b7e3f4bdb1c4 as well | MIT |
| `libtorrent-fixes.csv` | [libtorrent](https://github.com/arvidn/libtorrent) | C++ | `ChangeLog` at 53acefea9193eed8dc389bc45730b82fe22ff3a2 on RC_2_1, the default branch (SHA-256 73d65206b1b7290862aca6f0e940142d7020d3d6c6ba5a57925e3b73c355e36f), 0.9.1 to the unreleased 2.1.3 | BSD-3-Clause |
| `transmission-fixes.csv` | [Transmission](https://github.com/transmission/transmission) | C and C++ | `news/*.md` at 48835c6660a7a3730b5a122bb7b88909997addbe on main and the 4.1.0 to 4.1.3 news at tag 4.1.3 (838877323facc4cc2b677fe817e203779c437bb1), 0.1 to 4.1.3; the GitHub releases reproduce them. The SHA-256 of each news file with a catalogued entry is in the file's header | GPL-2.0-only OR GPL-3.0-only |

Each file's header repeats its sources and records what was left out and why.

### What is catalogued

- Fixes only. New features, refactors, performance work without an issue, documentation, tests, build and dependency
  changes are not catalogued. A fix to packaging, a platform or the library's language is catalogued, as `n/a`.
- Where the notes are missing or incomplete (BencodeNET, bencode.py, node-bencode, bendy and serde_bencode), the commit
  history was read as well. A fix found only there is catalogued under the first release that contains it, unless it
  was made before the first release or fixes code first added in the same release cycle.
- libtorrent: only `ChangeLog` entries about bdecode, bencode, `entry`, `lazy_entry` and `lazy_bdecode`.
  Transmission: only entries about benc, `tr_variant` and `.torrent` parsing at the bencode level. Both files' headers
  name the entries this leaves out.
- bencode.py and Transmission are copyleft, so every input in their files was written for this catalogue and none of
  their test data is restated. From the permissive libraries a short input from a regression test is restated, and the
  test is named in `reason`.
- No library here is a port of another, so no fix is counted twice. bencode.py began as the `bencode` package on PyPI,
  itself the BitTorrent client's bencode module, so it is evidence of that lineage rather than an independent design.

### Columns

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes write it; `unreleased` for an entry not yet released |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (first 10 hex digits) or empty |
| `summary` | A short paraphrase of the entry |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it needs several |
| `kind` | `parse`, `reject`, `write`, `write-reject`, `roundtrip` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` (below); empty for the defaults |
| `input`, `expected` | The input and the expected result, escaped (below) |
| `expectation` | Where `expected` comes from: `upstream` (the fix's own test), `spec` (BEP 3), `derived` (worked out, with the working in `reason`) or `oracle:<tool>` |
| `reason` | Required on every row: the upstream test or issue for a runnable row, the Bodu document a `dialect` row relies on, the exclusion an `n/a` row falls under |

Every file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII.
Anything else in `input` and `expected` is written with an escape: `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and
`\u{H...}` (the UTF-8 encoding of one Unicode scalar). Fields follow RFC 4180 quoting.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 76 | The row runs, and Bodu must do what the fix established |
| `dialect` | 16 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix; `reason` names the document |
| `n/a` | 43 | The fix concerns something Bodu does not have (a torrent or magnet-link model, a Rust or C++ API), memory management, a platform, packaging or performance; `reason` says which |
| `unknown` | 0 | No scenario could be found |

### How a row runs

A read is rendered as a **token transcript**: the tokens in order, separated by single spaces. `[` and `]` start and
end a list, `{` and `}` a dictionary, `k:<bytes>` is a dictionary key, `s:<bytes>` a byte string, and `i:<digits>` an
integer, exactly as its text appears between `i` and `e`. Bytes are escaped as above, and a space inside a value is
written `\x20`. For example `d3:cow3:moo4:spam4:eggse` renders as `{ k:cow s:moo k:spam s:eggs }`. Transcripts are
compared after each token's bytes are decoded, so `\u{E9}` and `\xC3\xA9` are the same.

- The **Reader** surface (the default) drives `Utf8BencodeReader` until `Read` returns `false`, and renders every value
  from `ValueSpan`: a key's or byte string's content, and an integer's text without its `i` and `e`.
- The **Document** surface parses with `BencodeDocument.Parse` and walks `RootElement`. A key is rendered from
  `BencodeProperty.GetNameBytes`, its exact bytes, and an integer's text from `GetRawBytes`.
- The **Node** surface parses with `BencodeNode.Parse` and walks the tree. The tree keeps no document order, so a
  dictionary renders in ascending bytewise key order, the order its writer uses, and rows that read through it use
  canonical input.

| Kind | Passes when |
|---|---|
| `parse` | Reading `input` succeeds and its transcript equals `expected` |
| `reject` | Reading `input` throws an exception whose type name is `expected` (`BencodeFormatException`) |
| `write` | Replaying `input` on `Utf8BencodeWriter` writes exactly the bytes `expected`, with every container closed |
| `write-reject` | Replaying `input` throws an exception whose type name is `expected` |
| `roundtrip` | `BencodeDocument.Parse` accepts `input`; copying it token by token from `Utf8BencodeReader` into `Utf8BencodeWriter` writes bytes that equal `expected` when `expected` is not empty, that `BencodeDocument.Parse` accepts, and whose transcript equals the input's |
| `unit` | Not run from the file: `input` names a test to be written in `Bodu.Text.Bencode.Test` (`<TestClass>.<Member>_When<Condition>_Should<Result>`), and `reason` gives its model, input and expected outcome |

The write notation reads a transcript as writer calls: `[` and `]` call `WriteStartList` and `WriteEndList`, `{` and
`}` call `WriteStartDictionary` and `WriteEndDictionary`, `k:` calls `WritePropertyName(ReadOnlySpan<byte>)`, `s:`
calls `WriteByteString`, and `i:` calls `WriteInteger(long)`, or `WriteInteger(ulong)` above `long.MaxValue`. Digits
outside both ranges have no typed overload and are offered to `WriteRawValue` with validation, which is how a row
holds Bodu to its documented integer range. An `i:` token must be canonical base-ten digits.

| Option | Applies to | Values |
|---|---|---|
| `Surface` | the read of a `parse`, `reject` or `roundtrip` row | `Reader` (default), `Document` or `Node` |
| `MaxDepth` | the reader, the document and the writer | a positive integer; Bodu caps it at 64 |
| `AllowUnsortedKeys` | the reader and the document | `true` or `false` (default) |
| `AllowDuplicateKeys` | the reader and the document | `true` or `false` (default) |
| `AllowMultipleRootValues` | the writer | `true` or `false` (default) |

### Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| bencodenet | 21 | 26 | 10 | 0 | 16 | 0 |
| bencode-py | 8 | 15 | 8 | 2 | 5 | 0 |
| node-bencode | 13 | 28 | 14 | 10 | 4 | 0 |
| bendy | 7 | 7 | 0 | 0 | 7 | 0 |
| serde-bencode | 10 | 18 | 11 | 3 | 4 | 0 |
| libtorrent | 11 | 21 | 17 | 0 | 4 | 0 |
| transmission | 7 | 20 | 16 | 1 | 3 | 0 |
| **Total** | **77** | **135** | **76** | **16** | **43** | **0** |

Of the 92 `applies` and `dialect` rows, 74 are runnable and 18 are `unit` rows.

### Status

`BencodeReleaseNoteCorpusTests` in `Bodu.Text.Bencode.Test` runs every runnable row from copies of these files
embedded under `Fixtures/ReleaseNotes/`, pins the counts above, and checks that each `unit` row names a test that
exists and that the copies match these files. Every row passes.

The first run against Bodu found these defects, each fixed test first:

- **#818.** `BencodeDocument` exposed a dictionary key only as `BencodeProperty.Name`, decoded as UTF-8 with U+FFFD, so
  a binary key, such as the info hashes that key a tracker's scrape response, could not be read back (bencode.py 3.0.0
  PR #14 case 3, node-bencode 4.0.0 PR #150 case 2). `BencodeProperty.GetNameBytes` and `NameEquals` and the
  `GetProperty(ReadOnlySpan<byte>)` overloads now give exact access.
- **#819.** The node tree, string-keyed dictionaries and extension data merged keys that are not valid UTF-8. They now
  refuse such a key, and dialect rows hold the node tree and the dictionary to that.
- **#820.** `Utf8BencodeReader.ValueSpan` held the previous byte string on integer and container tokens.
- **#821.** Two reader messages were left unformatted, and the out-of-range integer message named the wrong range.
- **#822.** The reader accepted empty input (Transmission 1.05 case 2). Every surface now rejects it with
  `BencodeFormatException`, and node-bencode 0.11.0 case 4 is a dialect row asserting that.
- **#823.** A null root value gave a different result for each kind of type. `BencodeSerializer` now throws
  `BencodeSerializationException` for one, and `SerializeToNode` returns `null`.
- **#824.** The package README and the concepts pages contradicted the code.

Bodu deliberately differs from two upstream fixes, and the rows say so as `dialect` rows. It throws for a null list
element, which node-bencode and serde_bencode drop (node-bencode 0.11.0 case 2, serde-bencode 0.1.3 case 2), because
dropping an element shifts every later one. It throws for a null root value, which they write as nothing
(node-bencode 0.11.0 case 3, serde-bencode 0.1.3 case 1), because BEP 3 has no null and an empty output is not a
document.
