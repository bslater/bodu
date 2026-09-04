# Outlook library hardening — security, exception discipline, performance

**Date:** 2026-09-03
**Status:** Phase A (tests) complete; Phase B (fixes) in progress.
**Scope:** `Bodu.Formats.Outlook` (shared model + `shared/` decode layer),
`Bodu.Formats.Outlook.Msg`, `Bodu.Formats.Outlook.Pst`, and the
`Bodu.IO.Pst` container.

A three-way audit of the Outlook stack found no wrong-answer bugs on the
real corpora, but it is not yet safe against hostile input: four
unbounded-work/allocation holes (one an uncatchable stack overflow), a
family of "wrong exception type escapes the documented contract"
defects, two silent-wrong-answer paths at the default validation level,
no caller-tunable resource limits, and a consistent 2–4× copy pattern
on payloads. Every finding was verified against source before this plan
was written.

## Ground rules

- **Tests first.** Every regression test was authored, committed, and
  confirmed failing (Phase A) before any production change (Phase B).
  Tests that need new API landed with declarations only — validated
  option properties with defaults, an enum member, method signatures
  whose bodies are the eventual contract — so the behavioural tests
  still went red.
- **No restructuring** beyond the established `shared/` source-compile
  precedent (`#if MSG / #elif OUTLOOK_PST`).
- **Limits are options; structural depth caps are constants.** A limit
  violation is a format failure at every validation level (container:
  `PstFileFormatException` + `PstFileError.LimitExceeded`; readers: the
  format exception). Validation level governs tolerance of *malformed*
  structure; limits govern *resource* budgets.

## Settled design

| Decision | Choice |
|---|---|
| Container limits | `PstFileOptions.MaxNodeDataLength` (256 MiB; materialization only — `OpenDataStream` stays unbounded) and `MaxDataTreeLeaves` (65,536; both paths). NBT/BBT depth 16 and BTH index levels 8 are constants. |
| Reader limits | `MaxEmbeddedMessageDepth` (16) and `MaxDecompressedRtfBytes` (64 MiB) on both reader option types; `OutlookMailStoreReaderOptions.MaxNodeDataLength` passes through. |
| `CompressedRtf` | `rawSize` above `min(payload × 8 + 4096, maxOutputBytes)` is malformed; output is pre-sized and stops at `rawSize`; truncated tokens are malformed. |
| Strict-mode corrections | `PT_NULL` / `PT_UNSPECIFIED` and a zero FILETIME decode as present-with-null. |
| Shared consolidation | `shared/MapiNamedPropertyRecords.cs` (NAMEID parser) and `shared/MapiBodies.cs` (HTML/RTF body decode); `.Msg` decoder unit tests linked into the `.Pst` test project. |
| Verified non-defect | `PstTableContext.TryGetRow`'s rows-per-block arithmetic matches MS-PST §2.3.4.4 (blocks are packed with ⌊8176 / row width⌋ rows); left as is. |

## Phase A red list (recorded 2026-09-03, before any fix)

Solution build green. Full Regression tier per project; every failure
below is a new test, and no previously green test regressed.

**Bodu.IO.Pst.Test** — 20 red of 308 (+2 excluded from the run because
they crash the test host today: `PstBTreeTests.EnumerateNodes_When
BranchPageReferencesItself_…` and `TryGetNode_WhenBranchPageReferences
Itself_…` — "Test host process crashed : Stack overflow"):
`ReadHeader_WhenIndexLevelsExceedSpecMaximum`,
`EnumerateRecords_WhenIndexItemReferencesItself`,
`ReadAllBytes_WhenLogicalPayloadExceedsMaterializationLimit`,
`ReadAllBytes_WhenCallerLowersMaterializationLimit`,
`OpenDataStream_WhenLeafCountExceedsLimit`,
`ReadBlock_WhenBlockIdentifierCollidesWithCachedPageKey`,
`ReadAt_WhenOffsetNearInt64Maximum`,
`EnumerateNodes_WhenNodeIdentifierExceeds32Bits`,
`TryGetValue_WhenRecordsAreUnordered_ForCompatible`,
`EnumerateRows_WhenMatrixIsLongerThanTheIndex`,
`EnumerateRowIds_WhenMatrixIsSubnodeResident_ShouldNotAllocatePerRow`,
`Open_WhenHeaderFileLengthExceedsStream`,
`Open_WhenStreamIsPositionedPastLeadingBytes`,
`Ctor_WhenIndexExceeds27Bits`, `GetGuid_WhenPayloadExceedsSixteenBytes`,
and the five `…ShouldReport…Category` guards.

**Bodu.Formats.Outlook.Test** — 2 red of 164:
`GetInt32_WhenStoredAsInt16_ShouldWiden`,
`GetInt64_WhenStoredAsNarrowerIntegerType_ShouldWiden`.

**Bodu.Formats.Outlook.Msg.Test** — 32 red of 216:
`Open_WhenBitFlipped_ShouldDecodeCleanOrThrowSanctionedFamily`
(`CompoundFileFormatException` leaks at Compatible),
`Decompress_WhenDeclaredRawSizeIsHuge` (`OutOfMemoryException`),
`Decompress_WhenTokenStreamExpandsPastDeclaredSize`,
`Decompress_When{Reference,Literal}TokenIsTruncated`,
`Decompress_WhenMaxOutputBytesBelowDeclaredSize`,
`Decompress_WhenBodyIsLarge` (8 MB for a 4 MB body),
`Decode_WhenStorageHoldsManyVariableLengthProperties` (55 s for 30,000
properties), `OpenMessage_WhenNestingExceedsMaxEmbeddedMessageDepth`,
`BodyRtf_WhenDecompressedSizeExceedsOption`, `BodyRtf_WhenReadTwice`,
`Dispose_WhenRootDisposed_ShouldInvalidateNestedMessage`,
`OpenContentStream_WhenByReference` (×3),
`TryConvertFileTime_When{ConvertedUnderNonUtcLocalZone,NearMaximum…}`,
`TryDecodeVariableValue_When{BytesNull,EncodingNull,Utf8PayloadCarriesByteOrderMark}`,
`GetEncoding_WhenMessageCodePageIsUtf16` (×2),
`Load_When{StringOffsetWrapsUnsigned,StringLengthWrapsUnsigned,PropertyIndexWrapsIntoWellKnownRange,StringNameIsWhitespace}`,
`Decode_WhenFileTimeZero` (×2), `Decode_WhenNullOrUnspecifiedType` (×2),
`Decode_WhenDeclaredSizeDisagreesWithStream`,
`EnumerateIndexed_WhenIndexIsDuplicatedUnderCompatible`.

**Bodu.Formats.Outlook.Pst.Test** — 14 red of 74:
`OpenContentStream_WhenPayloadIsLarge_ShouldNotCopyIt` (39 MB copied),
`OpenMessage_WhenNestingExceedsMaxEmbeddedMessageDepth`,
`BodyRtf_WhenDecompressedSizeExceedsOption`, `BodyRtf_WhenReadTwice`,
`Method_WhenDeclaredValueUndefined_ForStrictValidation`,
`DisplayName_WhenSessionDisposedAfterDecode`,
`Subject_WhenSessionDisposedAfterDecode`,
`Properties_WhenFolderDeclaresCodePage_ShouldBeInheritedByItsMessages`,
`RootFolder_WhenStoreObjectAbsent_ShouldStillDecodeFolders`,
`ReadRow_WhenFixedCellIsNarrowerThanItsType` (×2),
`Load_When{StringOffsetWrapsUnsigned,StringLengthWrapsUnsigned,PropertyIndexWrapsIntoWellKnownRange}`.

## Phase B tranches

- **H1** — process-killing and unbounded-work holes: B-tree depth and
  level checks, BTH index-level cap and descent-path check, data-tree
  materialization and fan-out limits, `CompressedRtf` bounds, embedded
  message depth.
- **H2** — exception contract and silent wrong answers: `.msg` container
  translation, the shared NAMEID parser, cache key split, container
  mediums, decoder UTC/null/BOM, disposal guards, strict cross-checks.
- **H3** — CPU amplification and accessor correctness: `.msg` stream
  index, widened accessor probes, folder encoding inheritance,
  store-node fallback.
- **H4** — allocation and copy reduction, each pinned by a measured
  guard.
- **H5** — parity, documentation alignment, regression closure.

## Status — landed 2026-09-03

All five tranches landed on `claude/next-up-n6wu7r` as one `fix(...)` commit
each (H1 `d65aca0`, H2 `9aef729`, H3 `729ebdc`, H4 `daa5a44`, H5 this
commit). Every Phase A red test is green; the Regression tiers of
`Bodu.IO.Pst.Test`, `Bodu.Formats.Outlook.Test`, `Bodu.Formats.Outlook.Msg.Test`,
and `Bodu.Formats.Outlook.Pst.Test` pass in full, as does the whole-solution
BVT.

Deviations from the plan as written, each forced by evidence during Phase B:

| Item | Plan | Landed | Why |
|---|---|---|---|
| SLENTRY `nid` width | Reject subnode identifiers above 32 bits in `PstSubnodeTree` alongside the NBT check. | NBT entries are rejected; SLENTRY keeps its low-dword read. | Real stores (the pstsdk `sample1.pst` corpus and the fixture builder alike) leave the SLENTRY upper dword uninitialized (`0xFC000000…`); rejecting it broke every subnode read. |
| `.msg` stream lookup | Index the property streams inside `MsgPropertyDecoder`; no `Bodu.IO.Compound` change. | `CompoundStorage.FindChild` builds a per-storage name index on its first lookup (read-only files only). | The reader can only open a stream by name, and `TryOpenStream` scanned the directory per call, so an index in the decoder could not make the decode linear. The change is internal, private, and transparent. |
| Linked decoder tests | Link the `.Msg` `CompressedRtf` / `MapiValueDecoder` / `MapiEncodingResolver` unit tests into the `.Pst` test project. | `MapiValueDecoderTests*` and `MapiEncodingResolverTests` are linked (namespace selected by `OUTLOOK_PST`); `CompressedRtfTests` is not. | The `CompressedRtfTests` root asserts the `.msg` exception type and shares helpers across its partials; the PST copy of `CompressedRtf` is exercised end to end by `OutlookMailMessageTests.Bodies` instead. |
| `PstTableContext.EnumerateRows` strict surplus | Throw when the matrix is longer than the index. | Throws under Strict only when the matrix carries whole rows beyond the index count, counted across every block. | Tolerant reads stop at the index; the strict check must not misfire on the last block's slack. |
| `OutlookMailAttachment.OpenContentStream` | Return `PstNode.OpenDataStream()` for a subnode-resident payload. | Wraps the decoded array read-only via `MemoryMarshal.TryGetArray`. | The property collection has already materialized the payload by the time the stream is opened; the zero-copy wrap meets the memory guard without a second read path. Streaming without materialization needs a container API for opening a property value as a stream — a follow-on. |

Follow-ons recorded, not in scope here: `PstDataStream` async/`Dispose`
overrides; the `.msg` `CompoundStream.AsMemory()` path for value streams that
never escape as owned arrays. The value-stream follow-on landed separately —
see below.

## Streaming property values — landed 2026-09-04

The last place a single property could force a large allocation was the
attachment payload: `OutlookMailAttachment.Properties` (and anything reading
it, `Method` and `FileName` included) materialized a subnode-resident
`PidTagAttachDataBinary` in full, capped by `MaxNodeDataLength`; the `.msg`
reader read the `__substg1.0_37010102` stream while decoding the attachment
storage. Landed tests-first on `claude/next-up-n6wu7r` (`test` `2580e08`,
then `fix` `dcfa3f9` container, `1220696` PST reader, `b752af6` `.msg`
reader, and the closing docs commit):

| Layer | Surface | Behaviour |
|---|---|---|
| `Bodu.IO.Pst` | `PstPropertyContext.TryGetValueLength` / `TryOpenValueStream`, `PstTableRow.TryGetCellLength` / `TryOpenCellStream`, `PstPropertyContext.EnumeratePropertyIds` / `TryGetWireType` | A heap-resident value is served from the heap's decoded bytes without a copy; a subnode-resident value sums its leaf lengths from the data tree's index blocks and streams through `PstDataStream`, so `MaxNodeDataLength` (materialization) does not apply. The id/wire-type accessors let a reader decide per property before touching a payload. |
| `Bodu.Formats.Outlook.Pst` | `OutlookMailStoreReaderOptions.MaxInlineAttachmentBytes` (1 MiB) | `PidTagAttachDataBinary` above the limit is a present property with a `null` value; `Method` reads presence, `Size` falls back to the store's recorded length, `OpenContentStream` streams block by block. |
| `Bodu.Formats.Outlook.Msg` | `OutlookMessageReaderOptions.MaxInlineAttachmentBytes` (1 MiB) | Same contract; the stream is sized from the directory (opening it under the buffered strategy would materialize it), and a deferred payload is served through `MsgContentStream`, which translates a container fault raised during a read. Combined with `CompoundReadStrategy.Streaming` the payload is never held in memory in full. |

Guards: the 318 MB subnode tree reports its length and streams under an
8 MiB ceiling at the container level; the 5-XBLOCK PST fixture and the 4 MiB
`.msg` fixture decode, report `Method` / `Size`, and stream under 8 MiB and
1 MiB ceilings respectively; small payloads keep decoding inline; the
malformed sweeps drain the new paths. Only the attachment payload is
deferred — widening deferral to other binaries is a separate decision.

