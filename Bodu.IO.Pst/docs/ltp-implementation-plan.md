# Bodu.IO.Pst — P1 LTP layer (HN / BTH / PC / TC) implementation plan

**Date:** 2026-08-19
**Status:** **Executed 2026-08-19** — tranche **P1** of
[`pst-container-exploration.md`](pst-container-exploration.md) §7,
landed per the commit sequencing in §8. One deliberate deviation: the
exact PC BTH shape (`cbKey`&#160;2 / `cbEnt`&#160;6) is enforced at
every validation level rather than Strict-only, because the record
layout slices fixed offsets and a different shape cannot be decoded at
all. One corpus finding recorded in the Regression suite: stored
subjects carry the MS-PST two-character subject-prefix marker
(U+0001 + length indicator), and `lspst`'s message counts are a floor —
one fixture carries message nodes the oracle did not classify as Email.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *New library
candidates → `Bodu.Formats.Outlook.Pst`* and the per-project
*`Bodu.IO.Pst`* section.

This plan sequences the LTP (Lists, Tables, Properties) layer of MS-PST
over the landed NDB substrate: the heap-on-node (HN), BTree-on-heap
(BTH), property context (PC), and table context (TC) surfaces the
exploration doc calls "the bulk of the package". P2 (hardening) and P3
(the `Bodu.Formats.Outlook.Pst` messaging reader) remain separate
tranches.

## 1. Governing constraints (from the exploration doc)

- **LTP lives in this package and is public.** §3: "The LTP layer sits
  in the container package deliberately. PC and TC are as
  format-agnostic as CFB's directory." The §4 API sketch puts the LTP
  views on `PstNode`: `ReadPropertyContext()` / `ReadTableContext()`
  with public `PstPropertyContext : IReadOnlyCollection<PstPropertyValue>`,
  `PstPropertyValue` (readonly struct), `PstTableContext`,
  `PstTableColumn`, `PstTableRow`.
- **MAPI-free boundary.** §1/§3: `Bodu.IO.Pst` speaks node ids,
  16-bit property ids, and raw `ushort` wire type codes with
  little-endian payloads. No `Bodu.Formats.Outlook` reference, no
  `MapiPropertyType`.
- **Streaming-first (R4).** TC row matrices are read one block at a
  time; nothing materializes the whole matrix.
- **Unicode only (P-D1).** ANSI/OST rejection already happens at
  `PstHeader.Parse`; LTP readers keep offsets in named constants so the
  ANSI widths can be parameterized later (R5). *(Since 2026-09-04 the
  ANSI format is read; the LTP layer needed two changes only — the
  row-matrix block payload, 8,180 bytes against 8,176, and the two-byte
  ANSI row-index number — both driven by the shared `PstLayout`.)*

## 2. Format facts the implementation encodes (MS-PST §2.3)

- **HN**: block 0 starts with HNHDR — `ibHnpm`(2) `bSig`(1, `0xEC`)
  `bClientSig`(1: `0xBC` PC, `0x7C` TC, `0xB5` BTH) `hidUserRoot`(4)
  `rgbFillLevel`(4). Later blocks start with HNPAGEHDR (`ibHnpm`, 2),
  except block index 8 and every 128 thereafter (HNBITMAPHDR, 66 bytes
  — header parsing needs only `ibHnpm`, first in all three shapes).
  Each block carries an HNPAGEMAP at `ibHnpm`: `cAlloc`(2) `cFree`(2)
  `rgibAlloc[cAlloc+1]`(2 each), monotonically non-decreasing and in
  bounds. **HID** = 32-bit: type bits (low 5, must be 0), `hidIndex`
  (11 bits, 1-based, nonzero), `hidBlockIndex` (high 16). Allocation
  *n* spans `rgibAlloc[n-1] .. rgibAlloc[n]` within its block. HID `0`
  is the null heap id. **HIDs address individual data blocks**, so the
  HN reader consumes the node's data blocks as an ordered segment list,
  not the flattened payload `PstDataTree.Resolve` returns today.
- **BTH**: `BTHHEADER` heap item — `bType`(1, `0xB5`) `cbKey`(1 ∈
  {2,4,8,16}) `cbEnt`(1) `bIdxLevels`(1) `hidRoot`(4). Index records
  `{key, hidNextLevel(4)}`; leaf records `{key, data(cbEnt)}`.
  `hidRoot == 0` → empty tree.
- **PC**: the BTH itself (`bClientSig 0xBC`, header at `hidUserRoot`),
  `cbKey = 2` (property id), `cbEnt = 6` (`wPropType`(2) +
  `dwValueHnid`(4)). Value placement: fixed types ≤ 4 bytes inline in
  the dword (`0x0002/0x0003/0x0004/0x000A/0x000B/0x0001`); fixed
  8/16-byte types via HID (`0x0005/0x0006/0x0007/0x0014/0x0040` = 8,
  `0x0048` = 16); everything else (strings, binary, object, the
  `0x1xxx` multi-valued forms) via **HNID** — low-5-bits-zero ⇒ HID
  into the heap, otherwise an NID into the owning node's subnode tree
  whose data payload is the value.
- **TC**: heap (`bClientSig 0x7C`); `hidUserRoot` → TCINFO: `bType`(1,
  `0x7C`) `cCols`(1) `rgib` = `TCI_4b/TCI_2b/TCI_1b/TCI_bm`(2 each)
  `hidRowIndex`(4) `hnidRows`(4) `hidIndex`(4, deprecated) then
  `rgTCOLDESC[cCols]` of 8 bytes: `tag`(4 = propid<<16 | wire type)
  `ibData`(2) `cbData`(1) `iBit`(1). Row index = BTH (`cbKey=4` rowId →
  `cbEnt=4` row number in the Unicode format). Row matrix:
  `hnidRows == 0` → empty; HID → one contiguous heap item; NID →
  subnode whose data blocks each hold `floor(8176 / TCI_bm)` rows
  (rows never span blocks; the last block may be short). Row layout:
  `dwRowID`(4), fixed-width cells at `ibData`, cell-existence bitmap in
  `[TCI_1b, TCI_bm)` (cell present iff `ceb[iBit/8] & (1 << (7 - iBit%8))`,
  MSB-first). Variable-width cells hold a 4-byte HNID resolved exactly
  like PC values.

## 3. Internal layer (`src/IO.Pst.Internal/`)

| File | Responsibility |
| --- | --- |
| `PstDataTree.cs` *(modify)* | Add `ResolveSegments(source, blockId) → List<byte[]>` (ordered leaf data blocks); re-express `Resolve` as its concatenation — behavior unchanged. |
| `PstHeapNode.cs` | HN parse over the segments; `ClientSignature`, `UserRootHid`, `GetItem(hid)` / `TryGetItem` with full bounds validation. |
| `PstHnid.cs` | HID-vs-NID discrimination: `IsNull(hnid)` (== 0), `IsHeapId(hnid)` (`(hnid & 0x1F) == 0`). |
| `PstBthHeader.cs` | `readonly record struct` (KeySize, DataSize, IndexLevels, RootHid). |
| `PstBTreeOnHeap.cs` | `ReadHeader`, `EnumerateRecords` (key/data memory pairs), `TryFind` (little-endian unsigned key compare, descent bounded by `IndexLevels`). |
| `PstWireType.cs` | Wire-type size classification: `TryGetInlineSize` (≤ 4-byte types), `TryGetFixedHeapSize` (8/16-byte types), `IsKnown`. Unknown wire types surface raw; `Strict` throws. |
| `PstLtpContext.cs` | The HNID payload resolver a PC/TC carries: HID → heap item copy; NID → owning node's subnode data via `PstDataTree` (subnode tree loaded lazily, cached by NID). |
| `PstPcEntry.cs` | `readonly record struct` (PropertyId, WireType, RawValue). |
| `PstPropertyContextReader.cs` | Validates `bClientSig 0xBC`, reads the BTH (`cbKey==2`, `cbEnt==6`), materializes the 8-byte records; values stay unresolved HNIDs. |
| `PstTcInfo.cs` / `PstTcColumn.cs` | `readonly record struct`s for TCINFO (rgib ends, row-index HID, rows HNID, columns) and TCOLDESC. |
| `PstTableContextReader.cs` | Validates `bClientSig 0x7C` / `bType 0x7C`, TCINFO/TCOLDESC extents; `EnumerateRowBlocks` (HID single heap item, or subnode blocks — the R4 streaming point); `TryLocateRow` via the row-index BTH. |

## 4. Public surface (`src/IO.Pst/`)

- `PstNode.ReadPropertyContext()` / `ReadTableContext()` — wrong-kind
  node throws `PstFileFormatException`; each call re-reads from disk
  (documented; the LRU block cache is P2).
- `PstPropertyContext : IReadOnlyCollection<PstPropertyValue>` —
  `Count`, `Contains`, `TryGetValue`, `GetValue` (miss →
  `PstFileException`), enumeration in BTH (property-id) order. Value
  payloads resolve at access time.
- `PstPropertyValue` (readonly struct) — `PropertyId`, `WireType` (raw
  MS-OXCDATA code), `RawData` (resolved little-endian payload), typed
  accessors `GetInt16/GetInt32/GetInt64/GetBoolean/GetSingle/GetDouble/
  GetGuid/GetString` (wire type `0x001F` UTF-16LE only — `0x001E` stays
  bytes; code pages are a format-layer concern) / `GetBytes`. Mismatch
  → `InvalidOperationException`. Multi-valued (`0x1xxx`) and
  `PtypObject` payloads surface raw; decoding them is P3's job.
- `PstTableContext` — `Columns` (`IReadOnlyList<PstTableColumn>`),
  `RowCount` (row-index record count), streaming `EnumerateRows()`
  (matrix order, one block resident at a time), `TryGetRow(rowId)`.
- `PstTableColumn` (readonly struct) — `PropertyId` (tag >> 16),
  `WireType` (tag & 0xFFFF), `Width` (`cbData`); `ibData`/`iBit` stay
  internal.
- `PstTableRow` — `RowId`, `TryGetCell(propertyId, out value)` (false
  when the column is absent or the existence bit is clear),
  `EnumerateCells()`; a row copies its `RowWidth` bytes so rows outlive
  the block buffer; variable cells resolve their HNID on access.

## 5. Validation levels

Mirrors the NDB precedent (`PstSource`): bounds/geometry and structural
signatures (`bSig 0xEC`, expected `bClientSig`, BTH `bType 0xB5`,
TCINFO `bType 0x7C`, `cbKey ∈ {2,4,8,16}`) are enforced at **every**
level; BTH leaf-key ordering, exact PC BTH shape, and unknown-wire-type
rejection are **Strict**-only; block CRC verification is already
governed per level by `PstSource.ReadBlock`. `Minimal` adds no
LTP-specific relaxation in P1 (documented on the readers).

## 6. Errors and resources

New keys in `src/IO.Pst/PstResourceStrings.resx` (all formatted with
`CultureInfo.CurrentCulture`):
`Format_Invalid_PstHeapNode`, `Format_Invalid_PstHeapId`,
`Format_Invalid_PstBTreeOnHeap`, `Format_Invalid_PstPropertyContext`,
`Format_Invalid_PstTableContext`, `Format_Invalid_PstPropertyWireType`
(all `PstFileFormatException`); `Op_Invalid_PstPropertyValueType`
(`InvalidOperationException` on mismatched typed accessors);
`IO_KeyNotFound_PstProperty` (`PstFileException`, mirroring
`IO_KeyNotFound_PstNode`). No new `PstUnsupportedFormatException`
sites — P1 introduces no new unsupported-variant surface.

## 7. Tests

- `test/IO.Pst.Internal/PstLtpFixtureBuilder.cs` — builds HN/BTH/PC/TC
  payload bytes; composed with the existing in-memory
  `PstFixtureBuilder`, so **no new binary fixtures are needed**.
- Internal BVT partials per reader: `PstDataTreeTests` (extended with
  `ResolveSegments_*`), `PstHeapNodeTests`, `PstBTreeOnHeapTests`,
  `PstPropertyContextReaderTests`, `PstTableContextReaderTests`.
- Public member-named partials: `PstNodeTests.ReadPropertyContext.cs` /
  `.ReadTableContext.cs`; `PstPropertyContextTests.{Count,TryGetValue,
  GetValue,Contains,IEnumerable}.cs`; `PstPropertyValueTests.Get*.cs`
  accessor backbone; `PstTableContextTests.{Columns,RowCount,
  EnumerateRows,TryGetRow}.cs`; `PstTableRowTests.{RowId,TryGetCell,
  EnumerateCells}.cs`. Two **Smoke** tests (the PC and TC happy paths).
- `test/IO.Pst/PstLtpCorpusTests.cs` (**Regression**) against the
  lspst oracle manifest (Unicode fixtures): folder display names
  (`0x3001`), message subjects (`0x0037`) and sender names (`0x0C1A`),
  Contents/Hierarchy table row counts vs the manifest, swept under
  Compatible **and** Strict, plus an every-node no-dangling-HNID sweep
  (each PC/TC value payload resolves without throwing). Raw property
  ids appear as literals with MS-OXPROPS comments — MAPI knowledge in
  *tests* does not breach the library boundary.

## 8. Commit sequencing (net-new; tests land alongside)

1. `PstDataTree.ResolveSegments` refactor + extended tests.
2. Heap-on-node: `PstHeapNode`, `PstHnid`, resx, `PstLtpFixtureBuilder`, tests.
3. BTree-on-heap: `PstBthHeader`, `PstBTreeOnHeap`, resx, tests.
4. Property context: `PstWireType`, `PstLtpContext`, `PstPcEntry`,
   `PstPropertyContextReader`, public `PstPropertyContext` /
   `PstPropertyValue`, `PstNode.ReadPropertyContext`, resx, tests.
5. Table context: `PstTcInfo`/`PstTcColumn`, `PstTableContextReader`,
   public `PstTableContext`/`PstTableColumn`/`PstTableRow`,
   `PstNode.ReadTableContext`, resx, tests.
6. Oracle corpus Regression + docs (CLAUDE.md Key Types, ROADMAP
   status, exploration doc P1 row marked executed).

Each commit gates on the project BVT run; the full Regression tier runs
before the final push.

## 9. Non-goals (P1)

MAPI semantics and folder/message traversal (P3); the name-to-id map
(P3); multi-valued / `PtypObject` / `PtypString8` decoding (raw
payloads only); OST-4K (still rejected at open; ANSI landed
2026-09-04); the decoded-block LRU, malformed-file fuzz sweeps, and
large-file memory-ceiling Regression (P2); writing, search machinery,
WIP encryption, and password handling (§9 of the exploration doc).
