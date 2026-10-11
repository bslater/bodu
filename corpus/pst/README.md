# Bodu pst validation corpus

External evidence used to validate `Bodu.IO.Pst` (the NDB and LTP container reader) and `Bodu.Formats.Outlook.Pst` (the mail-store reader over it, with the shared MAPI value model of `Bodu.Formats.Outlook`), kept in the repository so that every reconciliation can be reproduced from committed files. [MS-PST] is the specification Bodu follows, with [MS-OXCDATA], [MS-OXCMSG], [MS-OXPROPS] and [MS-OXRTFCP] for property values and compressed RTF; they publish no test files, so the evidence here is what other readers of the format learned by fixing their own defects.

## `fixes/` - the release-note fix catalogue

The release notes and commit histories of four PST readers were read in full, and every fix they list is catalogued: one file per library, `fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. A scenario is the one the fix's commit, issue or test describes. Each row records the version, the reference (an issue, a pull request or a commit) and a short summary in our own words, then a class, and, for a runnable row, the input and the result Bodu must give.

### Sources

| File | Library | Language | What was read | Licence |
|---|---|---|---|---|
| `libpff-fixes.csv` | [libpff](https://github.com/libyal/libpff) (and its Python binding pypff) | C | `ChangeLog` at 03545511ab7a7a609e50ec715bdd3985c3be71c5 (SHA-256 df10df01f18351daff0188cae5545962445a76291ed7eee74159d57655226985), every dated entry from 20080511 to 20141002, then the git log of that commit to 20260926, which the ChangeLog defers to; the GitHub releases page, the PyPI `libpff-python` versions and the Debian source versions, read 2026-10-08 | LGPL-3.0-or-later |
| `java-libpst-fixes.csv` | [java-libpst](https://github.com/rjohnsondev/java-libpst) | Java | No release notes exist (no changelog, no GitHub release notes), so the git history of master at f158a64acf2a0e46ac3bd699bc7a5a8da6c40d26 (2022-07-12), tags 0.7 to 0.9.4 and the commits after 0.9.4; Maven Central versions read 2026-10-08 | Apache-2.0 OR LGPL-3.0-or-later |
| `libpst-fixes.csv` | [libpst](https://www.five-ten-sg.com/libpst/) (readpst, lspst, pst2ldif, pst2dii) | C | `ChangeLog` (SHA-256 a7ef6e5f8a3e8d03659d99bf2f92fcc15f8bf2a481dc3774894cdf5f316ff0d9) and `NEWS` (SHA-256 2518a9720d5ef05f1664a22e5e95d7805d3096c521516c9274a6c9f39a0d529b) of `libpst-0.6.76.tar.gz` (SHA-256 3d291beebbdb48d2b934608bc06195b641da63d2a8f5e0d386f2e9d6d05a0b42) from https://www.five-ten-sg.com/libpst/packages/, read 2026-10-08, v0.1 to 0.6.76; each fix's commit in the git mirror [pst-format/libpst](https://github.com/pst-format/libpst) at cc600eebd8 | GPL-2.0-or-later |
| `xstreader-fixes.csv` | [XstReader](https://github.com/Dijji/XstReader) | C# | https://github.com/Dijji/XstReader/releases pages 1 and 2, read 2026-10-11, 1.0 to 1.14; the git history of master at 3b7856c8b3d01bdf8aa13744e476d1ef7761b832 (2021-01-02) | MS-PL |

Each file's header repeats its sources and records what was left out and why.

### What is catalogued

- Fixes only. New features, refactors, documentation, version bumps and packaging updates are not catalogued. A fix to a tool's output (readpst's MIME, mbox and vCard writers, pffexport, XstReader's WPF interface and CSV export), the build, packaging, a platform, a C API or the library's language (C memory management) is catalogued as `n/a`, as is a fix to a format Bodu rejects (the 4 KiB-page OST).
- Where the notes are missing or incomplete (libpff after 20141002, java-libpst, XstReader), the commit history was read as well. A fix found only there is listed under the first release that contains it, unless it was made before the first release or fixes code first added in the same release cycle. java-libpst has no release notes at all, so its commits after 0.9.4 are listed as `unreleased`.
- An entry that makes a released version read a structure existing files use (libpst's encryption type 2, `wVer` 15, 0x0101 and 0x0201 blocks, code pages 1200 and 1201) is catalogued as a fix; a new format (libpst's 64-bit format of 0.6.1, the OST 2013 format) is a feature.
- libpff and libpst are copyleft and XstReader is Ms-PL, so their inputs are written for this catalogue; java-libpst's own tests read whole PST files, so its inputs are written here too. Every input is a generator call (below), and no upstream test data or sample file is restated or committed.
- **Ports and lineage.** pst-extractor (TypeScript) is a port of java-libpst, so it is not catalogued separately. libpff began from an analysis of libpst 0.6.8 but shares no code with it, and XstReader is an independent implementation, so the four are separate evidence; where two libraries fixed the same scenario, each has its own row.

### Columns

| Column | Content |
|---|---|
| `library` | The library's short name, as in the file name |
| `version` | The release, as its notes write it (libpff's are dates); `unreleased` for an entry not yet released |
| `reference` | `#123` (an issue in that repository), `PR #123`, a commit (first 10 hex digits) or empty |
| `summary` | A short paraphrase of the entry |
| `class` | `applies`, `dialect`, `n/a` or `unknown` (below) |
| `case` | Empty when the fix has one row; `1`, `2`, ... when it needs several |
| `kind` | `parse`, `reject` or `unit` (below); empty for `n/a` and `unknown` |
| `options` | `Name=Value;Name=Value` (below); empty for the defaults |
| `input`, `expected` | The input and the expected result, escaped (below) |
| `expectation` | Where `expected` comes from: `spec` (MS-PST or a companion specification), `derived` (worked out from the generator definitions and Bodu's documentation, with the working in `reason`) |
| `reason` | Required on every row: the upstream commit, issue or test a runnable row comes from, the Bodu document a `dialect` row relies on, the exclusion an `n/a` row falls under, the place an `unknown` row was looked for |

Every file is UTF-8 with LF line endings and no byte order mark, and every character in it is printable ASCII. Anything else in `input` and `expected` is written with an escape: `\n`, `\r`, `\t`, `\0`, `\\`, `\xHH` (one byte) and `\u{H...}` (the UTF-8 encoding of one Unicode scalar). Fields follow RFC 4180 quoting.

### Classes

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 212 | The row runs, and Bodu must do what the fix established |
| `dialect` | 39 | The row runs, asserting Bodu's documented behaviour where it deliberately differs from the fix; `reason` names the document (a validation level, a documented limit, the code-page rules) |
| `n/a` | 308 | The fix concerns something Bodu does not have (a tool's output, a C API, recurrence decoding, S/MIME, the 4 KiB-page OST), memory management, a platform, the build or packaging; `reason` says which |
| `unknown` | 21 | The notes, commit and issue name no scenario; `reason` links what was read |

### How a row runs

An input is `gen:` followed by a generator pipeline (below); the bytes it builds are read on one of two surfaces.

| Kind | Passes when |
|---|---|
| `parse` | Opening the input and rendering it (below) succeeds, and the rendering equals `expected` |
| `reject` | Opening the input or rendering it throws an exception whose type name is `expected`: `PstFileFormatException` or `PstUnsupportedFormatException` from the container, `OutlookPstFormatException` from the mail store |
| `unit` | Not run from the file: `input` names a test to be written (`<TestClass>.<Member>_When<Condition>_Should<Result>`), and `reason` gives its model, input and expected outcome |

**The Store surface** (the default) opens the input with `OutlookMailStore.Open` and walks it depth first. Lines are joined with LF:

- `#store\t<display name>\t<property count>`, then the store's property listing with `Detail=properties`, when `Detail` holds `store`.
- `#name\t0x<id>\t<resolution>` for each identifier 0x8000 to 0x8000 + `Names` - 1, the resolution being `<property set GUID>:"<name>"` or `<property set GUID>:0x<8 hex>` from `TryGetPropertyName`, or `-`.
- `#folder\t<path>\t<MessageCount or ->\t<HasSubfolders>` for each folder, the root as `/` and a child as `<parent>/<name>` (`/` in a name as `\x2F`), then the folder's property listing when `Detail` holds `folders`. Bodu exposes no declared subfolder count, so `HasSubfolders` (`true` or `false`) stands in for it.
- One line per message of the folder, in `EnumerateMessages` order: `<path>\t<index>\t<Subject or ->\t<recipient count>\t<attachment count>\t<listing>`, where `<listing>` is the first 16 lower-case hex digits of the SHA-256 of the message's property listing (lines joined with LF, UTF-8), or `*` with `Detail=properties`, which then writes the listing itself as `#property\t<line>` lines. With `Detail=associated` the folder's associated messages follow, indexed `a0`, `a1`, ...
- With `Detail=bodies`: `#body.text\t`, `#body.html\t` and `#body.rtf\t` followed by `BodyText`, `BodyHtml` and `BodyRtf`: `-` for none, the escaped text when its UTF-8 is at most 256 bytes, otherwise `sha256:<16 hex>/<UTF-16 length>`.
- With `Detail=recipients`: `#recipient\t<i>\t<RecipientType or ->\t<DisplayName>\t<EmailAddress>\t<AddressType>` (each `-` when absent), then its listing with `Detail=properties`.
- With `Detail=attachments`: `#attachment\t<i>\t<Method>\t<FileName or ->\t<Size or ->\t<content>`, the content being the bytes of `OpenContentStream` (hex) for `ByValue`, `message` for `EmbeddedMessage`, and `-` otherwise; then its listing with `Detail=properties`; an embedded message is then rendered as a message line indexed `<message index>.<i>`, with the same details.

A **property listing** has one line per property in tag order: `<tag>\t<type>\t<value>`. The tag is `0x<8 hex>`, or `name:<GUID>:"<name>"` / `name:<GUID>:0x<8 hex>` for a named property the store resolves. The type is the `MapiPropertyType` member name, `Mv` before it for a multi-valued tag, or `0x<4 hex>` for a code the enumeration does not define. The value is `null` for none, a string escaped, a binary as upper-case hex up to 32 bytes and `sha256:<16 hex>/<length>` beyond, an array as `[a,b]` (with `,`, `[` and `]` inside a string element written `\x2C`, `\x5B` and `\x5D`), a Boolean `true` or `false`, a floating-point number in round-trip form, a time in ISO 8601 round-trip form (`o`), a GUID in `D` form, and any other number in invariant decimal.

**The Nodes surface** opens the input with `PstFile.Open` and writes `#file\t<Format>\t<CryptMethod>`, then one line per node in `EnumerateNodes` order, `0x<nid 8 hex>\t<PstNodeType>\t<DataLength>\t<subnode count>`, with `\t<data>` (the bytes of `ReadAllBytes`, hex or hashed as above) when `Data=true`. With `Node` (and optionally `Subnode`, a subnode of that node), `View` adds:

| View | Lines |
|---|---|
| `Pc` | `#pc\t<nid>\t<count>`, then per property `0x<id>\t0x<wire type>\t<value>`: a Unicode string as `"<escaped text>"`, anything else as the raw bytes |
| `Tc` | `#tc\t<nid>\t<RowCount>\t<columns>`, each column `0x<id>/0x<wire type>/<width>` separated by spaces, then per row `#row\t0x<row id>\t<cells>`, each present cell `0x<id>=<value>` separated by spaces, a space inside a value written `\x20` |
| `RowIds` | `#rowids\t<nid>\t<RowCount>\t<row ids separated by spaces>` |
| `Lookup` | `#lookup\t<nid>\t0x<Row>\t<cells>`, the cells of `TryGetRow(Row)` as for `Tc`, or `-` |
| `Subnodes` | `#subnodes\t<nid>`, then per subnode `0x<nid>\t<type>\t<length>\t<HasSubnodes>` |
| `Data` | `#data\t<nid>\t<data>` |

Text inside a rendering is escaped the same way everywhere: `\` as `\\`, LF, CR, tab and NUL as `\n`, `\r`, `\t` and `\0`, any other C0 control or DEL as `\xHH`, an unpaired surrogate as `\u{HHHH}`; everything else, non-ASCII included, is kept (the CSV escapes it in turn).

| Option | Maps to | Values |
|---|---|---|
| `Surface` | the rendering | `Store` (default), `Nodes` |
| `ValidationLevel` | `PstFileOptions.ValidationLevel`, `OutlookMailStoreReaderOptions.ValidationLevel` | `Minimal`, `Compatible` (default), `Strict` |
| `DecompressRtf` | `OutlookMailStoreReaderOptions.DecompressRtf` | `true` (default), `false` |
| `BlockCacheSize` | `BlockCacheSize` of either options type | a non-negative integer |
| `MaxNodeDataLength` | `MaxNodeDataLength` of either options type | a positive integer |
| `MaxDataTreeLeaves` | `PstFileOptions.MaxDataTreeLeaves` (Nodes) | a positive integer |
| `MaxEmbeddedMessageDepth`, `MaxDecompressedRtfBytes`, `MaxInlineAttachmentBytes` | the `OutlookMailStoreReaderOptions` members (Store) | a positive integer |
| `Detail` | the Store rendering's optional parts | a comma-separated list of `properties`, `recipients`, `attachments`, `bodies`, `folders`, `store`, `associated` |
| `Names` | the Store rendering's `#name` lines | a count |
| `Node`, `Subnode` | the node (and subnode) a `View` reads (Nodes) | `0x` and hex digits |
| `View` | the Nodes rendering's view | `Pc`, `Tc`, `RowIds`, `Lookup`, `Subnodes`, `Data` |
| `Row` | the row `View=Lookup` looks up | `0x` and hex digits |
| `Data` | node data on every Nodes line | `true`, `false` (default) |

### Generators

`gen:` is followed by calls separated by `|`: a generator, then any transforms, each `Name(key=value,...)`. A value runs to the next `,` or `)`, or is double-quoted with `""` for a quote; integers are decimal or `0x` hex; hex byte strings may start with `0x`; keys marked repeatable may be given more than once, and any other key at most once. Every generator writes through the repository's test builders, linked unchanged: `PstFixtureBuilder` (`Bodu.IO.Pst/test/IO.Pst.Internal/`) for the header, the two B-trees and the blocks, and `PstLtpFixtureBuilder` (same folder) for heaps and BTHs; their own documentation defines what they write (block identifiers from 4 in steps of 4 in write order, bit 0x2 set for internal blocks; external blocks encoded with the file's method; trailers and CRCs per MS-PST 2.2.2.8.1 and 5.3; B-trees written as leaves of up to `pagesize` entries, the capacity when not given, with one branch page over them when there is more than one leaf, each branch entry keyed by its leaf's first key). Every generator takes `format` (`Unicode`, the default, or `Ansi`), `crypt` (`None`, the default, `Permute` or `Cyclic`), `pagesize` and `version` (the header's `wVer`, defaulting to 23 or 14).

**Heap rule.** A heap (MS-PST 2.3.1) is filled item by item: an item goes in the current heap block unless `used + length + 4 + 2 * (items + 2)` exceeds the block payload (8176 bytes, 8180 in ANSI), in which case a new heap block starts; `used` starts at 12 (the HNHDR) in block 0, at 66 (an HNBITMAPHDR) in block 8 and every 128th block after it, and at 2 (an HNPAGEHDR) otherwise, and `items` counts the block's items. Each block ends with its page map (cAlloc, cFree 0, rgibAlloc). A heap of one block is one data block; a heap of several is an XBLOCK over them. A property context adds its BTH leaf (records of key 2 and data 6, in identifier order) and its BTHHEADER to the current block last, and names the BTHHEADER in hidUserRoot. A value is stored inline in its record when its type is 0x0000 to 0x0004, 0x000A or 0x000B; otherwise it is a heap item when at most 3580 bytes, or else a subnode of the owning node with identifier `((0x800 + k) << 5) | 0x1F`, the lowest k not yet used in that node's subnode list. A node's subnodes form one SLBLOCK in identifier order.

**`Ndb(nodes=n, data=hex)`** - n nodes (default 1) with identifiers 0x21, 0x61, 0x122, then `((0x401 + i - 3) << 5) | 0x02` for the i-th (0-based), each with its own data block holding `data` (default `010203`), no subnodes and parent 0.

**`DataTree(blocks=n, size=s, level=1|2, per=m, childlevel=l, count=c, total=t, selfref=true, internalleaf=true, rawroot=hex)`** - node 0x21 whose data is n data blocks (default 2), block k holding s bytes (default 16), byte j being `(16k + j) mod 256`, under an XBLOCK (level 1, the default) or an XXBLOCK over XBLOCKs of m blocks each (level 2; m defaults to 2, and each XBLOCK declares cLevel `childlevel`, default 1). `count` replaces the root's cEnt and `total` its lcbTotal; `selfref` appends the root's own identifier as a last child, `internalleaf` an XBLOCK over block 0; `rawroot` writes the root block as the given bytes instead.

**`Subnodes(entries=n, index=true, count=c, nidhigh=h, data=hex, slraw=hex)`** - node 0x21 (data `010203` or `data`) with n subnodes (default 2), subnode i being `((0x400 + i) << 5) | 0x1F` with a data block holding the byte i, in one SLBLOCK or, with `index`, one SLBLOCK per entry under an SIBLOCK. `count` replaces the SLBLOCK's cEnt, `nidhigh` sets the upper 32 bits of every Unicode SLENTRY nid, and `slraw` writes the subnode tree block as the given bytes.

**`Pc(prop=..., heapfill=k, emptyitem=true, bthlevels=1, leafsize=r, bthselfref=true, bthkey=n, bthent=n, bthidx=n, nid=0x21)`** - one node (default 0x21) whose heap holds a property context over the `prop` values (repeatable, the syntax of `Mail` below); `heapfill` first adds k 3000-byte zero items, `emptyitem` a zero-length item. The records form one BTH leaf item, or with `bthlevels=1` leaf items of r records (default 2) under one index item; `bthselfref` makes the index item's last entry name the index item itself; `bthkey`, `bthent` and `bthidx` replace the BTHHEADER's cbKey, cbEnt and bIdxLevels. A subnode-resident value of this generator is `((0x800 + k) << 5) | 0x1F` with k counting the node's subnodes. Heap block 0 is then patched: `hnsig` (bSig), `userroot` (hidUserRoot), `mapcount` (cAlloc), `mapfree` (cFree), `mapoffset=i:v` (rgibAlloc[i], repeatable), `hnpm` (ibHnpm, written last) and `heappatch=offset:hex` (repeatable), the page map located through the ibHnpm written first.

**`Table(rows=n, width=w, matrix=auto|heap|subnode, padrows=true, heapfill=k, matrixbytes=m, rowidxent=e, rownumber=i:v, ibit=c:v, nid=0x802E)`** - one node (default 0x802E) whose heap holds a table context of n rows (default 2) with identifiers 1 to n, each w bytes wide (default 5): column 0x67F20003 at offset 0, then filler columns 0x6000, 0x6001, ... (8-byte PtypInteger64 first, then 4-byte PtypInteger32, 2-byte PtypInteger16 and 1-byte PtypBoolean, as many as fill the row) that no row has, then the cell existence bitmap with only the row identifier's bit set; iBit is each column's position. The row matrix is a heap item when `matrix=heap`, or with `auto` (the default) when n * w is at most 3580 bytes; otherwise subnode 0x0001001F, `floor(8176 / w)` rows per data block (8180 in ANSI) under an XBLOCK when there are several, each block but the last padded to the full payload with `padrows`. The row index BTH has key 4 and data 4 (2 in ANSI): row identifier to row number. `matrixbytes` keeps only the first m bytes of a heap-resident matrix, `rowidxent` replaces the row index BTHHEADER's cbEnt, `rownumber` the number recorded for row i, and `ibit` a column's iBit; the heap patches of `Pc` apply too.

**`Mail(...)`** - a mail store written from a model:

- Node 0x21, the store, with PidTagDisplayName (0x3001) `storename` (default `Store`) unless `storeprops=none`, plus `sprop` values (repeatable); `store=false` writes no node 0x21.
- Node 0x122, the root folder, with 0x3001 `Root` and PidTagSubfolders (0x360A) true. Each folder with index x has a hierarchy table `(x << 5) | 0x0D` whose rows name its child folders, and a contents table `(x << 5) | 0x0E` whose rows name its messages (row identifiers only); a folder's node parent is its parent folder, the root's is itself, a table's is 0.
- Folders: the Inbox (`inbox`, default `Inbox`) first, then each `folder` path (repeatable, `/`-separated, parents created as needed), with node identifiers `(x << 5) | 0x02` for x from 0x401 in creation order (the Inbox is 0x8022), PidTagDisplayName, PidTagContentCount (0x3602, its message count) and PidTagSubfolders (true when it has children); `fprop` adds Inbox properties (repeatable). `twin=path` adds one more folder with that folder's name under the same parent; `searchfolder=name` adds a search folder `(x << 5) | 0x03` under the root; `inboxparent` sets the Inbox node's parent; `danglingrows=nid` (repeatable) puts a row naming an unwritten node before the Inbox's message rows; `contents=false` and `hierarchy=false` leave out the Inbox's tables.
- Messages: `messages` (default 1) in the Inbox, message i (1-based) being `((0x10000 + i) << 5) | 0x04` with PidTagMessageClass (0x001A) `IPM.Note` and PidTagSubject (0x0037) `Message i`. The first message takes `subject`, `class`, `cp` (PidTagMessageCodepage 0x3FFD, also the encoding of its String8 values; Windows-1252 when not given), `prop` (repeatable), `rcpt` and `att` (repeatable), `atttable=empty` and `rcpttable=empty` (an attachment or recipient table with no rows), and `subnodeindex=true` (its subnode tree as an SIBLOCK over one SLBLOCK per entry).
- A message's subnodes are its recipient table 0x692 (rows 1 to n, the recipients' properties as cells), its attachment table 0x671 (rows named by the attachment objects' identifiers, each with PidTagAttachNumber (0x0E21) = i and any `rowprop` cells) and its attachment objects `((0x401 + i) << 5) | 0x05`; each table keeps its own subnodes (a matrix or values moved out of its heap) in its own subnode tree. Tables put column 0x67F20003 first and the other cell types in tag order, 8- and 4-byte cells first, then 2-byte, then 1-byte, then the existence bitmap; a variable-size cell holds an HNID by the heap rule; the matrix is a heap item up to `matrixheap` bytes (default 3580), else a subnode of the table split as in `Table`, padded with `padrows=true`.
- Name-to-id map: `names` writes node 0x61 with 0x0001 (PtypInteger32 251), 0x0002 (the GUID stream), 0x0003 (the entry stream) and 0x0004 (the string stream). Entries are separated by `;`: `n~guid~id[~idx]` (a numeric name), `s~guid~name[~idx]` (a string name, appended to the string stream as a 4-byte length, UTF-16LE and zero padding to a multiple of 4) or `o~guid~offset[~idx]` (a string entry naming that string-stream offset); guid is `1` (PS_MAPI), `2` (PS_PUBLIC_STRINGS), `#k` (wGuid k as given) or a GUID, appended to the GUID stream in order of first use and numbered from 3; each NAMEID is dwPropertyID (the number or string offset), `wGuid << 1 | N` and wPropIdx (idx, or the entry's position). `nameguids`, `nameentries` and `namestrings` replace a stream's bytes (hex or `fill:n`).

`prop` is `<tag>[/heap|/subnode|/null|/raw=v]:<value>` with an 8-hex-digit tag: the placement forces a heap item or a subnode, writes 0, or writes v as the record's value. The value is read by the wire type: an integer for the inline types (a Boolean 1 or 0); UTF-16LE text for 0x001F; text in the owner's encoding for 0x001E; hex, `utf8:<text>` or `fill:n` for 0x0102 and 0x000D; a GUID for 0x0048; an ISO 8601 date-time (stored as the UTC FILETIME) or an integer for 0x0040; `d:<double>` or an integer for 0x0005 and 0x0007; an integer for 0x0006 and 0x0014; `raw:<hex>` gives the exact bytes for any non-inline type. A multi-valued value lists its items separated by `;`, variable-size types as a count, offsets and the items (MS-PST 2.3.3.4.2) and fixed-size types packed. `fill:n` is n bytes, byte i being `i mod 251`.

`rcpt` is `type~name~email~addrtype[~prop=...]`: PidTagRecipientType (0x0C15), PidTagDisplayName (0x3001), PidTagEmailAddress (0x3003) and PidTagAddressType (0x3002), each left out when empty.

`att` is `method~longname~content[~key=value...]`: PidTagAttachMethod (0x3705; `none` leaves it out) and PidTagAttachLongFilename (0x3707), then for method 5 an embedded message, subnode 0x8024 of the attachment object with 0x001A `IPM.Note` and 0x0037 `content`, referenced by a PidTagAttachDataObject record (0x3701000D: the subnode identifier and 4 zero bytes); otherwise the content is PidTagAttachDataBinary (0x3701) as UTF-8 text, `0x` hex or `fill:n`, or `-` for none. Keys: `short` (0x3704), `size` (0x0E20), `mime` (0x370E), `cid` (0x3712), `data=subnode` (the data as a subnode), `node=false` (no attachment object, only its table row), `embednode=false` and `embeddata=none` (the embedded message's subnode left out, or written with block identifier 0), `object=hex` (a PtypObject payload in subnode 0x0001203F, referenced by a record of that identifier and the length), `ercpt` (a recipient of the embedded message, `|` for `~`), `prop` (repeatable), `rowprop` (a cell of the attachment-table row), `eprop` (a property of the embedded message, replacing one with the same identifier) and `eatt` (an attachment of the embedded message, `|` for `~`).

**`Header(magic, client, version, sentinel, crypt, eof, nbtoffset, bbtoffset, crc)`** - writes dwMagic (offset 0), wMagicClient (8), wVer (10), bSentinel (512; 460 in ANSI), bCryptMethod (513; 461), ibFileEof (184, 8 bytes; 168, 4 bytes) and the offsets of the node and block B-tree roots (224 and 240; 188 and 196), then recomputes dwCRCPartial over bytes 8 to 478 (`crc=repair`, the default), recomputes and inverts it (`bad`) or leaves it (`keep`).

**`BTree(tree=nbt|bbt, page=root|leaf, leaf=i, entry=e, count=n, level=l, type=t, child=self|sibling|offset, btkey=k, nidhigh=h, bbtoffset=o, bbtlength=n, cbent=c, crc=repair|bad|keep)`** - patches one page of the node (default) or block B-tree: the root the header names, or the i-th child of the root. `count`, `level` and `cbent` replace cEnt, cLevel and cbEnt (cbEnt after the entry is located with the stride written), `type` replaces ptype and ptypeRepeat; for entry e (default 0, at e * cbEnt), `child` points its BREF at the root itself, copies the previous entry's BREF, or replaces its offset, `btkey` replaces its key, `nidhigh` the upper 32 bits of its first identifier, `bbtoffset` the identifier-width field after the first (a BBTENTRY's offset) and `bbtlength` the 2 bytes after the two identifiers (its cb). The page CRC is then recomputed over its first 496 bytes (500 in ANSI), recomputed and inverted, or kept.

### Counts

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| libpff | 266 | 297 | 104 | 22 | 160 | 11 |
| java-libpst | 66 | 79 | 49 | 8 | 20 | 2 |
| libpst | 172 | 187 | 54 | 9 | 116 | 8 |
| xstreader | 17 | 17 | 5 | 0 | 12 | 0 |
| **Total** | **521** | **580** | **212** | **39** | **308** | **21** |

Of the 251 `applies` and `dialect` rows, 247 are runnable and 4 are `unit` rows.

### Status

The rows were run with a harness outside the repository that implements the renderings, options and generators above over `Bodu.IO.Pst` and `Bodu.Formats.Outlook.Pst`: 236 runnable rows pass and 11 fail. They have not yet been ported into the test projects, and Bodu has not been changed. The failures, by apparent cause:

- **Container exceptions where the mail store documents tolerance.** A property whose value reference does not resolve (libpff 20100504) or whose fixed-size value is short (libpst 0.6.63 case 1) throws `PstFileFormatException` under `Compatible`, where `docs/guides/outlook/reading-pst-mail-stores.md` says a malformed property is omitted; an attachment whose data subnode is missing throws `PstFileFormatException`, where the same guide lists a missing by-value payload under `OutlookPstFormatException` (java-libpst PR #82 case 3).
- **OST client magic.** A header with wMagicClient `SO` is rejected as an invalid header (`PstFileFormatException`), at version 23 and at 36, where the guides say an OST file throws `PstUnsupportedFormatException` (libpff 792a60b797 cases 2 and 3).
- **Header version.** wVer 24 is rejected, though MS-PST 2.2.2.6 makes any version of 23 or more a Unicode file (libpff 20100429 case 1); Bodu documents accepting only the versions it knows.
- **A B-tree page reachable twice** is read twice, so its nodes are listed twice (libpff 0ba671e045).
- **A page map whose first allocation starts at offset 0**, inside the heap header, is not detected and the value reads the header bytes (libpff 20090616).
- **Trailing zeros** are kept in a name-to-id string (libpff 20100429) and in a binary HTML body (libpff 20091204).
- **UTF-7** (code page 65000) is not decoded (libpff 20090807 case 2).
