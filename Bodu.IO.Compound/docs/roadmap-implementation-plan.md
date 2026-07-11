# Bodu.IO.Compound roadmap — implementation plan

**Date:** 2026-07-11
**Status:** T0 done (roadmap truth-up); **T1–T4 executed 2026-07-11**; T5
(the `.msg` substrate review) remains open and gates the `.msg` project,
not this package.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *Per-project roadmap →
`Bodu.IO.Compound`*

> **T1–T4 executed 2026-07-11.** All four landed additively (no committed
> surface changed) with the golden v3/v4 fixtures byte-stable throughout.
> Notable deviations from the sketch below, each a deliberate outcome:
>
> 1. **T1a widened beyond vectors.** Full reader/writer scalar symmetry
>    was in scope, but the emitter also needed `Empty`/`Null` bodies and
>    a CLR-type→VT inference table for variant vectors (the reader
>    flattens `VT_VARIANT` vectors to a typeless `object?[]`), so variant
>    round-trips guarantee *value* identity, not byte identity. The
>    latent Unicode-dictionary asymmetry was fixed in the same item as
>    its own commit.
> 2. **T2 kept `MemoryStream`; no new builder member.** The exploration
>    confirmed `PooledBufferBuilder` is append-only and unfit for a
>    seekable cursor, and that `CompoundStreamBuilder.Content`'s setter
>    already stores `ReadOnlyMemory<byte>` without copying — so the
>    zero-copy dispose-time transfer needed no new API.
> 3. **T4 duplicated only the byte-moving layer.** `WriteTo`'s geometry
>    was extracted into `PreparePlan` / `EmissionPlan` (its own
>    golden-proven-neutral commit) so `WriteToAsync` shares one plan; the
>    `Emit*` twins fork only at the `SectorWriter` `ValueTask` methods.
>    `CfbStreamDataSource`'s monitor lock became a `SemaphoreSlim`
>    guarding both sync and async reads. Cancellation lands as
>    `OperationCanceledException` (or its `TaskCanceledException`
>    subtype), so its tests assert the base type.

This plan turns the `Bodu.IO.Compound` section of the repository roadmap
into sequenced, scoped work. The roadmap entry carries one concrete item
(**writable stream cursors**) and one directional note (**substrate for
new office-format readers**). A capability audit of the tree (§1) shows
the concrete item has **already been delivered** — the roadmap text is
stale — so this plan pivots: tranche T0 records that delivery and
corrects the roadmap, and tranches T1–T5 cover the genuine remaining
gaps the audit surfaced: property-set write-back symmetry, the writable
cursor's memory model, entry metadata on the edit surface, a true-async
commit/read path, and the substrate-readiness gate for the proposed
`.msg` reader.

Unlike the `Bodu.Core` plan this one operates on a **Stable-tier**
package (see *API-stability tiers* in `ROADMAP.md`; the README carries
the tier badge). Every code tranche is therefore **additive-only**: new
members and new option properties, no signature or behaviour changes to
the committed surface.

---

## 0. Ground rules

Every tranche inherits the repository conventions (see
[`CLAUDE.md`](../../CLAUDE.md)); the ones that recur in this plan:

- **Stability.** `Bodu.IO.Compound` is Stable. Additive API only;
  behavioural changes must be opt-in through `CompoundBuildOptions` /
  `CompoundFileOptions`. Package validation guards the surface.
- **Validation and messages.** Public surfaces validate through
  `ThrowHelper.ThrowIf…` (Core) or `CompoundThrowHelper`; all new
  user-facing text lands in `CompoundResourceStrings.resx` with the
  established key prefixes (`Arg_Invalid_*`, `Op_NotSupported_*`,
  `Format_Invalid_*`) — never string literals.
- **Tests.** MSTest partials with a member-named backbone
  (`CompoundStorageTests.WritePropertySet.cs`, not a catch-all file);
  KAT rows via the `Bodu.Test.Kat` generics wired through
  `KatDisplayName`; new tests default to BVT, exhaustive sweeps are
  `[TestCategory("Regression")]`, one happy-path Smoke test per new
  primary surface. The reference corpus under `test/Fixtures/Reference/`
  (provenance in its `NOTICE.md`) and the writer goldens
  (`golden-v3.cfb` / `golden-v4.cfb`) are the regression anchors —
  golden bytes must not change unless a tranche explicitly says so.
- **Docs.** Every new public member owes full XML docs (CS1591 breaks
  the build). The guide pages under `docs/guides/io-compound/` and the
  runnable samples under `samples/IO.Compound/` are snippet-compile
  guarded (`DocumentationSnippetCompileTests`) — API additions update
  the guide *and* keep the guards green.
- **Commit discipline.** One branch per session; each lettered item
  lands as an ordered commit sequence — *(1)* production code with XML
  docs, *(2)* test backbone, *(3)* Regression sweeps, *(4)* guide/sample
  updates — never one monolithic commit, never two items in one commit.
  Each item's final commit leaves
  `dotnet test bodu.slnx --settings bvt.runsettings` green.

## 1. What the audit found

The roadmap bullet reads: *"`CompoundStream` is a read-only cursor
today; mutation goes through `CreateStream(content)` or the builders."*
That is no longer true. Verified against the tree (2026-07-11):

**Already delivered** (the bullet's entire ask, and more):

| Capability | Evidence |
| --- | --- |
| Writable, seekable stream cursors | `CompoundStream` writable ctor over a staging node (`CompoundStream.cs:152`); `Write` / `SetLength` / `Flush` mutate and flush into the staging tree (`:401–506`) |
| BCL-style open semantics | `CompoundStorage.OpenStream(name, FileMode, FileAccess)` (`CompoundStorage.cs:239`), `TryOpenStream(…)` (`:351`), `CreateStream(name)` returning a writable cursor (`:391`); `CompoundFile.Open(stream, FileMode, FileAccess)` mirroring `Package.Open` (`CompoundFile.cs:318`) |
| Read + edit + author lifecycle | Update mode loads a staging snapshot (`OpenUpdateCore`, `CompoundFile.cs:445`); `Commit` rewrites the container, `Revert` restores the baseline (`:727`, `:766`) |
| Version 3 **and** 4 read *and* write | Reader accepts sector shift 9/12 (`CfbHeader.cs:173`); writer emits either from `CompoundBuildOptions.Version` (`CompoundBuildOptions.cs:27,39`; `CompoundContainerLayout.cs:328–337`); golden fixtures for both |
| Mini-stream / MiniFAT emission | 4096-byte cutoff, mini-sector layout, MiniFAT chains (`CompoundContainerLayout.cs:40,257–270,378–404`) |
| Streaming-scale authoring | Deferred payload sources — `AddStream(name, Func<Stream>, length)` (`CompoundStorageBuilder.cs:156`), `AddStreamFromFile` (`:172`), lazy `FromFile(file, lazy: true)` (`CompoundStorageBuilder.Serialization.cs:132`) — serialized through a pooled 80 KB buffer without materializing (`CompoundContainerLayout.cs:519–551`) |
| Balanced directory trees | Median-split sibling-tree build, ~log n height (`CompoundContainerLayout.cs:240–250`) |
| Validation / read-strategy options | `CompoundValidationLevel` (Strict/Compatible/Minimal), `CompoundReadStrategy` (Buffered/Streaming/Auto) |

**Genuine gaps** (the substance of this plan):

1. **Property-set write-back asymmetry.** `CompoundFile` /
   `CompoundStorage` expose read conveniences
   (`TryGetSummaryInformation`, `TryOpenPropertySet`) but no write
   counterpart — embedding requires manual
   `AddStream(SummaryInformation.StreamName, builder.ToArray())`. Worse,
   the reader parses vector-valued properties
   (`PropertySetReader.cs:227`) but `PropertySetWriter` throws
   `CompoundFileSerializationException` for them
   (`PropertySetWriter.cs:137`), so a property set read from a real
   file cannot always be re-emitted.
2. **Writable-cursor memory model.** A writable cursor buffers the whole
   payload in a `MemoryStream` and flushes via
   `SetContent(_write.GetBuffer()…)` (`CompoundStream.cs:502–506`),
   which copies again into a fresh array
   (`CompoundStreamBuilder.cs:212`) — double buffering, `int`-capped
   (~2 GB), with `MemoryStream`'s doubling growth on the LOH.
3. **Entry metadata is authoring-only.** `CompoundEntryBuilder` exposes
   settable `ClassId` / `CreationTime` / `ModifiedTime` / `StateBits`
   (`CompoundEntryBuilder.cs:71–89`), but the live edit surface
   (`CompoundStorage`) has no way to set them — relevant because Office
   consumers key off the root storage CLSID.
4. **No true async.** Every `*Async` member is a sync-over-async wrapper
   (`CompoundStream.cs:328–359,433–484`); `Commit` is sync-only.
5. **Substrate readiness for `.msg` is unproven.** The roadmap's *New
   library candidates* propose `Bodu.Formats.Outlook.Msg` over this
   container; nobody has walked MS-OXMSG against the current surface.

No `TODO` / `NotImplementedException` stubs exist anywhere in `src/` —
the gaps above are absences, not half-finished paths.

## 2. Settled decisions

| # | Decision | Position |
| --- | --- | --- |
| D1 | The "writable stream cursors" roadmap bullet | **Delivered — retire it via a roadmap truth-up (T0), no new cursor API.** |
| D2 | Commit model | **Full-rewrite stays; no in-place / transacted commit.** Large payloads route through deferred sources; the writable cursor stays in-memory with a documented, enforced cap (T2). |
| D3 | Property-set write-back | **Ship conveniences on `CompoundStorage` / `CompoundFile` plus vector emit in `PropertySetWriter` (T1) — the highest-leverage open item.** |
| D4 | Entry metadata on the edit surface | **Settable properties on `CompoundStorage` (storages only), no auto-stamping (T3).** |
| D5 | Async scope | **`CommitAsync` + true-async streaming reads only; buffered reads keep the sync wrappers (T4).** |

**D1 — record, don't rebuild.** The delivered surface
(`OpenStream(name, mode, access)` with `Package.Open` semantics, staged
writes, `Commit`/`Revert`) matches the bullet's intent — "round out the
`IStream` counterpart" — as closely as a managed `Stream` model can.
The remaining distance to COM `IStream` is transacted per-stream commit,
which D2 rejects. What the bullet needs is retirement, not code.

**D2 — keep the full-rewrite model.** The README already declares
incremental in-place editing (the COM `IStorage`/`Commit` transacted
model) out of scope (`README.md:99–104`), and the update path's
whole-file snapshot (`ReadAllBytes`, `CompoundFile.cs:451`) is
consistent with that. An in-place committer is a different, much larger
project (free-sector management, DIFAT growth, torn-write safety) with
no consumer demanding it — every known consumer (`.xls`, `.doc`,
`.msg`) rewrites documents whole. T2 therefore attacks the *avoidable*
costs (double copies, doubling growth, unclear cap) rather than the
model. *Reversal cost:* none — an incremental committer could still be
added later behind a new open mode without disturbing this surface.

**D3 — property-set symmetry before anything else.** It is the only gap
that blocks a real round-trip today (read a document with vector
properties → cannot re-emit its property set), and the fix is small,
additive, and immediately useful to the `.msg` candidate (MAPI files
carry `\x05` property-set streams too).

**D4 — storages only, no streams, no auto-stamping.** MS-CFB §2.6.1
requires stream entries to carry zero CLSID/timestamps/state, and the
writer already enforces that (`CompoundContainerLayout.cs:741–752`) —
so the edit surface only needs the storage side. Auto-stamping
`ModifiedTime` on `Commit` is deliberately excluded: it would make
byte-identical re-saves impossible and break the golden-fixture tests;
callers who want timestamps set them explicitly.

**D5 — async where it buys I/O, sync where it's a memcpy.** Buffered
reads and writable-cursor writes are pure memory operations; wrapping
them in `Task` machinery is the correct current behaviour, not a gap.
The genuinely blocking paths are `Commit` (destination writes +
deferred-source copies) and streaming-mode sector reads. Those two get
real async; nothing else changes.

---

## 3. T0 — Roadmap and docs truth-up

**Scope.** No code. Correct the stale claims:

- `ROADMAP.md` → `Bodu.IO.Compound`: retire the "writable stream
  cursors" bullet — record it as delivered with a one-line description
  of the semantics that shipped (BCL `FileMode`/`FileAccess` opens,
  staging-backed writes, whole-container rewrite on `Commit`), the way
  executed items are annotated elsewhere in the file. Replace the
  forward list with the T1–T5 items of this plan (directional bullets,
  first bullet = T1) and point at this document.
- `ROADMAP.md` current-state line: update the file counts and mention
  edit mode explicitly ("read + edit + authoring" is already right; the
  cursor sentence is not).
- `CLAUDE.md` project-table row for `Bodu.IO.Compound`: add the
  transactional edit surface (`OpenStream(name, mode, access)` /
  `Commit` / `Revert`) to the responsibility text, which today mentions
  only reading and builder authoring.

**Commit sequence.** One commit ("Roadmap: record delivered
Bodu.IO.Compound writable cursors; link implementation plan"), same
commit carries this plan document if it has not landed separately.

**Effort & risk.** Trivial; none.

## 4. T1 — Property-set write-back and vector emit

### T1.a Vector emit in `PropertySetWriter`

**Scope.** Emit `VT_VECTOR`-flagged values for exactly the element
types the reader parses (`PropertySetReader.cs:204–227`), so that
`OlePropertySet` round-trips: *read → `ToArray()` → read* must be
value-identical for every fixture the reader accepts. Unknown /
unparsed types keep throwing `CompoundFileSerializationException`
(`PropertySetWriter.cs:303`) — symmetry with the reader is the
boundary, not open-ended VT coverage.

**API sketch.** No public API change — `OlePropertySet.ToArray()` /
`WriteTo(…)` simply stop throwing for `IsVector` values. The
`Unsupported(type)` throw at `PropertySetWriter.cs:137` becomes the
vector-emit dispatch; element counts and padding follow MS-OLEPS §2.14.

**Placement.** `src/IO.Compound.PropertySets/PropertySetWriter.cs`
(internal; no new files unless the vector writers justify a
`PropertySetWriter.Vectors.cs` partial).

**Tests.** Extend `PropertySetWriterTests` with a per-element-type
backbone (`Write_WhenVectorOfInt32_…`, etc.); a Regression round-trip
sweep over every reference fixture that contains vector properties
(read → emit → read, assert value equality); negative rows stay for
types the reader itself rejects, as `InvalidKat<…>` rows.

**Docs.** `docs/guides/io-compound/property-sets.md` — drop/replace any
"vector values cannot be written" caveat.

**Effort & risk.** Small-medium. Risk concentrates in MS-OLEPS padding
rules (4-byte alignment per element for some VTs); the round-trip sweep
over real fixtures is the guard.

### T1.b Write-back conveniences

**Scope.** Symmetric write counterparts to the existing read
conveniences, valid on a writable file (throw
`InvalidOperationException` via the existing `RequireWritable` pattern
otherwise):

**API sketch.**

```csharp
// CompoundStorage — the general surface (any storage, any set name):
public void WritePropertySet(string name, OlePropertySet propertySet);

// CompoundFile — the two well-known sets at the root, mirroring
// TryGetSummaryInformation / TryGetDocumentSummaryInformation:
public void SetSummaryInformation(SummaryInformation summary);
public void SetDocumentSummaryInformation(DocumentSummaryInformation summary);
```

Each serializes through the T1.a writer and creates-or-replaces the
stream in the staging tree (`FileMode.Create` semantics), marking the
file dirty. Overloads taking the `SummaryInformationBuilder` /
`DocumentSummaryInformationBuilder` are *not* added — builders already
expose `ToArray()`, and the typed setters accept the built value.

**Placement.** `src/IO.Compound/CompoundStorage.cs` (member near
`TryOpenPropertySet`); `src/IO.Compound/CompoundFile.cs` (members near
the `TryGet…` pair). New resx keys only if a new failure mode needs
one (expected: none — reuses `RequireWritable`).

**Tests.** New member partials `CompoundStorageTests.WritePropertySet.cs`,
`CompoundFileTests.SetSummaryInformation.cs` (both Set members share the
file if cohesive): create → write set → commit → reopen → `TryGet…`
returns the values; replace-existing; read-only file throws; a Smoke row
on the `SummaryInformation` happy path. `SummaryInformationWriteTests`'
manual-`AddStream` coverage stays (it validates the builder path).

**Docs.** `property-sets.md` gains the write-back section; the
`samples/IO.Compound` basics sample gains a snippet (compile-guarded).

**Effort & risk.** Small. Depends on T1.a (a set read from disk may
contain vectors; write-back must not throw where read succeeded).

## 5. T2 — Writable-cursor memory model

**Scope.** Remove the avoidable costs in the writable-cursor path
without changing observable semantics:

1. **Eliminate the flush double-copy.** `FlushToNode` currently copies
   the `MemoryStream` buffer into a fresh array via
   `SetContent(ReadOnlySpan<byte>)` (`CompoundStream.cs:504` →
   `CompoundStreamBuilder.cs:212`). Add an internal transfer path that
   wraps the already-right-sized buffer
   (`Content = buffer.AsMemory(0, length)`) instead of copying —
   internal only; the public `SetContent(ReadOnlySpan<byte>)` keeps its
   defensive copy.
2. **Replace `MemoryStream` growth with pooled/chunked backing.** Back
   the write buffer with the established chunked machinery
   (`PooledBufferBuilder` from `Bodu.Core`, or `SegmentedBuffer<byte>`)
   so large payloads avoid doubling reallocation and LOH churn. The
   final flush assembles once. (If the chunked type cannot hand off a
   single contiguous buffer, item 1's transfer path assembles exactly
   one array — still strictly better than today's two.)
3. **Enforce and document the cap.** Growing a writable cursor past
   `int.MaxValue` today fails as an unspecified cast/overflow. Make it
   throw a deliberate `NotSupportedException` with a
   `CompoundResourceStrings` message
   (`Op_NotSupported_CompoundStreamPayloadTooLarge`), and document the
   supported route for larger-than-memory payloads: deferred sources
   (`AddStream(name, Func<Stream>, length)` /
   `AddStreamFromFile`), which already stream through the pooled
   serializer path with `long` lengths. Note in the same docs pass that
   V3 stream sizes are 32-bit per MS-CFB regardless.

Explicitly *not* in scope (per D2): spill-to-disk machinery inside the
cursor, incremental commit, and any change to the update-mode whole-file
snapshot.

**Placement.** `src/IO.Compound/CompoundStream.cs`,
`src/IO.Compound.Builders/CompoundStreamBuilder.cs` (internal transfer
member), resx + Designer for the new message.

**Tests.** Extend `CompoundStreamTests` / `CompoundStreamAccessTests`
write coverage: flush produces identical committed bytes (golden
fixtures unchanged — this is the tranche's key invariant); a Regression
row writing a multi-hundred-MB payload through the cursor and asserting
a single-buffer flush via the committed result; the over-cap throw as
an `InvalidKat` row (seek `SetLength` past `int.MaxValue` — cheap, no
allocation needed to trigger the guard).

**Docs.** `streaming-and-buffering.md` gains a "large payloads" section
routing readers to deferred sources; XML remarks on the writable ctor
path updated.

**Effort & risk.** Medium. Behaviour-neutral by construction; the risk
is accidental golden-byte drift, which the writer goldens catch.

## 6. T3 — Entry metadata on the edit surface

**Scope.** Let live edits set what the authoring builders already can:
CLSID, creation/modified FILETIMEs, and state bits on **storage**
entries (streams are forced to zero per MS-CFB §2.6.1 — see D4).

**API sketch.**

```csharp
// CompoundStorage — settable on a writable file, mirroring
// CompoundEntryBuilder's authoring surface:
public Guid ClassId { get; set; }
public DateTimeOffset? CreationTime { get; set; }
public DateTimeOffset? ModifiedTime { get; set; }
public uint StateBits { get; set; }
```

Getters work on any storage (today's values surface only through
`EnumerateEntries` snapshots); setters require a writable file
(`RequireWritable`) and delegate to the staging node's existing
builder properties, marking the file dirty. The root storage is
included — root CLSID is *the* Office file-type discriminator.

**Placement.** `src/IO.Compound/CompoundStorage.cs`; no new types.

**Tests.** Member partials (`CompoundStorageTests.ClassId.cs`, one file
per property or one cohesive `…EntryMetadata.cs` if the four stay
trivially parallel — prefer per-member); commit → reopen → values
visible via `EnumerateEntries` / `Stat`; read-only file throws;
cross-check against the reference manifest's recorded root CLSIDs.

**Docs.** `authoring-compound-files.md` note that edit mode now covers
metadata; XML docs.

**Effort & risk.** Small. One design check during review: getters on a
*read-only* storage must read from the directory entry, not the staging
node (the two backing shapes already coexist in `CompoundStorage`).

## 7. T4 — True-async commit and streaming reads

**Scope (per D5).** Two paths only:

1. **`CompoundFile.CommitAsync(CancellationToken)`** — an async
   serialize: `CompoundContainerLayout.WriteToAsync` writing to the
   destination with `WriteAsync`, and the deferred-source copy loop
   (`CopyContent`, `CompoundContainerLayout.cs:519–551`) using
   `ReadAsync`. `Flush()`'s alias relationship gets an async sibling
   (`FlushAsync` on `CompoundFile` if consistent with the sync pair).
2. **True-async streaming reads** — `CompoundStream.ReadAsync` in
   streaming mode performs the sector seek + read against the source
   with real async I/O instead of delegating to sync `Read`. Buffered
   and writable cursors keep their current wrapper behaviour
   (documented as such).

Out of scope: async open paths (`OpenAsync`) — open-time work in
buffered mode is one contiguous read the caller can already do
themselves and pass a `MemoryStream`; revisit only on demand.

**Placement.** `src/IO.Compound/CompoundFile.cs`,
`src/IO.Compound/CompoundStream.cs`,
`src/IO.Compound.Internal/CompoundContainerLayout.cs` (+
`CfbSectorReader` async read member). Watch the analyzer set —
VisualStudio.Threading.Analyzers and AsyncFixer will police the
duplicated sync/async pairs; keep the pairs structurally parallel.

**Tests.** Async siblings of the existing commit/read backbones
(`CompoundFileTests.CommitAsync.cs`, streaming rows in
`CompoundFileTests.Streaming.cs`); cancellation observed before
first write and mid-copy (`FaultingStream` / throttled mocks from
`Bodu.Test.IO`); byte-identical output vs sync `Commit` over the golden
fixtures.

**Docs.** `streaming-and-buffering.md` async section; XML remarks on
the wrapper members stating which paths are truly async.

**Effort & risk.** Medium. The known trap is code drift between the
sync and async serialize paths — mitigate by extracting the shared
layout computation (already separate from I/O in
`CompoundContainerLayout`) so only the byte-moving loops fork.

## 8. T5 — Substrate-readiness review for `Bodu.Formats.Outlook.Msg`

**Scope.** Analysis, expected to produce **no code** in this package.
Before the `.msg` reader (a *New library candidates* item) starts, walk
MS-OXMSG against the current surface and confirm the container needs
nothing new:

- named-property mapping streams (`__nameid_version1.0` storage) — plain
  stream reads; ✓ expected;
- recipient/attachment storages (`__recip_version1.0_#NNNNNNNN`) —
  child-storage enumeration + name conventions; ✓ expected;
- property streams (`__properties_version1.0`) — `.msg`'s own binary
  layout, **not** MS-OLEPS; parsing belongs in the format package, not
  here;
- nested attached messages — storage recursion; ✓ expected;
- the root-CLSID discrimination story (T3 provides the write side;
  reads already surface it).

**Deliverable.** A short design note in the `Bodu.Formats.Outlook.Msg`
kickoff (that package's docs, not this one) recording the audit result.
Any gap it *does* find (e.g. a missing enumeration convenience) comes
back to this plan as a scoped additive item rather than being invented
speculatively now.

**Effort & risk.** Small; pure reading. Gates the `.msg` project, not
this package's release.

---

## 9. Sequencing summary

| Tranche | Item | Depends on | Notes |
| --- | --- | --- | --- |
| **T0** | Roadmap/docs truth-up | — | Free; do first so the roadmap stops misleading. |
| **T1** | Vector emit + property-set write-back | — (T1.b needs T1.a) | Highest leverage; unblocks true round-trips. |
| **T2** | Writable-cursor memory model | — | Behaviour-neutral; golden fixtures are the invariant. |
| **T3** | Entry metadata on edit surface | — | Small; root CLSID matters to Office consumers. |
| **T4** | Async commit + streaming reads | — | Largest; keep sync/async pairs parallel. |
| **T5** | `.msg` substrate review | T3 helpful, not required | Analysis only; gates `Bodu.Formats.Outlook.Msg`. |

T1–T4 are mutually independent and reorderable; the listed order is
leverage. All are additive, so none gates the Wave 2 first publish —
they can land before or after tagging without a major-version event.

## 10. Follow-ups and explicitly out of scope

- **In-place / incremental / transacted commit** (COM
  `IStorage::Commit` model) — out of scope per D2 and the README;
  revisit only with a consumer that measurably cannot rewrite whole
  containers.
- **Encrypted containers** (RC4 / agile encryption of Office documents)
  and **damaged-file recovery** beyond `CompoundValidationLevel.Minimal`
  — remain out of scope per the README; recovery heuristics belong in a
  forensic tool, not a framework library.
- **COM interop shims** (`IStream` / `ILockBytes` adapters) — the
  README's "managed counterpart" framing is deliberate; no COM surface.
- **Async open paths** — deferred per T4's scope note.
- **`Bodu.Formats.Outlook.Msg` / `.doc` readers and
  `Bodu.IO.FileSignatures`** — different projects' roadmaps; T5 only
  gates the first of them.
- **`ROADMAP.md` upkeep** — as tranches land, retire the corresponding
  bullets in the `Bodu.IO.Compound` section (directional edits, per
  that file's contribution note).
