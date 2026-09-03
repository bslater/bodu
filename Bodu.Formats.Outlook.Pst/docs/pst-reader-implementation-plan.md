# Bodu.Formats.Outlook.Pst — `.pst` messaging reader kickoff and implementation plan

**Date:** 2026-08-31
**Status:** Kickoff — tranches T0–T7 sequenced below; T0 is this
document. Deviations from the sketches will be recorded here at their
landing commits, following the `.msg` plan's convention.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *`Bodu.IO.Pst` → P2*;
[`Bodu.IO.Pst/docs/pst-container-exploration.md`](../../Bodu.IO.Pst/docs/pst-container-exploration.md)
— §7 (tranches P2/P3), §3 (the layering boundary);
[`Bodu.IO.Pst/docs/ltp-implementation-plan.md`](../../Bodu.IO.Pst/docs/ltp-implementation-plan.md)
— §9 (deferrals this plan absorbs);
[`Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md`](../../Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md)
— the template this plan mirrors, and deviation *(1)* (the shared
model's public constructors placed for this reader).

**Numbering reconciliation.** The container exploration's tranche table
names *hardening* **P2** and *the messaging reader* **P3** (separate
plans); `ROADMAP.md`'s `Bodu.IO.Pst` section merged both into a single
**P2** that is also the shipping gate. This plan executes
exploration-P2 *and* exploration-P3 under the roadmap's P2 label; both
§7 rows in the exploration doc are marked executed when T7 lands.

This plan turns the roadmap's `.pst` messaging candidate into
sequenced, scoped work. A `.pst` mail store is the NDB+LTP container
`Bodu.IO.Pst` already reads; this package adds only the messaging
layer — store, folder hierarchy, messages, recipients, attachments,
and named-property resolution — translating LTP property bags and
tables into the shared `Bodu.Formats.Outlook` MAPI value model. Per
the roadmap, the namespace flattens to **`Bodu.Formats.Outlook`** so
the value model stays shared with the `.msg` reader; this package
references the shared package and `Bodu.IO.Pst`, never
`Bodu.Formats.Outlook.Msg`.

---

## 0. Ground rules

Every tranche inherits the repository conventions
([`CLAUDE.md`](../../CLAUDE.md)); the ones that recur in this plan:

- **Tier.** The new package starts **Preview** (README tier badge);
  `Bodu.IO.Pst` stays Preview and both promote together once the
  surface has settled against a richer corpus.
- **Validation and messages.** Public surfaces validate through
  `ThrowHelper.ThrowIf…` (Core); all user-facing text lands in
  `OutlookPstResourceStrings.resx` with the established key prefixes —
  never string literals.
- **Tests.** MSTest partials with a member-named backbone; KAT rows via
  the `Bodu.Test.Kat` generics wired through `KatDisplayName`; new
  tests default to BVT, exhaustive sweeps are
  `[TestCategory("Regression")]`, one happy-path Smoke test per primary
  type. Tests are authored before the implementation they pin;
  defect-driven container changes land red-green (failing test commit
  first).
- **Docs.** Every public member owes full XML docs (CS1591 breaks the
  build). The DocFX treatment for this gate is the *full* set the
  roadmap prescribes — a `docs/docs/io-pst/` conceptual section, the
  `docs/apidoc/Bodu.IO.Pst.md` overview, a guide, and a runnable
  `samples/IO.Pst/` project — snippet-compile guarded.
- **Commit discipline.** One branch per session; each tranche lands as
  an ordered commit sequence — tests, production code, sweeps, docs —
  never one monolithic commit. Each tranche's final commit leaves
  `dotnet test bodu.slnx --settings bvt.runsettings` green.

## 1. Substrate-readiness review (MS-PST messaging vs `Bodu.IO.Pst`)

Reviewed 2026-08-31 against the shipped container surface. Unlike the
`.msg` review (which found the Compound substrate complete), this
review returns a short additive list — the hardening items the
container's own docs already deferred to this gate. Everything else
maps onto existing members:

| Messaging requirement | Container surface | Status |
| --- | --- | --- |
| Open read-only from path or stream; sniff without parsing | `PstFile.OpenRead(path/stream)` / `Open(stream, options)` / `IsPstFile` | ✓ |
| Reach the store object, name-to-id map, and root folder | `PstNodeId.MessageStore` (0x21) / `.NameToIdMap` (0x61) / `.RootFolder` (0x122) + `GetNode`/`TryGetNode` | ✓ |
| Read an object's property bag | `PstNode.ReadPropertyContext()` → `PstPropertyContext` / `PstPropertyValue` (wire-typed, `RawData` exposed) | ✓ |
| Walk a folder's hierarchy / contents / FAI tables | Compose `new PstNodeId(PstNodeType.HierarchyTable / ContentsTable / AssociatedContentsTable, folder.Index)`; `ReadTableContext()` streams rows; contents-row `RowId` = message NID (pinned by `PstLtpCorpusTests`) | ✓ |
| Reach a message's recipient / attachment tables and attachment objects | Message-node subnodes typed `RecipientTable` (0x12) / `AttachmentTable` (0x11) / `Attachment` (0x05) via `EnumerateSubnodes` / `TryGetSubnode` (pinned: subnodes 0x0692/0x0671 of message 0x00200024 in `sample1.pst`) | ✓ |
| Decode String8, multi-valued, and object-typed payloads | `PstPropertyValue.RawData` + `WireType` — decoding is deliberately this package's job (the container's LTP plan §9 deferral; boundary rule §3) | ✓ (by design) |
| Tolerate sloppy real-world writers | `PstValidationLevel` (Strict / Compatible / Minimal) | ✓ |
| Stream large attachment payloads without materialization | `PstNode.OpenDataStream()` — **currently buffers via `ReadAllBytes`** | **additive → T2** |
| Bounded re-read cost for per-message PC/TC access | No decoded-block cache — every read decodes fresh | **additive → T2** |
| Distinguish "node missing" from "file corrupt" | Both throw base `PstFileException` today | **additive → T2** |
| Attachment size without reading the payload | `DataLength` lives only on `PstNodeInfo`, not `PstNode` | **additive → T2** |

The four additive rows are container work this plan executes as
tranche **T2** (the exploration doc's own P2), before the messaging
surface consumes them. Nothing else comes back to `Bodu.IO.Pst`.

## 2. Package and namespace shape

| Package | Folder | Namespace | Responsibility |
| --- | --- | --- | --- |
| `Bodu.Formats.Outlook.Pst` | `Bodu.Formats.Outlook.Pst/` | `Bodu.Formats.Outlook` (public) + `Bodu.Formats.Outlook.Pst` (internal record layer) | The `.pst` messaging reader over `Bodu.IO.Pst`: the store session, folder walk, message/recipient/attachment surfaces, named-property resolution, and body conveniences. |

References: `Bodu.Core` + `Bodu.IO.Pst` + `Bodu.Formats.Outlook` +
`System.Text.Encoding.CodePages` — never `Bodu.Formats.Outlook.Msg`.
The internal layer mirrors `Bodu.Formats.Outlook.Msg` /
`Bodu.Formats.Excel.Biff8`: an internal namespace in a flat
`Formats.Outlook.Pst/` folder, granted to the test assembly via
`InternalsVisibleTo`.

### Settled decisions

| # | Decision | Position |
| --- | --- | --- |
| P1 | Public type family | **`OutlookMail*`** — `OutlookMailStore` / `OutlookMailFolder` / `OutlookMailMessage` / `OutlookMailAttachment` (+ `OutlookMailStoreReaderOptions`, `OutlookPstFormatException`). The flattened namespace already carries `OutlookMessage`/`OutlookAttachment` from `.Msg`, so the PST family needs distinct simple names; "mail store" is standard Outlook parlance for a PST. `Pst*` public names are rejected (that prefix belongs to the container namespace); the exception keeps the format-naming convention (`OutlookMsgFormatException` precedent). |
| P2 | Shared decode logic | **A `Bodu.Formats.Outlook/shared/**` source-compile layer** (the `Bodu.IO.Hashing/shared` / `Bodu.Text.Serialization/shared` pattern), compiled into each reader under its format symbol: `MapiValueDecoder` (pure `Try*` scalar / packed fixed-width MV / FILETIME decode — no throw sites; each format decides skip-vs-throw), `MapiEncodingResolver` (code-page resolution, Windows-1252 fallback, parent inheritance), `CompressedRtf` (MS-OXRTFCP). Cross-package `InternalsVisibleTo` is rejected (it would reverse the `.msg` plan's deviation *(1)*); compiling the resolver into the shared *package* is rejected (it would drag `System.Text.Encoding.CodePages` into the dependency-free model, breaking its D3). `.Msg` migrates onto the shared source in the same tranche, its existing suite guarding the refactor. |
| P3 | Format symbol | **`OUTLOOK_PST`**, not `PST` — the container already claims `PST` for the shared CRC core's namespace switch (`Bodu.IO.Pst.csproj` / `CrcCore.cs`). `Bodu.IO.Hashing/shared/CrcCore.cs` and the new shared files gain `#elif OUTLOOK_PST` branches mapping to this package's namespaces. |
| P4 | `OutlookRecipient` | **Moved to the shared package** (`Bodu.Formats.Outlook/src/Formats.Outlook/`). It is already container-free (wraps a `MapiPropertyCollection`); both readers surface it. Nothing is published to nuget.org yet, so the public-type move is free. `OutlookAttachment` stays in `.Msg` (container-coupled). |
| P5 | Session semantics | **The store owns everything.** `OutlookMailStore` owns its `PstFile` (and stream unless `leaveOpen`); folders/messages/attachments are views bound to the session; embedded messages share, never own. Single-use, single-threaded — documented, not guarded (container P-D5). |
| P6 | Laziness | **Streaming-first, decode-once.** Open parses the header + store PC only; the named-property map parses on first lookup (a PST map can be arbitrarily large — deliberately *not* the `.msg` D5 eager parse); folder/message enumerations stream TC rows block-at-a-time; each object decodes its PC once on first `Properties` access. No whole-tree materialization. TC-row projection fast paths (reading display names from hierarchy-row cells) are not built — the T2 block cache makes PC reads cheap. |
| P7 | Missing vs malformed tables | **Missing table node = empty enumeration** (real PSTs omit empty tables); a *present but malformed* table throws per validation level (`OutlookPstFormatException` for messaging-level violations, the `PstFile*` family for container corruption). |
| P8 | Well-known property ids | **Curated additions to `MapiPropertyIds`** (folder counts/flags, store entry ids, message-list columns, attachment extras — §3). The full-catalogue generator remains a future tooling candidate (`.msg` D7). |
| P9 | Named properties | **Identifier-level bidirectional lookup on the store** (`TryGetNamedPropertyId` / `TryGetPropertyName`), matching the `.msg` surface (its deviation *(2)*): the NameToIdMap (node 0x61) is store-wide in MS-PST, so the map lives on `OutlookMailStore`, not per message. |

## 3. Public API sketch

```csharp
namespace Bodu.Formats.Outlook;

/// <summary>A disposable, read-only session over a PST mail store.</summary>
public sealed class OutlookMailStore : IDisposable
{
    public static OutlookMailStore OpenRead(string path);
    public static OutlookMailStore OpenRead(Stream stream, bool leaveOpen = false);
    public static OutlookMailStore Open(Stream stream, OutlookMailStoreReaderOptions options, bool leaveOpen = false);
    public static bool IsPstFile(Stream stream);          // delegates to PstFile.IsPstFile

    public MapiPropertyCollection Properties { get; }     // store PC (node 0x21), lazy
    public string? DisplayName { get; }                   // PidTagDisplayName convenience
    public OutlookMailFolder RootFolder { get; }          // node 0x122, lazy

    public bool TryGetNamedPropertyId(MapiNamedProperty name, out ushort id);
    public bool TryGetPropertyName(MapiPropertyTag tag, out MapiNamedProperty name);
}

/// <summary>One folder in the store hierarchy.</summary>
public sealed class OutlookMailFolder
{
    public string? DisplayName { get; }                   // 0x3001
    public string? ContainerClass { get; }                // 0x3613
    public int? MessageCount { get; }                     // 0x3602
    public int? UnreadCount { get; }                      // 0x3603
    public bool HasSubfolders { get; }                    // 0x360A; false when absent
    public MapiPropertyCollection Properties { get; }     // folder PC, lazy

    public IEnumerable<OutlookMailFolder> EnumerateSubfolders();          // hierarchy TC, streaming
    public IEnumerable<OutlookMailMessage> EnumerateMessages();           // contents TC, streaming
    public IEnumerable<OutlookMailMessage> EnumerateAssociatedMessages(); // FAI TC, streaming
}

/// <summary>One message, decoded from its property context and subnode tables.</summary>
public sealed class OutlookMailMessage
{
    public MapiPropertyCollection Properties { get; }     // lazy, decoded once
    public string? Subject { get; }                       // 0x0037, U+0001 prefix-marker normalized
    public string? SenderName { get; }
    public string? SenderEmailAddress { get; }
    public string? MessageClass { get; }
    public string? InternetMessageId { get; }
    public string? TransportMessageHeaders { get; }
    public DateTimeOffset? SentTime { get; }              // 0x0039
    public DateTimeOffset? ReceivedTime { get; }          // 0x0E06
    public string? BodyText { get; }                      // 0x1000
    public string? BodyHtml { get; }                      // 0x1013 via resolved code page
    public string? BodyRtf { get; }                       // 0x1009 via shared CompressedRtf
    public IReadOnlyList<OutlookRecipient> Recipients { get; }
    public IReadOnlyList<OutlookMailAttachment> Attachments { get; }
}

/// <summary>One attachment: metadata plus content access.</summary>
public sealed class OutlookMailAttachment
{
    public OutlookAttachmentMethod Method { get; }        // 0x3705
    public string? FileName { get; }                      // 0x3707 preferred over 0x3704
    public string? ContentId { get; }                     // 0x3712
    public string? MimeTag { get; }                       // 0x370E
    public long? Size { get; }                            // 0x0E20
    public MapiPropertyCollection Properties { get; }     // attachment PC (subnode 0x05), lazy

    public Stream OpenContentStream();                    // ByValue: streams 0x3701 via the container
    public OutlookMailMessage OpenMessage();              // EmbeddedMessage: 0x3701 PtypObject subnode PC
}

/// <summary>Options for opening a mail store.</summary>
public sealed class OutlookMailStoreReaderOptions
{
    public PstValidationLevel ValidationLevel { get; init; }  // the one sanctioned container type
    public int BlockCacheSize { get; init; }                  //   on the surface (.msg precedent)
    public bool DecompressRtf { get; init; } = true;
}

public sealed class OutlookPstFormatException : OutlookFormatException { … }
```

Design notes:

- **Boundary rule honored** (exploration §3): no `Pst*` type appears on
  the surface except `PstValidationLevel` on the options — the exact
  analogue of `CompoundValidationLevel` on `OutlookMessageReaderOptions`.
  `PstNodeId` never leaks; object identity stays internal.
- **`MapiPropertyIds` additions** (shared package, additive):
  `ContentCount 0x3602`, `ContentUnreadCount 0x3603`,
  `Subfolders 0x360A`, `ContainerClass 0x3613`, `MessageFlags 0x0E07`,
  `MessageSize 0x0E08`, `HasAttachments 0x0E1B`, `CreationTime 0x3007`,
  `LastModificationTime 0x3008`, `EntryId 0x0FFF`, `RecordKey 0x0FF9`,
  `IpmSubTreeEntryId 0x35E0`, `IpmWastebasketEntryId 0x35E3`,
  `FinderEntryId 0x35E7`, `LtpRowId 0x67F2`, `LtpRowVer 0x67F3`,
  `AttachNumber 0x0E21`, `AttachMimeTag 0x370E`,
  `ReceivedByName 0x0040`, `SentRepresentingName 0x0042`,
  `SentRepresentingEmailAddress 0x0065`, `ConversationTopic 0x0070`,
  `NormalizedSubject 0x0E1D`. No new `MapiPropertyCollection` accessors
  this pass (`GetInt16` / Currency-decimal / non-string MV arrays are
  demand-driven follow-ons).

## 4. Internal structure (`Bodu.Formats.Outlook.Pst`, internal)

| Type | Responsibility |
| --- | --- |
| `PstMapiPropertyReader` | `PstPropertyContext` / `PstTableRow` → `MapiPropertyCollection`: wire code → `MapiPropertyType`, shared scalar + packed fixed-width MV decode, the PST variable-size MV layout (`ulCount` + offset table per MS-PST §2.3.3.4), String8 via the shared encoding resolver, `PtypObject` surfaced raw. **Verification basis:** the spec text itself was unreachable from this environment (external fetches are policy-blocked), so the variable-size MV layout — `ulCount:u32`, then `ulCount` offsets from the payload start, element *i* spanning offset *i* to offset *i+1* (the last to the payload end) — is pinned by synthetic KAT fixtures plus decode consistency across the reference corpus, and matches the layout the established open-source PST readers implement. |
| `PstTableRowReader` | Streams TC rows into per-row collections; carries the contents-row → message-NID convention. |
| `PstStoreLayout` | Composes table NIDs from folder NIDs; locates recipient/attachment subnode tables; the U+0001 subject-prefix normalization. |
| `PstNamedPropertyMap` | Parses the NameToIdMap (0x61) PC: bucket count `0x0001`, GUID stream `0x0002`, entry stream `0x0003` (8-byte NAMEID records), string stream `0x0004`; bucket properties `0x1000+` ignored on read. Layout re-verified against MS-PST §2.4.7 and probed against the corpus before coding — **probe outcome (T5):** both Unicode fixtures carry the node with the expected shape (`sample1.pst`: `0x0001` Int32, `0x0002` 160 bytes = 10 GUIDs, `0x0003` 1376 bytes = 172 records, `0x0004` 1224 bytes, plus `0x1000+` bucket binaries); the first NAMEID record is the numeric name `0x8205`, GUID index 6 (kind bit 0, stream entry 0), `wPropIdx` 0 → identifier `0x8000`, and the populated string stream carries string-named entries — pinned by the Regression corpus test. The parse mirrors `MsgNamedPropertyMap` (the NAMEID record and GUID-index scheme are shared between MS-OXMSG and MS-PST); it stays format-local because its inputs are PC values, not compound streams. |

## 5. Project scaffolding

- Csproj follows `Bodu.Formats.Outlook.Msg.csproj`: `net8.0`,
  `<RootNamespace>Bodu</RootNamespace>`, `InternalsVisibleTo` for the
  test assembly, `OutlookPstResourceStrings.resx` + Designer,
  `DefineConstants` `OUTLOOK_PST`, shared-source Compile Includes for
  `Bodu.Formats.Outlook/shared/**` and (for `CompressedRtf`'s CRC)
  `Bodu.IO.Hashing/shared/**`.
- Solution: one `bodu.slnx` folder with src + test entries.
- **Builder visibility:** the test project references
  `Bodu.IO.Pst/test/Bodu.IO.Pst.Test.csproj`, and that project grants
  `InternalsVisibleTo("Bodu.Formats.Outlook.Pst.Test")` — the container
  fixture builders' bodies use container internals, so source-linking
  them would demand a far wider grant; their call surfaces are
  BCL-typed, so the assembly reference works and keeps them
  single-sourced. The grant also reaches `PstReferenceFixtures` and the
  embedded corpus.
- README with the **Preview** badge; `CLAUDE.md` and `ROADMAP.md` rows
  land with the shipping tranche.

## 6. Fixtures and tests

**Synthetic fixtures** are authored with the container test project's
`PstFixtureBuilder` + `PstLtpFixtureBuilder`, composed by a new
`PstMessagingFixtureBuilder` into mini mail stores: store PC, root +
user folders with hierarchy/contents/associated TCs, messages with
recipient/attachment subnode TCs and attachment PCs, an
embedded-message `PtypObject` subnode, String8 and PST-layout MV
values, and a synthetic NameToIdMap with numeric- and string-named
entries. Malformed knobs: missing tables, dangling row NIDs, wrong
wire types, truncated name-map streams, `EmbeddedMessage` without
`0x3701`.

**The reference corpus** (pstsdk fixtures + the `lspst` seed manifest,
already under `Bodu.IO.Pst/test/Fixtures/Reference/`) anchors
Regression at its oracle ceiling: user folder display names via the
walk, the known (sender, subject) pair via `EnumerateMessages`,
message-count floors, an every-object full-decode sweep under
Compatible *and* Strict, and a name-map parse when node 0x61 is
present. Recipient rows, attachment names, bodies, and dates have no
corpus oracle — their coverage is synthetic-primary, by design;
regenerating a richer manifest out-of-band is a recorded follow-on.

Test layout (per the conventions): `OutlookMailStoreTests.cs` +
`.OpenRead/.Properties/.RootFolder/.NamedProperties/.Dispose.cs`,
`OutlookMailFolderTests.*`, `OutlookMailMessageTests.*` (incl.
`.Recipients/.Attachments/.Bodies`), `OutlookMailAttachmentTests.*`,
internal-layer partials per member, and manifest-driven `IKat` corpus
records. KATs: `ValidKat<,>`/`InvalidKat<>` for MV-layout and NAMEID
record vectors; Smoke: one `OpenRead` happy path over the minimal
synthetic store.

## 7. Tranches

| Tranche | Item | Depends on | Notes |
| --- | --- | --- | --- |
| **T0** | This plan document | — | Commit first. |
| **T1** | Shared model + decode layer: `MapiPropertyIds` additions, `OutlookRecipient` move, `shared/{MapiValueDecoder,MapiEncodingResolver,CompressedRtf}.cs`, `CrcCore` `OUTLOOK_PST` branch, `.Msg` migration | T0 | The `.Msg` suite guards the refactor. |
| **T2** | Container hardening (exploration-P2): `BlockCacheSize` + decoded-block LRU in `PstSource`, streaming `PstDataStream` behind `OpenDataStream`, `PstNode.DataLength`, per-instance subnode caching, `PstFileError` + `PstNodeNotFoundException`, patch-a-copy malformed sweeps, mega-logical-node memory-ceiling Regression | T0 | Red-green: the memory-ceiling and cache byte-count tests land failing first. |
| **T3** | Package scaffolding + store/folder walk: projects, slnx, resx, IVT; internal readers + layout (MV layout verified against §2.3.3.4 first); `OutlookMailStore` / `OutlookMailFolder` / options / exception; `PstMessagingFixtureBuilder`; Smoke + backbones | T1, T2 | |
| **T4** | `OutlookMailMessage` + `OutlookMailAttachment`: subnode table walk, recipients, attachments, embedded messages (§2.4.6.3 verified), subject normalization | T3 | |
| **T5** | Named properties (§2.4.7 verified + corpus probe recorded) + bodies (code pages, RTF) | T4 | |
| **T6** | Regression closure: corpus / malformed / memory sweeps; full `regression.runsettings` for `Bodu.IO.Pst` + both Outlook packages | T5 | |
| **T7** | Shipping gate: release manifest (+`Bodu.IO.Pst`, +`Bodu.Formats.Outlook.Pst`) + `BoduBaseVersion` → 0.4.0; `CLAUDE.md` / `ROADMAP.md` / exploration-doc §7 rows; `docs/docs/io-pst/` + `docs/apidoc/Bodu.IO.Pst.md`; `samples/IO.Pst/` (PST cannot be authored by the sample — it ships a corpus copy with NOTICE or takes a path argument; decided at landing); package icon; package-matrix row; then droppable extras (guides, hero art, Outlook apidoc pages) | T6 | Ordered by droppability. |

## 8. Out of scope

- **Writing `.pst` files** — the container's P-D2 stands; authoring is
  a non-goal for the foreseeable future.
- **ANSI (`wVer` 14/15) and OST-4K stores** — recognized and rejected
  by the container (P-D1); demand-driven follow-ons.
- **MAPI session semantics** (`IMsgStore`/`IMAPIFolder` emulation,
  search-folder evaluation, rule processing) — this is a file reader.
- **The PST "password"** — a CRC in the store PC, not encryption; the
  raw property is reachable through `Properties`; honouring it would
  be theatre (exploration §9).
- **RTF→HTML de-encapsulation (MS-OXRTFEX)** and **S/MIME
  processing** — same posture as the `.msg` reader.
- **TC-row projection fast paths** and a **full property-id catalogue
  generator** — recorded follow-ons, not this gate.
