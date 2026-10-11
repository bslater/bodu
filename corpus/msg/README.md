# Outlook .msg release-note fix catalogue

`fixes/` holds one file per `.msg` library, `fixes/<library>-fixes.csv`: every defect fix the library's release notes
list (or, where there are none, its fix commits), turned into a row that states the scenario of the fix and what Bodu
must do with it. The rows hold `Bodu.Formats.Outlook.Msg` (`OutlookMessage`, its recipients and attachments) and the
shared MAPI value model and compressed-RTF decoder of `Bodu.Formats.Outlook` to those fixes. A row that can run is a
`parse` or `reject` case over a generated message or a short byte string; a scenario that needs typed code (a file
path, a disposal sequence, a named-property lookup) is a `unit` row naming the test that covers it; a fix that cannot
touch Bodu is recorded as `n/a` with the reason. Nothing here is copied from the libraries: each row restates the
fix's scenario in a few words and builds its own input, and from the permissively licensed libraries at most a short
test input is restated, with the test named in `reason`. No row uses an upstream sample file (there is no `file:`
input), and extract-msg, which is GPL-3.0, contributes derived rows only.

## Sources

| Library | File | Language, licence | What was read | Versions |
|---|---|---|---|---|
| [MsgReader](https://github.com/Sicos1977/MSGReader) | `msgreader-fixes.csv` | C#, MIT | GitHub Releases pages 1 to 16 (read 2026-10-08), and, as there is no changelog file, every commit of `master` at `70dd199e30` that touches the `.msg` reader | 1.0 to 6.1.4 |
| [extract-msg](https://github.com/TeamMsgExtractor/msg-extractor) | `extract-msg-fixes.csv` | Python, GPL-3.0-or-later | `CHANGELOG.md` at `18172ea473` (SHA-256 `b194cfcb...a4980b`) for v0.20.7 on, and the commit history at the same commit for the tags before it | 0.1 to v0.56.1 |
| [@kenjiuno/msgreader](https://github.com/HiraokaHyperTools/msgreader) | `kenjiuno-msgreader-fixes.csv` | TypeScript, Apache-2.0 | no changelog and no releases, so every commit of `master` at `3a5a9359a0` that touches `src/`, with the 59 closed issues; versions from the npm registry (read 2026-10-08) | 1.1.3 to 1.28.0 |
| [compressed_rtf](https://github.com/delimitry/compressed_rtf) | `compressed_rtf-fixes.csv` | Python, MIT | no changelog, so all 67 commits of `master` at `d672c4df1e` with every issue and pull request; versions from PyPI (read 2026-10-08) | 1.0.0 to 1.0.7, and the unreleased 1.0.8 |
| [Apache POI HSMF](https://poi.apache.org/changes.html) | `poi-hsmf-fixes.csv` | Java, Apache-2.0 | the change pages `changes.html` (SHA-256 `0160cc45...6bdca3`), `devel/history/changes-3x.html` (`3332f9f5...37ab5f`) and `devel/history/changes-pre3x.html` (`b9dea004...cc9aac`), read 2026-10-08, with each fix's commit and test in `apache/poi` at `ae62bb5116` | 3.0.2-BETA1 (HSMF's first entry) to 5.5.1, and the unreleased 6.0.0 |

Each file's header lines give the full commit and digest, every page read, and the file's own scope notes. Every
scenario was taken from the fix's regression test, read with `git show` from a partial clone, or, without a test, from
its issue or commit; GitHub Releases and issue pages were read with WebFetch. ASF Bugzilla now requires a login, so
POI's bug reports could not be read: its scenarios come from the commits and tests, and its `reference` column gives
the fix's commit with the bug number in the summary.

## What is catalogued

Every entry that fixes a defect or addresses an issue, security fixes included, from the first release to the newest,
and entries listed for an unreleased version (`unreleased`). Features, refactors, documentation, translations, build,
CI and test-only changes and routine dependency updates are not; when an entry mixes a feature and a fix, the fix is
catalogued, and an entry filed as an addition is catalogued when it answers a reported defect. Where a library has no
release notes, its commit history is read and each fix is catalogued under the first release that contains it; a fix
to code first added in the same release cycle is not catalogued.

The scope is the `.msg` reader of each library: MsgReader's EML reader, RTF-to-HTML converter and the files
`ExtractToFolder` writes, and POI's components other than HSMF, are left out entirely (the POIFS entry made for HSMF
is kept). Fixes inside each library's own `.msg` features that Bodu does not have (saving to HTML or PDF, writing
messages, command lines, MAPI emulation) are catalogued as `n/a` with the reason.

A fix becomes one row, or one row per case when its scenario has several distinct inputs; adjacent rows with the same
version, reference and summary are one fix, numbered `1`, `2`, and so on.

## Row format

The columns are `library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason`, in
RFC 4180 quoting, one row per line, every character printable ASCII. `input` and `expected` use the escapes `\n`,
`\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and `\u{H...}` (the UTF-8 encoding of a scalar value); a decoded field is a
byte sequence.

| Class | Meaning |
|---|---|
| `applies` | The row runs, and Bodu must do what the fix established. |
| `dialect` | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix; `reason` cites the Bodu document, or the specification Bodu follows, that states it. |
| `n/a` | The fix concerns something Bodu does not have or cannot be affected by; `reason` names it. |
| `unknown` | No scenario could be found; `reason` names what was read. |

| Kind | The row passes when |
|---|---|
| `parse` | reading `input` with the row's options succeeds and its rendering equals `expected` |
| `reject` | reading `input` and rendering it throws; `expected` is the exception's type name, `OutlookMsgFormatException` in every row here |
| `unit` | (not run as data) `input` names the test that covers the scenario, `<TestClass>.<Member>_When<Condition>_Should<Result>`, and `reason` describes its model, input and outcome |

`write`, `write-reject` and `roundtrip` rows do not occur: Bodu reads `.msg` files and does not write them. A
`reject` row renders the input in full, so a failure in something read lazily (recipients, attachments, a nested
message, a body) counts.

`expectation` says where `expected` comes from: `upstream` (the fix's own test), `spec` (MS-OXMSG, MS-OXCMSG,
MS-OXCDATA, MS-OXPROPS, MS-OXRTFCP or MS-CFB, cited in `reason`), or `derived` (worked out, with the working in
`reason`).

### Options

`options` is `Name=Value` pairs separated by `;`. The field is split on `;` first, then each pair at its first `=`,
and each value is decoded with the escapes above. Every option except `Surface` sets the property of the same name on
the `OutlookMessageReaderOptions` the row is read with; an option left out keeps the documented default, shown here.

| Option | Values | Default |
|---|---|---|
| `Surface` | `Message`, `Conveniences`, `CompressedRtf` (see Rendering) | `Message` |
| `ValidationLevel` | `Compatible`, `Strict`, `Minimal` | `Compatible` |
| `ReadStrategy` | `Buffered`, `Streaming`, `Auto` | `Buffered` |
| `DecompressRtf` | `true`, `false` | `true` |
| `MaxEmbeddedMessageDepth` | a positive integer | `16` |
| `MaxDecompressedRtfBytes` | a positive integer | `67108864` |
| `MaxInlineAttachmentBytes` | a positive integer | `1048576` |

## Inputs

`input` is `gen:` followed by a message specification, or `bytes:` followed by the input bytes themselves (for the
compressed-RTF decoder, and for inputs that are not compound files). The field is decoded with the escapes first; the
text after `gen:` is then read one byte to one character (Latin-1).

### `gen:Msg(...)`

`gen:Msg(items)` builds a `.msg` file with `MsgFixtureBuilder` (the test project's builder over `Bodu.IO.Compound`'s
`CompoundFile.Create`: compound file version 3, 512-byte sectors, a 4,096-byte mini-stream cutoff). `items` is a list
separated by `;`, and the same list describes the root message, a recipient, an attachment, an embedded message or any
other storage. A value runs to the next `;` or `)`; inside it `%HH` is the byte `HH` and every other character is its
own byte, so `;`, `)`, `%`, and `|` or `+` where they would separate, are written `%3B`, `%29`, `%25`, `%7C` and `%2B`.
Stream and storage names are taken literally.

| Item | What it adds |
|---|---|
| `TTTTTTTT=value` | a property with tag `TTTTTTTT` (eight hexadecimal digits): its record and, by type, its streams (below) |
| `rec:TTTTTTTT=V` or `rec:TTTTTTTT=V/F` | a raw record: tag, flags `F` (hexadecimal, default `00000006`) and the 8-byte value field holding `V` (up to 16 hexadecimal digits) little-endian; no stream |
| `size:TTTTTTTT=n` | sets the low 32 bits of the value field (the Size field) of the last record with that tag to `n` |
| `flags:TTTTTTTT=n` | sets the flags of the last record with that tag to `n` |
| `stream:NAME=bytes` | a stream `NAME` holding `bytes` (below); of two streams with the same name, compared case-insensitively, the later one replaces the earlier, whether a `stream:` item or a typed property wrote it |
| `recip(items)` | a child storage `__recip_version1.0_#XXXXXXXX`, where `XXXXXXXX` is the number of children of this storage whose names already start with `__recip_version1.0_#`, in eight uppercase hexadecimal digits |
| `attach(items)` | the same with `__attach_version1.0_#` |
| `embed(items)` | the records `37050003` (value 5, `afEmbeddedMessage`) and `3701000D` (value 0) in this storage, and the child storage `__substg1.0_3701000D` described by `items` |
| `storage:NAME(items)` | a child storage `NAME` described by `items` |
| `nameid(entries)` | the named-property mapping storage `__nameid_version1.0` (below) |
| `noprops` | no `__properties_version1.0` stream in this storage |
| `props=bytes` | the property stream is exactly `bytes` |
| `hdr=bytes` | the property stream's header is `bytes` instead of the default |
| `tail=bytes` | `bytes` after the last record of the property stream |
| `cut=n`, `unallocated=n`, `reverse:NAME`, `move:minifat` | whole-file transforms, at the top level only (below) |

A storage's property stream is its header, then one 16-byte record per property in the order the items give them
(the tag and the flags as 32-bit little-endian integers, then the 8-byte value field), then the tail. The default
header is 32 zero bytes for the root, 24 for `__substg1.0_3701000D` and 8 for any other storage; in the root's and an
embedded message's header the 32-bit fields at offsets 8 and 16 hold the number of children whose names start with
`__recip_version1.0_#`, and those at 12 and 20 the number starting with `__attach_version1.0_#`. Every record written
by a typed property has flags `00000006` (readable and writable). Each storage holds its property stream first, then
its streams in the order given, then its child storages in the order given.

A typed property is encoded by its type, the tag's low 16 bits:

| Type | Value written as | Value field of the record | Stream `__substg1.0_TTTTTTTT` |
|---|---|---|---|
| `001F` | text (UTF-8 after `%HH`) | its UTF-16 byte count plus 2 | the text in UTF-16LE, no terminator |
| `001E` | text | its byte count plus 1 | the bytes, no terminator |
| `0102` | hexadecimal digits | the byte count | the bytes |
| `0048` | a GUID in `D` form | 16 | the GUID in the MS-OXCDATA byte order (the first three fields little-endian) |
| `000D` | an integer, empty meaning 0 | the integer | none |
| `0002`, `0003`, `000A` | a decimal or `0x` integer | the 16- or 32-bit two's-complement value | none |
| `0014`, `0000`, `0001` | a decimal or `0x` integer | the 64-bit value | none |
| `000B` | `true`, `false` or an integer | 1, 0 or the integer | none |
| `0004` | a decimal number | its IEEE 754 single | none |
| `0005`, `0007` | a decimal number | its IEEE 754 double | none |
| `0006` | a decimal number | the number times 10,000 as a 64-bit integer | none |
| `0040` | an ISO 8601 date and time, UTC when it has no offset | its FILETIME | none |

The value field is little-endian, with any unused high bytes zero, and a fixed-length type also takes `0x` with up to
16 hexadecimal digits as the whole 8-byte field. With the multi-valued bit (`0x1000`) the value is a list of elements
separated by `|` (an empty value has none), each written as its single-valued type would write it:

- `101F` and `101E`: the stream `__substg1.0_TTTTTTTT` holds a 4-byte count per element (its UTF-16 byte count plus 2,
  or its byte count plus 1), element `i` is the stream `__substg1.0_TTTTTTTT-XXXXXXXX` (`i` in eight hexadecimal
  digits) without a terminator, and the record's value field is the length stream's size.
- `1102`: the same, with an 8-byte entry per element (the byte count, then 4 zero bytes).
- `1002`, `1003`, `1004`, `100A`, `1005`, `1006`, `1007`, `1014`, `1040`, `1048`: the stream `__substg1.0_TTTTTTTT`
  holds the values packed (2, 4, 4, 4, 8, 8, 8, 8, 8 and 16 bytes each), and the record's value field is its size.

`bytes` is a list of parts joined by `+`, each decoded with `%HH` first: `x:` and hexadecimal digits, `t:` and text
(its bytes), `u:` and text (UTF-8, written as UTF-16LE), or `z:` and a count of zero bytes. An empty `bytes` is no
bytes.

**`nameid(entries)`** builds `__nameid_version1.0`, which has no property stream, from entries separated by `;`:

| Entry | What it adds |
|---|---|
| `guid=GUID` | the GUID's 16 bytes to the GUID stream |
| `id=LID@G` or `id=LID@G#P` | a numeric entry to the entry stream: `LID` (4 bytes), `G << 1` (2 bytes), then `P` (2 bytes; by default the number of entries before it) |
| `name=TEXT@G` or `name=TEXT@G#P` | the string stream is padded with zeros to a multiple of 4, then gets the name's UTF-16 byte count (4 bytes), the name in UTF-16LE (`TEXT` is UTF-8 after `%HH`) and zero padding to a multiple of 4; the entry stream gets the name's offset (4 bytes), `(G << 1) or 1` (2 bytes) and `P` |
| `stream:NAME=bytes` | a stream; naming `__substg1.0_00020102`, `__substg1.0_00030102` or `__substg1.0_00040102` replaces the stream the entries build |

`G` is `PS_MAPI` (1), `PS_PUBLIC_STRINGS` (2) or an integer (3 and up name GUID stream entry `G - 3`, MS-OXMSG
2.2.3.1.2.1), and `LID` and `P` are decimal or `0x` integers. The storage holds the GUID, entry and string streams
(`__substg1.0_00020102`, `__substg1.0_00030102`, `__substg1.0_00040102`); the name-to-ID mapping streams
(`__substg1.0_10XX0102`) are not generated, and a row that needs one adds it with `stream:`.

**Whole-file transforms** are applied after the file is built, in the order written. Each reads the file as a version 3
compound file, follows the DIFAT, and appends a sector only where the FAT already has a free entry for it.

- `cut=n` keeps the first `n` bytes of the file; a negative `n` drops the last `-n` bytes.
- `reverse:NAME` takes the first stream entry named `NAME` in directory order, which must be held in at least two
  regular sectors, and stores its logical sector `i` in the physical sector that held logical sector `n - 1 - i`: the
  FAT is relinked in logical order and the entry's starting sector becomes the sector that held the last one.
- `unallocated=n` inserts `n` free directory entries (zero bytes, with the left, right and child fields `0xFFFFFFFF`)
  directly after the root entry. Free entries that only padded the last directory sector are dropped first, every
  left, right and child field of an allocated entry that is 1 or more grows by `n`, and the directory chain is
  extended with appended sectors when it runs out of room.
- `move:minifat` copies the mini FAT's sectors, in order, to sectors appended at the end of the file, links them in
  the FAT, marks the old sectors free, and points the header's first mini FAT sector (offset `0x3C`) at the first new
  one.

For example, `gen:Msg(0037001F=Hi;attach(37050003=1;3707001F=a.txt;37010102=61))` is a message with the subject `Hi`
and one attachment, `a.txt`, whose content is the byte `61`.

## Rendering

**`Message`** (the default) opens the input with `OutlookMessage.Open` and writes, one line each, every property of
the message in ascending tag order, as `scope TAB key TAB type TAB value`; then `message TAB #body.text TAB value`,
`#body.html` and `#body.rtf` for each of `BodyText`, `BodyHtml` and `BodyRtf` that is not null; then the properties
of each recipient (scope `recipient[i]`) and of each attachment (`attachment[i]`), the attachment's nested message
following its properties when its `Method` is `EmbeddedMessage`, rendered the same way with the scope
`attachment[i]/message` (and its own recipients as `attachment[i]/message/recipient[j]`). Every line ends with a line
feed.

- The key is the tag in eight uppercase hexadecimal digits or, for a named-range tag that the message's name map
  resolves, `name:` with the property set's GUID in `D` form, a colon, and `0x` with the numeric id in at least four
  uppercase hexadecimal digits, or the string name in double quotes.
- The type is the `MapiPropertyType` name (`Int32`, `Unicode`, `String8`, `SystemTime`, ...), or `0x` and four
  hexadecimal digits for a type the enum does not define, followed by `[]` when the tag is multi-valued.

**`Conveniences`** writes the convenience accessors instead, as `scope TAB #name TAB value`, leaving out each one that
is null: for the message `#subject`, `#sender.name`, `#sender.email`, `#class`, `#message-id`, `#headers`, `#sent`,
`#received`, `#depth`, `#recipients` and `#attachments` (`Subject`, `SenderName`, `SenderEmailAddress`,
`MessageClass`, `InternetMessageId`, `TransportMessageHeaders`, `SentTime`, `ReceivedTime`, `EmbeddedDepth` and the
two counts); for each recipient `#type`, `#name`, `#email` and `#addrtype` (`RecipientType`, `DisplayName`,
`EmailAddress`, `AddressType`); for each attachment `#method`, `#filename`, `#cid`, `#mime` and `#size` (`Method`,
`FileName`, `ContentId`, `MimeTag`, `Size`), then `#content`, the bytes `OpenContentStream()` returns, when the method
is `ByValue`, or the conveniences of the nested message, scoped `attachment[i]/message`, when it is `EmbeddedMessage`.
Enum values render as their names.

**`CompressedRtf`** passes the input bytes to the compressed-RTF decoder (`CompressedRtf.Decompress`, with
`MaxDecompressedRtfBytes` as its ceiling), and the rendering is the decompressed bytes.

Values render as `null`; text, escaped as below, in full when its UTF-8 form is at most 256 bytes and otherwise as
`sha256:` with the first 16 lowercase hexadecimal digits of its SHA-256 digest, a colon and its byte length;
`true` or `false`; binary data in lowercase hexadecimal up to 32 bytes and as the same hash beyond; numbers in the
invariant culture (`R` for floating-point values, so `1.5`); times in the round-trip `o` form
(`2025-01-11T10:00:00.0000000+00:00`); GUIDs in the `D` form; and multi-valued properties as `[v1,v2]`. In text a
backslash is doubled, tab, line feed, carriage return and NUL are `\t`, `\n`, `\r` and `\0`, other C0 controls and
DEL are `\xHH`, an unpaired surrogate is `\uHHHH`, and inside a list element a comma and square brackets are `\x2C`,
`\x5B` and `\x5D`. The rendering is UTF-8, and `expected` writes it in the field escapes: a tab separator appears as
`\t`, a backslash in a value as `\\\\`, and U+00E9 as `\u{E9}`.

## Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown | unit | Runnable | Pass | Fail |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| msgreader | 89 | 95 | 36 | 14 | 44 | 1 | 0 | 50 | 50 | 0 |
| extract-msg | 276 | 287 | 69 | 11 | 202 | 5 | 8 | 72 | 72 | 0 |
| kenjiuno-msgreader | 27 | 30 | 17 | 2 | 11 | 0 | 1 | 18 | 18 | 0 |
| compressed_rtf | 7 | 8 | 0 | 1 | 7 | 0 | 0 | 1 | 1 | 0 |
| poi-hsmf | 19 | 27 | 17 | 3 | 6 | 1 | 0 | 20 | 20 | 0 |
| **Total** | **418** | **447** | **139** | **31** | **270** | **7** | **9** | **161** | **161** | **0** |

Runnable rows are the `applies` and `dialect` rows that are not `unit` rows. The nine `unit` rows (`unit` counts the
`applies` and `dialect` rows of that kind) name tests in `Bodu.Formats.Outlook.Msg.Test`, which the governance tests
require to exist, and all nine pass. The `n/a` rows are dominated by what Bodu does not offer: saving messages and
attachments as files (text, HTML, PDF), parsed transport headers, typed contact, calendar, task and journal items,
command lines, writing messages, S/MIME and RTF de-encapsulation; the rest are the libraries' languages, packaging,
exception wording and performance. The Pass and Fail columns are the runner's results on net8.0 and net10.0 with the
fixes below; when the catalogue was written, against `b6fa6972e8`, 153 runnable rows passed and eight failed.

## What the catalogue found

When the catalogue was written, eight runnable rows failed, for five causes. Each became an issue, each is fixed with
its regression test committed first, and every runnable row now passes.

**Compressed RTF: an uncompressed (`MELA`) payload's CRC and sizes were checked** (four rows, #976). MS-OXRTFCP 2.2.3.1
says that for COMPTYPE UNCOMPRESSED the reader SHOULD read every byte to the end of the stream whatever RAWSIZE says, or
MAY read RAWSIZE bytes, and MUST NOT validate the CRC field. `CompressedRtf.Decompress` rejected a `MELA` payload whose
CRC was not zero ("failed its checksum") and one whose RAWSIZE exceeded the bytes COMPSIZE left after the header
("declared sizes are inconsistent"). Rows: msgreader 1.9.15 #33 (a non-zero CRC), compressed_rtf 1.0.7 #9 case 2 (CRC
`0x12345678`), msgreader 6.0.7 PR #492 (RAWSIZE equal to COMPSIZE, 12 more than the data) and msgreader 4.5.4
`2af8c434d6` (COMPSIZE equal to RAWSIZE, both the data length). Both readers now take every byte after an uncompressed
header, whatever its CRC and declared sizes say, holding only its length to the caller's ceiling
(`docs/guides/outlook/reader-options-and-limits.md:38`, `docs/docs/outlook/concepts.md:107`).

**Strict accepted a named-property storage without its entry stream** (extract-msg v0.48.0 case 2, #977). MS-OXMSG
2.2.3.1 says the GUID, entry and string streams MUST be present. Under `Strict` a `__nameid_version1.0` storage missing
any of them now throws `OutlookMsgFormatException`, and `docs/guides/outlook/reader-options-and-limits.md:95` lists it
among the `Strict` failures.

**A string was not cut at its terminator** (kenjiuno-msgreader 1.23.0-beta1 #56 case 3, #978). A `PtypString` stream
holding `Hello`, a UTF-16 NUL and `junk` rendered as `Hello\0junk`. A `PtypString` or `PtypString8` now ends at its
first NUL (MS-OXCDATA 2.11.1) at every level in both readers: single and multi-valued values, the string names of both
named-property maps, and the text `BodyHtml` decodes from a binary `PidTagHtml`. The row runs at the default level.
Under `Strict` its input still throws, because its record's Size of 12 disagrees with the 20-byte stream (MS-OXMSG
2.4.2.2), a check the fix leaves as it was.

**A record of zeros was reported as a property** (kenjiuno-msgreader 1.7.3-alpha.1 #15 case 2, #979). Tag `0x00000000`
is property 0 of type `PtypUnspecified`, which no object stores. A 16-byte record of zeros is now padding, skipped at
every level.

**UTF-7 was not decoded** (msgreader 3.6.2 `2e085b219d`, #980). A `String8` HTML body with `PidTagInternetCodepage`
65000 rendered as its Windows-1252 reading, `<p>Gr+APYA3w-e</p>`, because .NET serves code page 65000 only behind an
opt-in switch. A declared code page of 65000 now decodes as UTF-7 (RFC 2152) in both readers, through a `UTF7Encoding`
the shared resolver constructs itself with SYSLIB0001 suppressed at that one site, and
`docs/docs/outlook/concepts.md:97` says so.

### Documentation that disagreed with the behaviour

All three are fixed.

- A recipient or attachment storage without a `__properties_version1.0` stream threw at every level, against
  `docs/guides/outlook/reader-options-and-limits.md:87` (#981). `Compatible` and `Minimal` now omit that recipient or
  attachment and read the rest of the message, `Strict` still throws, and line 94 of that guide lists the condition. The
  extract-msg v0.41.0 #357 row keeps its `reject` expectation, now under `ValidationLevel=Strict` and citing that line,
  and the test of the `Dispose_WhenAttachmentsFailToLoad_ShouldReleaseTheFile` unit row now fails the load with a
  three-byte attachment property stream, malformed at every level.
- A `PtypMultipleFloatingTime` (`0x1007`) property, one of the fixed-length multi-valued types of MS-OXMSG 2.1.4.1, was
  left out under `Compatible` and rejected under `Strict`, against `Bodu.Formats.Outlook.Msg/README.md:31-33` and
  `docs/docs/outlook/concepts.md:54` (#982). It now decodes to a `double[]` in both readers, as a single `AppTime`
  decodes to a `double`.
- `OutlookMessage.OpenRead(string)` documented only `ArgumentNullException` and `OutlookMsgFormatException` (#991). It
  now documents the `ArgumentException` for an empty path and the exceptions `File.OpenRead` lets through. The same
  issue added the skipped UTF-16 code pages (1200 and 1201) to `docs/docs/outlook/concepts.md:97`, and corrected
  `MsgNamedPropertyMap`'s remarks, which gave a named identifier as `0x8000` plus the entry's position rather than
  `0x8000 + wPropIdx`.

Smaller observations, also fixed: `OutlookAttachmentMethod` gains `ByWebReference = 7` for `afByWebReference` (MS-OXCMSG
2.2.2.9), which both readers report and `Strict` accepts, and whose content accessors throw `NotSupportedException`
(#983); and a compound file whose sector chains are broken is reported as a malformed container rather than as "not an
OLE2 compound file", with the `CompoundFileFormatException` that says why still its `InnerException` (#984). Outside
this area, `Bodu.IO.Compound` still fills the unused directory entries it writes with zero bytes, where MS-CFB 2.6.3
asks for `0xFFFFFFFF` in their left, right and child fields (#985).

### Documented behaviour that departs from the specification

Two `dialect` rows pass because Bodu documents a behaviour the specification does not share; they are kept as `dialect`
rows, and listed here for review.

- A `String8` `PidTagBody` decodes with `PidTagMessageCodepage` first (`docs/docs/outlook/concepts.md:97`), where
  MS-OXCMSG 2.2.1.58.6 names `PidTagInternetCodepage` as the code page of `PidTagBody` (poi-hsmf PR #149 case 4).
- The `LZFu` decoder stops at the declared size, and returns what it has when the body ends at a token boundary
  (`Bodu.Formats.Outlook/shared/CompressedRtf.cs:42-46` and `:154-156`, internal documentation), where MS-OXRTFCP
  2.2.3.2 decodes to the end reference and treats input that ends first as corrupt (poi-hsmf PR #1071 case 2).

## Ports and shared lineage

The five libraries are independent implementations: MsgReader in C#, extract-msg and compressed_rtf in Python,
@kenjiuno/msgreader in TypeScript and POI HSMF in Java. @kenjiuno/msgreader is a fork of the JavaScript
FreiraumIO/msgreader and ykarpovich/msg.reader, not a port of MsgReader or extract-msg; its author also wrote
MsgReader's 4.5.4 `MELA` fix, which is catalogued once, under msgreader. extract-msg decompresses RTF with
compressed_rtf, but its changelog does not repeat compressed_rtf's fixes, so nothing is counted twice.
@kenjiuno/msgreader decompresses RTF with its own @kenjiuno/decompressrtf package, which is not catalogued.

## Validation

The catalogue runs in `Bodu.Formats.Outlook.Msg.Test` as `MsgReleaseNoteCorpusTests`, over byte-identical copies of
`fixes/*.csv` embedded in the test assembly. Every runnable row is a test of its own in the Regression tier, named after
the row, and renders its input as described above. The governance tests run in every build: each file loads cleanly
(header lines, columns, classes, kinds, escapes, printable ASCII, a reason on every row), each library holds the row
counts per class pinned in the table above, every row's options are known and its input can be built, every `unit` row
names an existing test, and the embedded copies match this directory byte for byte. The runner was ported from the
scratch harness the rows were first checked with, which also checked each `unit` row's scenario from its `reason`.
