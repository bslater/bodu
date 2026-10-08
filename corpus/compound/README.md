# Bodu compound-file validation corpus

External evidence used to validate `Bodu.IO.Compound`, the reader, editor and writer for the OLE2 / Compound File
Binary format, kept in the repository so that every reconciliation can be reproduced from committed files. The
specification Bodu follows is [MS-CFB]; it states the format in prose and publishes no test vectors, so the evidence
here is what other compound-file libraries learned by fixing their own defects.

The test project already holds a corpus of real files: `Bodu.IO.Compound/test/Fixtures/Reference/` carries 8 valid
and 33 malformed or unsupported files from compoundfiles (MIT) and olefile (BSD-2-Clause), with their provenance in its
`NOTICE.md`, and `CompoundMalformedFixtures` classifies each malformed file by the category Bodu reports. The catalogue
below adds no file to it: every one of its inputs is generated.

## `fixes/` - the release-note fix catalogue

The release notes of five compound-file libraries were read in full, and every fix they list that touches the container
format, the OLE property sets Bodu reads, or the parts of their API that Bodu shares, is catalogued: one file per
library, `fixes/<library>-fixes.csv`, and two for Apache POI, whose compound-file layer (POIFS) and property-set layer
(HPSF) are separate modules, with one row per fix, or one per case where a fix needs several. A scenario is, first, the
regression test the fix added, read from its commit, and otherwise the scenario its issue or commit describes. Each row
records the version, the reference (an issue, a pull request or a commit) and a short summary in our own words, then a
class, and, for a runnable row, the input and the result Bodu must give.

### Sources

| File | Library | Language | What was read | Licence |
|---|---|---|---|---|
| `olefile-fixes.csv` | [olefile](https://github.com/decalage2/olefile) | Python | `CHANGELOG.md` at 7c0f1ce6f27311e03f25a997024846b58fa9826f (SHA-256 dc2d568fa26c4f77ccef45018aa42c66ae6cab3a35930356b7666d974fcc6e2d), every entry from 0.47 down to the PIL 1.1.6 history it opens with, each read in the commit that made it; issues #27, #31, #61, #70, #96, #103, #120, #142 and #158, read 2026-10-08 | BSD-2-Clause AND MIT-CMU |
| `openmcdf-fixes.csv` | [OpenMcdf](https://github.com/ironfede/openmcdf) | C# | `CHANGELOG.md` at 8e2907a2717cb43212b1be249f6ed5a6f824bfc0 (SHA-256 e71936330303d524a94359c815a5258bf149651f13e98f73ea9b22e95ac28a5f), 1.0 to 3.3.0, and the GitHub releases, pages 1-4 (v2.1.1.29598 to v3.3.0); each fix read in the commits of its pull request; issues #14, #15, #52, #53, #68, #69, #70, #75, #76 and #256, read 2026-10-08 | MPL-2.0 |
| `cfb-fixes.csv` | [cfb](https://github.com/mdsteele/rust-cfb) | Rust | There is no changelog and there are no GitHub releases, so the commit history: master at 9b4e6479144009101eced6623aca73d4e762bca1 (2026-09-20), v0.1.0 to v0.15.0, each fix under the first version tag that contains it; issues #30 and #34, read 2026-10-08 | MIT |
| `poi-fixes.csv` | [Apache POI](https://github.com/apache/poi), POIFS | Java | The change log pages [changes.html](https://poi.apache.org/changes.html) (SHA-256 0160cc45b13a338988d64dc6863f72c1ecef9ff133d8b2c8d5c771ee9d6bdca3, 4.0.0 to the unreleased 6.0.0), [changes-3x.html](https://poi.apache.org/devel/history/changes-3x.html) (SHA-256 3332f9f5bf4288220d1da148f39aa8b72085628c9748d52dde2543a1dc37ab5f, 3.0-alpha1 to 3.17) and [changes-pre3x.html](https://poi.apache.org/devel/history/changes-pre3x.html) (SHA-256 b9dea0044cb83d867f1177d074026538dec234879f2af088f7d9361790cc9aac, 0.1 to 2.5.1-FINAL), read 2026-10-08; each fix read in its commit in the Git mirror, trunk at ae62bb5116b9aee19ebd5834e3a82066132c9f7f | Apache-2.0 |
| `poi-hpsf-fixes.csv` | [Apache POI](https://github.com/apache/poi), HPSF | Java | The same three change log pages, read 2026-10-08; each fix read in its commit in the Git mirror, trunk at ae62bb5116b9aee19ebd5834e3a82066132c9f7f | Apache-2.0 |
| `compoundfiles-fixes.csv` | [compoundfiles](https://github.com/waveform80/compoundfiles) | Python | `docs/changelog.rst` at dd68dec26ba04315ee6df4170946b6a6daefa54f (SHA-256 2ae046cb0a04134a978cbb689d0f4ecddcb17bc24ccb171a50fa9846a7da5ab7), 0.1 to 0.3, with the commits of #2, read 2026-10-08 | MIT |

Each file's header repeats its sources and records what was left out and why. The specifications were read as the
Microsoft Open Specifications DOCX files, kept outside the repository:

| Specification | Version | Source | SHA-256 |
|---|---|---|---|
| [MS-CFB] Compound File Binary File Format | 12.0, 2024-04-23 | `https://winprotocoldocs-bhdugrdyduf5h2e4.b02.azurefd.net/MS-CFB/%5bMS-CFB%5d-240423.docx` | 2d650184072a148ba98ad0b68072fd5ad7780e46f3528d7f263f3127b2dadab5 |
| [MS-OSHARED] Office Common Data Types and Objects Structures | 2025-11-13 | `https://officeprotocoldocs-f5hpbjgea6b8gneq.b02.azurefd.net/files/MS-OSHARED/%5bMS-OSHARED%5d-251113.docx` | 3a17ec72868a7ba8c9c987995c8902e832a42d66eecbf149101a4e6c7255f87c |

[MS-OSHARED] is cited only by the property-set rows (document parts, heading pairs and the summary information
streams), which OpenMcdf's, olefile's and POI's HPSF notes include and which Bodu reads through `OlePropertySet`.

### What is catalogued

- Fixes only. New features, refactors, performance work, documentation, tests, build and dependency changes are not
  catalogued, and each file's header names the borderline entries it leaves out. A fix to packaging, a platform or the
  library's language is catalogued, as `n/a`.
- cfb has no release notes, so its commit history was read instead; for the other four the release notes govern, and
  a commit is read to find a fix's scenario. compoundfiles' one entry for several fixes (#2) is broken down by its
  commits.
- Apache POI: POIFS, its compound-file layer, in `poi-fixes.csv`, and HPSF, its property-set layer, in
  `poi-hpsf-fixes.csv`. Every fix-marked entry the change log files under either module is catalogued, with the fixes
  filed under another module whose commit changes POIFS or HPSF or whose text names it. The application modules
  (HSSF, HWPF, HSLF, HSMF) are not. ASF Bugzilla now admits only logged-in users, so POI's bug reports were not read;
  each entry was traced to the commit that fixed it.
- OpenMcdf is MPL-2.0, so every input in its file was written for this catalogue and none of its test data or code is
  restated. From the permissive libraries a short scenario from a regression test may be restated, with the test named
  in `reason`.
- No library here is a port of another, so no fix is counted twice. olefile grew out of PIL's OleFileIO, and its file
  catalogues that history under olefile's own name.
- compoundfiles' test files are the reference fixtures above. Its rows restate the scenarios as generators and are the
  only coverage of those the fixture table does not hold.

### Columns

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes write it; `unreleased` for an entry not yet released |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (first 10 hex digits) or empty |
| `summary` | A short paraphrase of the entry; POI's carry the Bugzilla number, since a Bugzilla bug is not an issue in its repository |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it needs several |
| `kind` | `parse`, `reject`, `write-reject`, `roundtrip` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` (below); empty for the defaults |
| `input`, `expected` | The input and the expected result, escaped (below) |
| `expectation` | Where `expected` comes from: `upstream` (the fix's own test), `spec` ([MS-CFB]), `derived` (worked out, with the working in `reason`) or `oracle:<tool>` |
| `reason` | Required on every row: the upstream test or issue for a runnable row, the Bodu document or specification section a `dialect` row relies on, the exclusion an `n/a` row falls under |

A `reason` cites Bodu's documents by file name and line, as they stood at 378a6099db: the `.cs` files are under
`Bodu.IO.Compound/src/` (`IO.Compound/`, or `IO.Compound.Builders/`, `IO.Compound.Internal/` and
`IO.Compound.PropertySets/` for `CompoundBuildOptions.cs`, `CompoundContainerLayout.cs` and `OlePropertySection.cs`),
`concepts.md` is `docs/docs/io-compound/concepts.md`, and the guides (`reading-compound-files.md`,
`editing-in-place.md`, `custom-property-sets.md`) are under `docs/guides/io-compound/`.

Every file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII.
Anything else in `input` and `expected` is written with an escape: `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and
`\u{H...}` (the UTF-8 encoding of one Unicode scalar). Fields follow RFC 4180 quoting. Within a file, no two rows share
a name, which is `<library> fix <version> <reference>: <summary>`, followed by ` (case <n>)` when the row has a case.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 276 | The row runs, and Bodu must do what the fix established |
| `dialect` | 32 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix; `reason` names the document or the [MS-CFB] section |
| `n/a` | 85 | The fix concerns something Bodu does not have (encryption, VBA, Ole10Native, OOXML packages, file transactions, thumbnails, an API Bodu does not offer), the library's language or runtime, a platform, packaging or performance; `reason` says which |
| `unknown` | 4 | No scenario could be found; `reason` links what was read |

### How a row runs

A read is rendered as a **listing**, one line per entry, depth first in the order the API enumerates, lines separated
by a line feed with none after the last. A storage renders as `<path>\tstorage`, a stream as
`<path>\tstream\t<length>\t<hash>`. The root's path is `/`; a child's path is its parent's path, a `/` (omitted after
the root's) and its name exactly as `CompoundEntryInfo.Name` gives it. The length is `CompoundEntryInfo.Length` in
decimal, and the hash is the first 16 lowercase hexadecimal digits of the SHA-256 of the bytes
`CompoundStream.ReadAllBytes` returns for the stream opened by its name with `CompoundStorage.OpenStream`. Children are
listed as `EnumerateEntries` yields them, and a storage's children are rendered through the matching
`EnumerateStorages` element. A read opens the container with `CompoundFile.Open(new MemoryStream(bytes, false),
options)` and reads every stream, so a failure that surfaces only when a stream is read counts as a failure to read.

| Kind | Passes when |
|---|---|
| `parse` | Reading `input` succeeds and its listing equals `expected` |
| `reject` | Reading `input` throws an exception whose type name is `expected`'s part before any `:`; when `expected` carries `:<category>`, the exception is a `CompoundFileFormatException` whose `Category` is that `CompoundFileError` member |
| `write-reject` | Running `input`'s edit script, or committing it, throws an exception whose type name is `expected` |
| `roundtrip` | Without an edit script: `input` reads (the first listing), `CompoundStorageBuilder.FromFile` of the open file serialized with `ToArray()` reads again, and the two listings are equal, and equal `expected` when it is not empty. With an edit script: the script runs and commits, the committed file renders `expected`, and copying it the same way renders the same again |
| `unit` | Not run from the file: `input` names a test to be written in `Bodu.IO.Compound.Test` (`<TestClass>.<Member>_When<Condition>_Should<Result>`; where the test already exists, `reason` says so), and `reason` gives its model, input and expected outcome |

There are no `write` rows: Bodu's writer is checked by reading back what it wrote, so a write-side fix is a
`roundtrip` row, or a `unit` row when the bytes themselves matter (a header field, an unused directory slot).

| Option | Maps to | Values |
|---|---|---|
| `ValidationLevel` | `CompoundFileOptions.ValidationLevel` | `Strict`, `Compatible` (default) or `Minimal` |
| `ReadStrategy` | `CompoundFileOptions.ReadStrategy` | `Buffered` (default), `Streaming` or `Auto` |

Options apply to every read a row makes, starting from `new CompoundFileOptions()`. No row uses `ReadStrategy`; the
streaming reader is reached through `unit` rows.

**Validation levels.** Most libraries here have a strict and a lenient mode (olefile's defect levels, OpenMcdf's
`StrictValidation`, cfb's `Strict` and `Permissive`); POI and compoundfiles have one. A check a library applies only
in its strict mode runs at `ValidationLevel=Strict`, and a deviation its default mode accepts runs at the default
level. A recovery runs at `ValidationLevel=Minimal`. Bodu's levels are its documented leniency
(`CompoundValidationLevel.cs:13-49`; the concepts page, Validation level), so a row that holds Bodu to a level cites
that documentation, and a `Strict` row that rejects a violation of an [MS-CFB] MUST cites both.

**Categories.** A `reject` row names a category only when the fix is about that class of defect (a cycle, a missing
mini FAT, a wrong signature, a truncated file); otherwise it expects `CompoundFileFormatException` alone.

#### Inputs

Every input is generated. `input` is a `gen:` pipeline, optionally followed by an edit script, or `new:V3` / `new:V4`
followed by an edit script. (The rules also allow `bytes:<escaped>` for a literal container and `file:<name>` for an
upstream sample kept outside the repository; no row uses either, so no file awaits a place under
`Fixtures/Reference`.)

A **`gen:` pipeline** is a list of steps separated by `|`, each `Name` or `Name(arg,arg,...)` (arguments split at
commas outside parentheses). It opens with `V3` or `V4`, continues with builder steps, and ends with patch steps:

1. **Build.** `CompoundStorageBuilder.CreateRoot()`, then each builder step in order, then
   `ToArray(new CompoundBuildOptions { Version = CompoundFileVersion.V3 or V4 })`.
   - `Storage(path)` adds a storage; `Stream(path,content)` adds a stream. A path is `/` followed by names separated by
     `/`; every parent named must already exist.
   - `content` is `<n>`, the `n` bytes of the **catalogue pattern** (byte `i` is `(i * 31 + 7) mod 256`), or
     `<n>*<b>`, `n` copies of the byte `b`. Numbers are decimal or `0x` hexadecimal.
2. **Read the layout.** The container as built is read once, before any patch, to resolve every name a patch uses: the
   sector size from the header's sector shift, the FAT sectors (the header's 109 DIFAT slots, then the DIFAT sector
   chain, skipping values of `0xFFFFFFFA` and above), the FAT, the directory chain from the header's First Directory
   Sector Location, the mini FAT chain from its First Mini FAT Sector Location, and the mini FAT. Patches therefore
   never depend on what an earlier patch changed, and no row assumes where Bodu's writer puts a structure.
3. **Patch.** Each patch step rewrites bytes of the built container, in order. Integers are written little-endian,
   keeping the low-order bytes of the value.

| Patch step | Effect |
|---|---|
| `Header(field,value)` | Writes a header field: `MinorVersion` (0x18, 2 bytes), `MajorVersion` (0x1A, 2), `ByteOrder` (0x1C, 2), `SectorShift` (0x1E, 2), `MiniSectorShift` (0x20, 2), `DirectorySectors` (0x28, 4), `FatSectors` (0x2C, 4), `FirstDirectorySector` (0x30, 4), `TransactionSignature` (0x34, 4), `MiniStreamCutoff` (0x38, 4), `FirstMiniFatSector` (0x3C, 4), `MiniFatSectors` (0x40, 4), `FirstDifatSector` (0x44, 4), `DifatSectors` (0x48, 4), or `Difat0` to `Difat108` (0x4C + 4k, 4) |
| `Set(offset,width,value)` | Writes `value` as a `width`-byte integer (1, 2, 4 or 8) at file offset `offset` |
| `Hex(offset,hex)` | Writes the bytes the hexadecimal string gives at file offset `offset` |
| `Dir(entry,field,value)` | Writes a field of an entry's 128-byte record: `NameLength` (64, 2), `Type` (66, 1), `Color` (67, 1), `Left` (68, 4), `Right` (72, 4), `Child` (76, 4), `StateBits` (96, 4), `Created` (100, 8), `Modified` (108, 8), `Start` (116, 4), `Size` (120, 8), `SizeLow` (120, 4) or `SizeHigh` (124, 4) |
| `Name(entry,text)` | Writes `text` as UTF-16LE into the 64-byte name field, zero-filling the rest, and sets the name length to its byte count plus 2; an empty `text` leaves the terminator alone |
| `Fat(sector,value)` | Writes the FAT entry of `sector` |
| `MiniFat(minisector,value)` | Writes the mini FAT entry of `minisector` |
| `Truncate(length)` | Cuts the container to `length` bytes, which must not lengthen it |
| `Append(count,byte)` | Appends `count` copies of `byte` |
| `GrowFat(n)` | Adds `n` FAT sectors (below); the container must be as built and have no DIFAT sector |
| `MoveLast(entry)` | Moves the last sector of an entry's regular chain to a new sector at the end (below) |
| `MoveLast(dir)` | The same move for the last sector of the directory chain |

An **entry** is a path, resolved by walking each storage's red-black tree from its Child link (left links first, then
the node, then right links) and comparing names ordinally, or `#<sid>` for a stream identifier; `/` is the root. An
**expression** is terms joined by `+` and `-`, each a number (decimal or `0x` hexadecimal), a symbol (`ENDOFCHAIN`
0xFFFFFFFE, `FREESECT` 0xFFFFFFFF, `FATSECT` 0xFFFFFFFD, `DIFSECT` 0xFFFFFFFC, `MAXREGSECT` 0xFFFFFFFA, `NOSTREAM`
0xFFFFFFFF), a measure of the container as built (`length`, its length in bytes, and `sectors`, its whole sectors after
the header, which is the number the next appended sector takes) or a function:

| Function | Value |
|---|---|
| `sector(entry,k)` | The `k`-th sector of the entry's regular chain (the root's is the mini stream; a stream must be 4096 bytes or more); a negative `k` counts from the end |
| `minisector(entry,k)` | The `k`-th mini sector of a stream's mini chain (the stream must be under 4096 bytes) |
| `sid(entry)` | The entry's stream identifier |
| `fatsector(k)`, `dirsector(k)`, `minifatsector(k)` | The `k`-th sector of the FAT (in DIFAT order), of the directory chain and of the mini FAT chain |
| `offset(s)` | The file offset of sector `s`, `(s + 1) * sector size` |
| `entry(entry)` | The file offset of the entry's directory record |

**`GrowFat(n)`**, with `S` the sector size, `E = S / 4`, `F` the FAT sectors as built and `L` the sectors as built:
the new FAT sectors are `L` to `L + n - 1`, each `S` bytes of 0xFF; the `T = F + n` FAT sector numbers go, in order,
into the header's 109 DIFAT slots (the rest `FREESECT`) and then into `D = ceil(max(0, T - 109) / (E - 1))` DIFAT
sectors `L + n` to `L + n + D - 1`, each holding `E - 1` numbers (unused slots `FREESECT`) and then the next DIFAT
sector, or `ENDOFCHAIN` in the last. The FAT marks the new FAT sectors `FATSECT` and the DIFAT sectors `DIFSECT`; these
sectors must fall within the FAT as built; the header's Number of FAT Sectors, First DIFAT Sector Location and Number
of DIFAT Sectors are set.

**`MoveLast(entry)`**, with `L` the sectors as built and `c` the chain: the bytes of sector `c[-1]` are copied to a
new sector `L` at the end; the FAT entry of `c[-2]` (or, for a one-sector chain, the entry's Start field) becomes `L`,
the FAT entry of `L` becomes `ENDOFCHAIN`, and that of `c[-1]` becomes `FREESECT`. **`MoveLast(dir)`** does the same
for the directory chain `d`, writing the header's First Directory Sector Location when the directory has one sector.
Sector `L` must fall within the FAT as built, and the container must be as built. Followed by `Truncate(length + k)`,
either leaves a final sector of `k` bytes.

An **edit script** follows ` => ` and is a list of edits separated by `|`. Its base is `new:V3` or `new:V4`
(`CompoundFile.Create(stream, leaveOpen: true, new CompoundBuildOptions { Version = ... })`) or a `gen:` pipeline,
whose container is opened with `CompoundFile.Open(stream, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true)`.
Each edit's path is resolved by opening its parent storages from the root with `CompoundStorage.OpenStorage`; the
script ends with an implicit `Commit`.

| Edit | Call |
|---|---|
| `CreateStorage(path)` | `parent.CreateStorage(name)` |
| `CreateStream(path,content)` | `parent.CreateStream(name, content)` |
| `Write(path,content)` | `OpenStream(name, FileMode.Create, FileAccess.Write)`, then `Write(content)` |
| `Append(path,content)` | `OpenStream(name, FileMode.Append, FileAccess.Write)`, then `Write(content)` |
| `Overwrite(path,offset,content)` | `OpenStream(name, FileMode.Open, FileAccess.ReadWrite)`, `Position = offset`, then `Write(content)` |
| `SetLength(path,length)` | `OpenStream(name, FileMode.Open, FileAccess.ReadWrite)`, then `SetLength(length)` |
| `Delete(path)` | `parent.Delete(name)` |
| `Rename(path,newName)` | `parent.Rename(name, newName)` |
| `Commit`, `Revert` | `file.Commit()`, `file.Revert()` |
| `Reopen` | `Commit`, dispose, and open the committed bytes for update again |

### Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| olefile | 56 | 70 | 37 | 12 | 20 | 1 |
| openmcdf | 135 | 165 | 120 | 12 | 32 | 1 |
| cfb | 49 | 71 | 66 | 1 | 4 | 0 |
| poi | 50 | 59 | 32 | 4 | 22 | 1 |
| poi-hpsf | 20 | 21 | 14 | 1 | 5 | 1 |
| compoundfiles | 3 | 11 | 7 | 2 | 2 | 0 |
| **Total** | **313** | **397** | **276** | **32** | **85** | **4** |

Of the 308 `applies` and `dialect` rows, 192 are runnable and 116 are `unit` rows.

### Status

The catalogue is not yet embedded in `Bodu.IO.Compound.Test`. A scratch harness implementing the rules above ran
every runnable row against `Bodu.IO.Compound` at 378a6099db, each with a 10-second timeout; no row timed out or
crashed, 152 passed and 40 failed. The failures, which are reported here and not yet fixed, fall into these groups:

- **The Strict level accepts violations of [MS-CFB] MUSTs** (24 rows), although it is documented to reject every
  non-conformant condition (`CompoundValidationLevel.cs:34`; concepts page, Validation level): a header CLSID or
  reserved bytes that are not zero, a major version of 5, a version-3 Number of Directory Sectors that is not 0 and a
  version-4 one that does not count the chain, FAT entries past the end of the file that are not `FREESECT`, FAT and
  DIFAT sectors not marked `FATSECT` and `DIFSECT`, a DIFAT chain ending in `FREESECT`, two consecutive red nodes, a
  root not named `Root Entry`, a stream CLSID, stream times or root creation time that are not zero, a storage start
  sector that is not zero, names whose length is wrong or that lack their terminator, and two streams sharing sectors
  (olefile 0.16 and 0.12 cases 1 to 4, OpenMcdf PR #482, #454 and #437, cfb PR #91, ac9d592f05, PR #53, PR #50,
  50a3c3d3e5, f894aba526, 16481f5c28 and f5c29eb014, compoundfiles #2 cases 2 and 5).
- **The header's mini stream cutoff is used as written** (2 rows): a cutoff of 0 reads a mini stream from regular
  sectors, so its bytes come back wrong without an error (olefile 0.43 #27), and Strict accepts a cutoff other than
  4096 (olefile 0.31).
- **A final sector shorter than a sector is rejected at every level** (5 rows), with `SectorOutOfRange`, even when the
  file holds every byte its streams declare. Every library here reads such files (olefile 0.18, OpenMcdf 1.5.2, cfb
  0.4.0 #1, POI 3.1-final and 3.8-FINAL, the last a cut directory sector). Bodu documents that file truncation is
  always validated (`CompoundValidationLevel.cs:13-15`), but [MS-CFB] 2.3 requires a file whose FAT uses sector N to
  be at least (N + 1) x (Sector Size) bytes long, the offset where sector N starts, so the rows stay `applies`.
- **Header counts are not checked against what they count** (2 rows): a Number of DIFAT Sectors of 0 with a DIFAT
  sector in use (OpenMcdf PR #388 case 3) and a Number of FAT Sectors of 0x20202001 in a 2560-byte file (POI 3.6),
  both read without error at the default level, and at Strict too.
- **A duplicate name throws `CompoundFileSerializationException`** (2 rows), where `CompoundStorage.CreateStream` and
  `CreateStorage` document `ArgumentException` (OpenMcdf 1.5.1, the second case differing only in case).
- **The category differs from the defect** (3 rows): a mini stream whose start lies past the mini FAT, and a header
  naming no mini FAT for a mini stream, report `SectorOutOfRange` rather than `InvalidMiniFat` (cfb 0.7.0 #16,
  compoundfiles #2 case 8); a version-4 file cut to 1000 bytes reports `InvalidDifat` rather than `TruncatedFile` (cfb
  0.7.0 PR #24 case 1).
- **A directory entry of an invalid type in the tree fails the file at the default level** (1 row): the entry is
  skipped when the directory is parsed, as documented (`CompoundValidationLevel.cs:40-41`), but the tree walk then
  reports its sibling link as `DirectoryCycle` (POI 3.12, 57851).
- **`Minimal` does not recover a chain that runs past the end of the file** (1 row): a stream whose last sector lies
  beyond the end throws `SectorOutOfRange` at `Minimal`, which is documented to stop such a chain and zero-pad it
  (`CompoundValidationLevel.cs:46-47`; compoundfiles #2 case 7). A chain that leaves the FAT is recovered (olefile 0.44
  cf8bd94140 case 2 passes).

Where a scratch probe or the code settled a `unit` row, its reason says what Bodu does today. Thirteen of them
describe behaviour Bodu lacks:

- Unused directory slots are written with zero sibling and child links rather than `NOSTREAM` ([MS-CFB] 2.6.3;
  OpenMcdf PR #376 case 2, cfb 32983e7d16 case 2).
- A version-3 container is written past 2 GB ([MS-CFB] 2.9; OpenMcdf PR #274, and #14 for update mode), and a
  version-4 container past 2 GB chains stream data through the range lock sector ([MS-CFB] 2.8; OpenMcdf PR #270).
- A `CompoundFileVersion` that is neither `V3` nor `V4` is written as version 3 rather than rejected (OpenMcdf PR #314
  case 2).
- Document parts and heading pairs written as unaligned strings, as [MS-OSHARED] 2.3.3.1.12 to 2.3.3.1.15 and
  2.3.3.2.2.1 require, are dropped or misread (OpenMcdf PR #108, POI HPSF 3.8-FINAL).
- A string or dictionary length whose byte count overflows `Int32` escapes from `OlePropertySet.Parse` as
  `OverflowException` (POI HPSF PR #1064 case 1 and PR #1345).
- A `VT_ARRAY` value is read as a scalar of its element type rather than dropped (POI HPSF PR #1064 case 2, OpenMcdf
  PR #371 case 2).
- A Unicode string that begins on the next 4-byte boundary, as some writers place it, is dropped (POI HPSF
  3.10-beta2).
