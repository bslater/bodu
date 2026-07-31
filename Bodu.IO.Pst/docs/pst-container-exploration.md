# Bodu.IO.Pst — MS-PST container exploration

**Date:** 2026-07-31
**Status:** Exploration / pre-implementation design note. No code
exists; this document records the format anatomy, the layering
decision, an API sketch, the fixture strategy, and the risks that must
be settled before implementation starts.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *New library
candidates → `Bodu.Formats.Outlook.Pst` / `Bodu.IO.Pst`*;
[`Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md`](../../Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md)
(the shared MAPI value model this container's format reader consumes).

---

## 1. Why a third low-level container

The repository's *container + format-reader split* (Architectural
patterns, `ROADMAP.md`) separates byte-level container knowledge from
format semantics: `Bodu.IO.Compound` owns CFB, the proposed
`Bodu.IO.Packaging` would own OPC, and format readers
(`Bodu.Formats.Excel.Binary`, the `.msg` reader) layer on top.

MS-PST does **not** fit either existing container. A `.pst` / `.ost`
file is its own three-layer structure with no CFB inside:

1. **NDB (Node Database)** — the storage layer: header, allocation
   maps, two B-trees (blocks and nodes), blocks with trailers, data
   trees for large payloads, subnode trees, and the optional
   "compressible encryption" byte permutation.
2. **LTP (Lists, Tables, Properties)** — the structured-data layer
   built on NDB nodes: Heap-on-Node, BTree-on-Heap, the Property
   Context (a property bag), and the Table Context (a typed row/column
   table).
3. **Messaging** — MAPI semantics: the store object, the folder
   hierarchy, messages, recipients, attachments, and the named-property
   map.

`Bodu.IO.Pst` owns layers 1 and 2 (the reusable, format-agnostic
substrate — the analogue of CFB sectors + directory in
`Bodu.IO.Compound`). Layer 3 belongs to the future
`Bodu.Formats.Outlook.Pst`, which translates LTP property bags and
tables into the shared `Bodu.Formats.Outlook` MAPI value model. The
boundary rule: **`Bodu.IO.Pst` never mentions MAPI** — it speaks node
ids, property ids, and wire type codes; folder/message/recipient
semantics never leak below the format package.

## 2. Format anatomy (what the implementation must cover)

### 2.1 File header and variants

- Magic `!BDN` (`dwMagic 0x4E444221`), client magic `SM`.
- `wVer` discriminates the on-disk shape: **14/15 = ANSI** (32-bit
  BIDs/IBs, the pre-2003 format), **23 = Unicode** (64-bit, the
  default since Outlook 2003), **≥ 36 = the 4 KiB-page OST variant**
  (WIP). Almost every structure below has an ANSI and a Unicode
  layout; the two differ in field widths and offsets, not concepts.
- `bCryptMethod`: `None (0)`, `Permute (1)` — a fixed byte-substitution
  table, `Cyclic (2)` — a rolling substitution keyed by the low DWORD
  of the block id, and the Windows-Information-Protection encrypted
  variant (`0x10`), which is out of scope. Permute/Cyclic apply to
  data-block payloads only, never to pages or metadata — this is
  obfuscation, not cryptography.
- Header and block trailers carry CRCs (the MS-PST §5.3 CRC — see
  risk R3) plus the ROOT record: file EOF, allocation-map validity,
  and the BREFs (block id + absolute offset) of the two B-tree roots.

### 2.2 NDB layer

- **Pages** are fixed 512-byte units with a page trailer (type, CRC,
  block id / signature). The two structural page types are the NBT
  (node B-tree) and BBT (block B-tree) `BTPAGE`s; allocation-map page
  types (AMap/PMap/FMap/FPMap/DList) exist for writers and can be
  skipped by a reader.
- **The BBT** maps a block id (BID) → `BREF` (absolute offset) +
  byte count + reference count. **The NBT** maps a node id (NID) →
  data BID, subnode BID, and parent NID.
- **Blocks** hold payloads up to 8176 bytes (8192 including the
  16-byte trailer; sizes rounded to 64-byte boundaries). Payloads
  larger than one block go through data trees: an **XBLOCK** (array of
  data BIDs) or **XXBLOCK** (array of XBLOCK BIDs). Node subnode trees
  use the parallel **SLBLOCK/SIBLOCK** shapes to give one node a
  private namespace of child nodes (this is where a message keeps its
  recipient table, attachment table, and attachment payloads).
- **NIDs** are 32-bit: 5 type bits + 27 index bits. The type
  partitions the namespace (normal folder, message, attachment,
  internal, contents-table, …) and well-known fixed NIDs anchor the
  file (message store `0x21`, name-to-id map `0x61`, root folder
  `0x122`) — those constants matter to the format layer, but NID
  parsing itself is container-level.

### 2.3 LTP layer

- **Heap-on-Node (HN):** turns one node (and its data tree) into a
  heap of small variable-size items addressed by 32-bit heap ids
  (HIDs). Multi-block heaps carry per-block page maps.
- **BTree-on-Heap (BTH):** a B-tree stored inside a heap; keyed
  lookups with fixed key/entry sizes declared in its header.
- **Property Context (PC):** a BTH keyed by 16-bit property id whose
  8-byte records carry the 16-bit wire type and a 4-byte
  value-or-reference: values ≤ 4 bytes are inline; larger values live
  in the heap (HID) up to ~3.5 KiB, and beyond that in a subnode
  (NID). This is the property bag behind every store / folder /
  message / attachment object.
- **Table Context (TC):** a typed table: a column-descriptor array
  (tag, offset, width, existence bit), a row-index BTH (row id → row
  number), and a row matrix stored across data blocks with a fixed
  rows-per-block stride. Folder listings and recipient/attachment
  tables are TCs.

## 3. Layering and package shape

| Package | Namespace | Owns |
| --- | --- | --- |
| `Bodu.IO.Pst` | `Bodu.IO.Pst` | Header/version/crypt handling, NBT/BBT lookups, block reads + data/subnode trees, permute/cyclic decoding, CRC validation, HN/BTH/PC/TC readers. Public surface speaks NIDs, property ids, and wire type codes. |
| `Bodu.Formats.Outlook.Pst` *(later)* | `Bodu.Formats.Outlook` | Store/folder/message/recipient/attachment semantics over PC/TC, named-property resolution, translation into the shared `Mapi*` value model. |

Notes on the split:

- **The LTP layer sits in the container package deliberately.** PC and
  TC are as format-agnostic as CFB's directory: they encode *structure*
  (typed bags and tables), not mail semantics, and the messaging layer
  cannot be parsed without them. Splitting LTP into a third package
  would serve no second consumer.
- **`Bodu.IO.Pst` returns wire-typed values** (`ushort` property type
  codes, raw little-endian payloads with typed accessors). The mapping
  to `MapiPropertyType` / `MapiProperty` is one thin translation in
  the format package — keeping the container free of the
  `Bodu.Formats.Outlook` dependency and usable by non-mail tooling
  (forensics, indexing).
- Dependencies: `Bodu.Core` only. Same csproj shape as
  `Bodu.IO.Compound` (`net8.0`, `RootNamespace Bodu`, flat folders
  `IO.Pst/` + `IO.Pst.Internal/`, `PstResourceStrings.resx`,
  `InternalsVisibleTo` the test assembly). Tier: **Preview**.

## 4. Public API sketch

```csharp
namespace Bodu.IO.Pst;

/// <summary>A disposable, read-only session over a PST/OST file.</summary>
public sealed class PstFile : IDisposable
{
    public static PstFile OpenRead(string path);
    public static PstFile Open(Stream stream, PstFileOptions options,
        bool leaveOpen = false);
    public static bool IsPstFile(Stream stream);        // magic sniff

    public PstFileFormat Format { get; }                // Unicode / Ansi / Ost4K
    public PstCryptMethod CryptMethod { get; }          // None / Permute / Cyclic

    public PstNode GetNode(PstNodeId id);               // throws PstNodeNotFoundException
    public bool TryGetNode(PstNodeId id, out PstNode node);
    public IEnumerable<PstNodeInfo> EnumerateNodes();   // NBT walk, on demand
}

/// <summary>A 32-bit node id: 5 type bits + 27 index bits.</summary>
public readonly struct PstNodeId : IEquatable<PstNodeId>
{
    public PstNodeId(uint value);
    public PstNodeId(PstNodeType type, uint index);
    public uint Value { get; }
    public PstNodeType Type { get; }
    public uint Index { get; }
}

/// <summary>One node: raw data access plus the LTP views over it.</summary>
public sealed class PstNode
{
    public PstNodeId Id { get; }
    public PstNodeId ParentId { get; }
    public long DataLength { get; }

    public Stream OpenDataStream();          // decoded, data-tree-flattened
    public byte[] ReadAllBytes();

    public bool TryGetSubnode(PstNodeId id, out PstNode subnode);
    public IEnumerable<PstNodeInfo> EnumerateSubnodes();

    public PstPropertyContext ReadPropertyContext();   // node must be a PC
    public PstTableContext ReadTableContext();         // node must be a TC
}

/// <summary>The LTP property bag: 16-bit property ids, wire-typed values.</summary>
public sealed class PstPropertyContext : IReadOnlyCollection<PstPropertyValue>
{
    public bool TryGetValue(ushort propertyId, out PstPropertyValue value);
}

/// <summary>One PC entry / TC cell: id, wire type code, raw payload,
/// and typed accessors (GetInt32 / GetString / GetBinary / …).</summary>
public readonly struct PstPropertyValue { … }

/// <summary>The LTP table: columns, row count, forward-only row reads.</summary>
public sealed class PstTableContext
{
    public IReadOnlyList<PstTableColumn> Columns { get; }
    public int RowCount { get; }
    public IEnumerable<PstTableRow> EnumerateRows();    // streaming, in row order
    public bool TryGetRow(uint rowId, out PstTableRow row);
}

public sealed class PstFileOptions
{
    public PstValidationLevel ValidationLevel { get; init; }  // Strict/Compatible/Minimal
    public int BlockCacheSize { get; init; }                  // decoded-block LRU budget
}

// Exceptions mirror the Compound hierarchy:
public class PstFileException : Exception { public PstFileError Error { get; } }
public class PstFileFormatException : PstFileException { … }
public sealed class PstNodeNotFoundException : PstFileException { … }
```

Shape rationale:

- **Streaming-first, unlike Compound's buffered default.** Real PSTs
  are multi-gigabyte; whole-file buffering is a non-option. All reads
  are random-access against the source stream through B-tree lookups,
  with a bounded LRU of *decoded* blocks (`BlockCacheSize`) as the only
  cache. `EvictingDictionary` from `Bodu.Collections` is the natural
  backing.
- **`PstValidationLevel` mirrors `CompoundValidationLevel`:** Strict
  verifies every trailer CRC and signature on the read path; Compatible
  checks structure but skips CRCs; Minimal is for salvage reads.
- The internal layer (`IO.Pst.Internal/`) holds the raw structures:
  `PstHeader`, `PstPage` / B-tree walkers, `PstBlock` + trailer,
  data/subnode tree flattening, the permute/cyclic tables, the §5.3
  CRC, `HeapOnNode`, `BTreeOnHeap`, and the PC/TC decoders the public
  types wrap.

## 5. Scope decisions

| # | Decision | Position |
| --- | --- | --- |
| P-D1 | Format variants | **Unicode (`wVer 23`) only at first.** ANSI and the 4 KiB OST variant are recognized and rejected with a precise `PstFileFormatException` (the `ExcelBinaryUnsupportedException` pattern); their layouts are kept in mind (width-parameterized readers) so adding them is additive. |
| P-D2 | Read/write | **Read-only, with authoring an explicit non-goal** for the foreseeable future — PST writing means allocation maps, free-space management, and CRC maintenance; no consumer needs it. |
| P-D3 | Crypt methods | Permute and Cyclic both ship day one (files with either are common); the WIP-encrypted variant (`0x10`) is rejected. |
| P-D4 | Search machinery | Search folders, search-update queues, and the DList are **skipped** — they are Outlook runtime state, not archive content. Their NIDs simply come back from `EnumerateNodes` untyped. |
| P-D5 | Concurrency | Same contract as `CompoundFile`: the session is single-threaded; no internal locking. Document it, don't guard it. |

## 6. Fixture strategy — the key pre-implementation blocker

Unlike `.msg` (authorable via `Bodu.IO.Compound`), **nothing in the
repository can author a PST**, so acquiring trustworthy fixtures is the
gating work item:

1. **Self-authored corpus (preferred anchor).** Generate small PSTs
   once, out-of-band, with Outlook or the Windows PST provider
   (`IPM.Note` trees of known content), check them in under
   `test/Fixtures/Reference/` with a `NOTICE.md` manifest recording
   provenance and the expected structure (folder counts, message
   subjects, property values) that Regression asserts against.
2. **Third-party corpora.** `libpff` and `java-libpst` carry test
   files; each candidate's licence must be verified before check-in
   (the Compound reference-corpus precedent).
3. **Hand-built structural cases.** Truncated headers, wrong CRCs,
   cyclic-encoded blocks, oversized-payload data trees — small
   binary fixtures built by a test-only helper that patches bytes in
   copies of the reference files (not a PST writer).

A minimal-scope spike (§7, P0) validates the fixtures before the LTP
work begins, so any corpus problem surfaces first.

## 7. Proposed sequencing

Gated on **M1 of the `.msg` plan** (the shared MAPI model) only for the
*format* package; the container below has no Outlook dependency and can
start independently:

| Tranche | Item | Notes |
| --- | --- | --- |
| **P0** | Spike: header + NBT/BBT walk + block read with permute/cyclic + CRC, against the reference corpus | Proves the fixtures and the §5.3 CRC question (R3). Ends with `PstFile.OpenRead` + `EnumerateNodes` + `OpenDataStream` working. |
| **P1** | LTP: HN, BTH, PC, TC surfaces | The bulk of the package. |
| **P2** | Hardening: validation levels, malformed-file sweeps, large-file streaming Regression, docs | Ships `Bodu.IO.Pst` (Preview). |
| **P3** | `Bodu.Formats.Outlook.Pst` (separate plan) | Folders / messages / recipients / attachments / named properties over P1's surfaces, in the shared value model. |

## 8. Risks and open questions

- **R1 — Spec-vs-reality drift.** Real-world PSTs (especially ones
  written by non-Outlook tools) deviate from MS-PST in padding,
  reference counts, and allocation metadata. Mitigation: the
  Compatible validation default + a corpus that includes third-party
  writers' output.
- **R2 — Fixture provenance.** No fixtures, no project (§6). This is
  resolved before P0 is declared done.
- **R3 — The §5.3 CRC.** MS-PST specifies its own table-driven 32-bit
  CRC. Whether it reduces to an existing `CrcStandard` catalogue entry
  in `Bodu.IO.Hashing` (initial value / finalization differ from
  CRC-32/ISO-HDLC) is checked in P0; if not, a small internal
  implementation ships in `IO.Pst.Internal` — it is not worth a
  public catalogue entry unless it maps cleanly.
- **R4 — Memory discipline.** B-tree pages and TC row matrices invite
  accidental materialization. The streaming-first rule (§4) is a
  design invariant, enforced by a Regression test that reads a
  multi-hundred-MB fixture under a memory ceiling.
- **R5 — ANSI/OST demand.** Deferring ANSI (P-D1) is a bet that
  post-2003 archives dominate. The width-parameterized internal
  readers keep the door open; revisit on the first real request.

## 9. Non-goals

- Writing / repairing PST files, and allocation-map maintenance.
- The WIP-encrypted OST variant (`bCryptMethod 0x10`).
- Search-folder evaluation and other Outlook runtime state (P-D4).
- Password handling: the PST "password" is a CRC stored in the store
  PC — it is not encryption, and honouring it would be theatre; the
  reader ignores it (and the format package can surface it as data).
- Any MAPI semantics below the format package (§3's boundary rule).
