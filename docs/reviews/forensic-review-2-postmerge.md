# Forensic Review 2 — Post-#593 Design, Architecture & Hot-Path Assessment

**Date:** 2026-07-05
**Base:** `master` at commit `86d8ceb` (after PR #593 "Harden parsers and add bounds validation for untrusted input", PR #594 roadmap, and the executed `Bodu.Collections` package split).
**Scope:** The whole solution, reviewed read-only across seven domain clusters. Two goals: (1) confirm the PR #593 remediations are present and *effective*, and (2) a fresh forensic pass for design weaknesses, architectural anomalies, and unoptimised hot paths — with dedicated attention to the newest surface (`Bodu.Threading`, `Bodu.Functional`, `Bodu.Collections.Probabilistic`, and the tree-backed collections) and to the new types introduced by #593.

## 1. Method

Seven independent reviewers swept the tree by cluster: Core/Collections/Numerics; Security.Cryptography; the Text.\* family; Financial/ExchangeRates/Caching; IO.Compound/Excel/Hashing; Globalization.Calendar/Plugins/Data; and the new advanced algorithmic surface. Each verified the #593 fixes in its area and hunted new issues in three buckets — design weakness, architectural anomaly, unoptimised hot path — reporting concrete `file:line` anchors. Severities are the reviewers' own (High / Med / Low), normalised here.

## 2. Executive summary

The remediations from #593 are, with the exceptions listed in §3, **present and effective** — every fix that was spot-checked holds, and the newest algorithmic core (RB-tree interval/navigable structures, DisjointSet, Aho-Corasick, the `Option`/`Result`/`Either` railway types) is correct. The review nonetheless surfaced **four High-severity items** and a substantial set of Med items, clustered around four recurring themes:

- **Untrusted-input hardening was not applied to every sibling path.** #593 fixed the FAT/property-count allocations in the CFB reader but missed the stream-payload sizing path; and the calendar's untrusted-XML entry point has no size/depth caps at all.
- **"Fix landed on one twin only" divergence.** The constant-time GF(2¹²⁸) multiply hardening reached GCM but not GCM-SIV (a timing side-channel), and cancellation-race handling reached `AsyncLock` but not its sibling primitives.
- **Per-operation re-materialisation on warm paths.** The FX provider rebuilds its entire book on every load, and the calendar re-resolves the whole resource per day / per query — both quadratic on realistic workloads.
- **Duplication that is now genuinely, safely extractable** (distinct from the duplication that is *not* — see §5): CMAC/GF-double/XOR crypto primitives, the file-feed FX provider skeleton, the JSON-feed parser scaffolding, and the async waiter-queue.

## 3. Effectiveness of the #593 remediations

### 3.1 Confirmed present and sound

| Area | Fix | Verdict |
|---|---|---|
| IO.Compound | `CfbDirectory.CollectChildren` iterative stack + `visited`; `BuildFat` DIFAT dedup + `maxFatSectors` cap + checked arithmetic; `PropertySetReader` count bounds + `MaxVariantDepth=32`; `CompoundStream.Read` chain-index bound | Sound |
| Formats.Excel | `Biff8SharedStringTable`/`Biff8StringReader` empty-CONTINUE guard; `Biff8RecordCursor.TryRead` length bounds | Sound |
| Crypto | CCM `ValidatePlaintextLength` (no-alloc 2²⁴ guard); GCM-SIV POLYVAL final partial-block zero-pad; Blake2b/2s `HashSize` re-encodes the RFC 7693 parameter block; Tiger little-endian word loads; Argon2/Scrypt `Validate()` ceilings *before* derivation allocates | Sound |
| Numerics | `BigDecimal.Parse` scale bound + ArrayPool scratch above 256 chars (closes the stackalloc-overflow vector) | Sound |
| Text | Base58/Base62 `MaxDecodeInputLength` + reconciled `IsValid`; O(1) duplicate-key handling (INI/DotEnv/Bencode/Toml); YAML alias-expansion budget + `EffectiveMaxDepth` clamp; Toml/Bencode mutable-struct field-loss fix | Sound |
| Financial | Currency ISO-shape gate (`IsValidIsoCodeShape`); applied cash-rounding settlement policy; FX timestamp/interval parse hardening; Sqlite/Distributed out-of-range-decimal row swallow | Sound |
| Calendar | Plugin trust sequencing: hash-the-loaded-bytes-once, load into a **collectible** ALC, trust evaluated before any plugin code runs | Sound |
| Collections | New `CollectionCapacity.Grow` and `SortedRangeSearch` helpers; `IComparableExtensions`→`ComparableExtensions` rename | Sound |

### 3.2 Gaps and incomplete remediations (highest value in this report)

| Sev | Finding | Anchor |
|---|---|---|
| **High** | **Missed sibling of the #593 CFB count-bound work.** `CfbSectorReader.ReadChain` / `InitializeMiniStream` allocate the *declared* stream / mini-FAT size (`new byte[size]`) before the sector chain is confirmed to supply it. At `CompoundValidationLevel.Minimal` the short-chain guard short-circuits, so a crafted `Root.Size` / `MiniFatSectorCount` drives a ~2 GB allocation (or a `> int.MaxValue` `OverflowException` that escapes the `CompoundFileFormatException`-only contract). Same class as the fixed FAT/property paths; the stream-payload path was not covered. | `CfbSectorReader.cs:99-114, 208-212` |
| **Med** | **Batch 4 "parse *and* pow bounds" only half-done.** `BigDecimal.Parse` is bounded, but `BigDecimal.Pow` still computes `value._scale * exponent` as an unchecked `int * int`. A power-of-ten base (`_mantissa == ±1`) makes `BigInteger.Pow` return instantly, so a large exponent cheaply overflows the scale — either surfacing a misleading `Arg_OutOfRange_BigDecimalScale`, or (when it wraps to a small positive scale) escaping the guard and returning a wrong result. | `BigDecimal.Arithmetic.cs:136` |
| **Med** | **Phase 6 resx migration missed one throw.** `Utf8TomlReader.TryGet.cs:354` (`TryGetGuid`) is the only wrong-token accessor still throwing a message-less `InvalidOperationException`; every sibling now uses `Op_Invalid_TomlReaderValueType`. | `Utf8TomlReader.TryGet.cs:354` |
| **Med** | **`PropertySetReader` `checked((int)…)` casts throw `OverflowException`**, escaping the class's documented `CompoundFileFormatException`-only contract and defeating the per-property "skip and continue" resilience (the section-level reads aren't inside any try). Sibling: `ParseDictionary`'s `entryCount` is not pre-bounded against remaining bytes like the #593-fixed counts (safe today only because nothing is pre-sized from it). | `PropertySetReader.cs:85,109,362,409-436` |
| **Med** | **DotEnvReader is bounded but still O(n²) *under* the cap.** #593 added `MaxPendingLength` (correct DoS ceiling), but the `string _pending` is reallocated per refill and per entry (`_pending += …`, `_pending[consumed..]`), and `TryReadOneEntry` re-parses from index 0 each refill. Worst case ≈ O(cap²/bufferSize) ≈ 256 M char-ops before rejection — bounded/sub-second, but the true linear fix (resumable scan offset + a head/tail char buffer) remains open, as the #593 commit itself noted. | `DotEnvReader.cs:164-188` |
| **Med** | **Plugin `LoadFrom(Assembly)` re-read TOCTOU is real but undocumented.** The path overload is fully fixed; the `Assembly` overload still hashes a re-read of `assembly.Location`, so the hashed and loaded bytes can differ. Inherent for an already-loaded assembly, but the XML doc doesn't warn callers to prefer the path overload for security-sensitive loads. | `NotableDatePluginLoader.cs:80-94` |
| **Low** | **A *successfully* loaded plugin's collectible ALC is never handed back**, so the collectibility benefit is realised only on the failure path — a trusted plugin a host later wants to drop is pinned for process life. | `NotableDatePluginLoader.cs:132-142` |
| **Low** | **Regression-guard gaps for two #593/earlier fixes.** The GCM-SIV partial-block zero-pad fix has no non-block-aligned KAT (only RFC 8452 C.1 is wired up); Snefru's endianness (looks correct — consistent big-endian) has no KAT. Recommend adding RFC 8452 C.2 and a Snefru vector. | `GcmSivModeTransformTests.KnownAnswerTests.cs:18-22` |

## 4. New findings (fresh forensic pass)

### 4.1 High severity

| # | Finding | Anchor |
|---|---|---|
| H-1 | **GCM-SIV's GF(2¹²⁸) multiply is not constant-time, while GCM's twin is.** GCM's `GhashMultiply` was hardened to branchless masks; GCM-SIV's still uses data-dependent branches (`if (((xi >> bit) & 1) == 1) Xor(...)`, `if (lsb) v[0] ^= 0xE1`). Both operands are secret (reflected POLYVAL state and reflected `K_auth`), so this leaks key/state-dependent timing that the GCM path was specifically written to avoid — a classic copy-paste divergence where hardening landed on one twin only. GCM-SIV's version also omits the `finally`-clear that GCM performs. | `GcmSivModeTransform.cs:356-386` vs `GcmModeTransform.cs:490-528` |
| H-2 | **CFB stream-size allocation DoS at `Minimal` validation** (also §3.2; counted once). | `CfbSectorReader.cs:99-114` |
| H-3 | **`WebExchangeRateProvider.RebuildSnapshot` re-materialises the entire book on every load.** It calls `_builder.ToBook()` (full `Array.Copy` of every series), builds a `FrozenDictionary`, then constructs a `FixedDatedExchangeRateProvider` re-walking the book — on *each* load. RBA `PreloadAsync` / BoE range loads run sequentially, each triggering a full rebuild, so warming N units is O(N × total observations) with N FrozenDictionary builds. | `WebExchangeRateProvider.cs:418-422`; `RbaExchangeRateProvider.cs:186-190,325-332` |
| H-4 | **Working-day sweeps re-resolve the whole resource per day.** `EnumerateWorkingDays`/`AddWorkingDays` call `service.Resolve(single-day, territory)` once per calendar day; each `Resolve` re-scans every definition × rule across a 3-civil-year window and allocates fresh `List`/`HashSet`/`Dictionary`. A multi-decade sweep is O(days × definitions × 3 × rules) with a per-day allocation storm. | `NotableDateOnlyExtensions.EnumerateWorkingDays.cs:45`, `.AddWorkingDays.cs:48-52`; `NotableDateService.cs:330-371` |

### 4.2 Medium severity (grouped)

**Correctness / robustness**
- **EAX `ComputeCmac` leaks the partial MAC on a mid-MAC cipher fault** — SIV's twin has a `catch { Clear(mac); throw; }`, EAX has none. (`EaxModeTransform.cs:340-418` vs `SivModeTransform.cs:375-458`)
- **`IntervalTree` overlap-query enumerator only checks the structural version *after* yielding a match**, not on each stack pop — a mutation during a walk over non-matching nodes returns wrong/duplicate results or NREs instead of failing fast, diverging from its own design note and its full `Enumerator`. (`IntervalTree{T}.Queries.cs:154,183`)
- **Calendar untrusted-XML entry point has no caps.** `NotableDateDocumentParser` uses `XDocument.Parse(xml, LoadOptions.None)` — XXE is blocked only by the *implicit* framework default; there is no document-size, nesting-depth, or `MaxCharactersFromEntities` ceiling, so a large/deeply-nested payload is a memory/stack DoS on a documented untrusted entry point. (`NotableDateDocumentParser.cs:38`)
- **Async cancellation-race semantics diverge across the "success wins" family** — `AsyncLock` honours a token that cancels concurrently with the grant; `AsyncSemaphore`/`AsyncReaderWriterLock`/`AsyncAutoResetEvent` return success on the same race, despite identical docstrings. Not a leak, but an observable contract divergence. (`AsyncLock.cs:212-216` vs `AsyncSemaphore.cs:287`, `AsyncReaderWriterLock.cs:346,369`, `AsyncAutoResetEvent.cs:182`)

**Hot paths**
- **Quadratic set operations on pre-sorted data.** `IntervalSet.Union(interval)` rebuilds the list and full-`Sort`s (O(n² log n) to accumulate via repeated `.Union`); `RangeSet.Union`/`Except` insert element-by-element with O(n) shifts (O(n·m)) while sibling `Intersect` already uses an O(n+m) two-pointer sweep. (`IntervalSet{T}.SetOperations.cs:32`; `RangeSet{T}.cs:365,419` vs `:387`)
- **GCM-SIV reflects the constant hash key on every block** (`ReflectBytesAndBits(h,…)` in `PolyvalMultiply`) — should be computed once in the ctor. (`GcmSivModeTransform.cs:395-407`)
- **CCM allocates a fresh counter/scratch array *inside* the per-block loop** — the outlier vs EAX/SIV/GCM-SIV, which hoist a single `stackalloc`. (`CcmModeTransform.cs:357,421`)
- **Fletcher runs one byte at a time with a modulo per byte**, while its Adler sibling batches to NMAX=5552 with a SIMD path. (`Fletcher{T}.cs:127-131` vs `Adler{T}.cs:86-162`)
- **YAML deserialisation is O(entries × members) with a per-comparison `WireName` allocation** and re-runs `EnsureUniqueWireNames` per object instance, vs Bencode/Toml's O(1) precomputed-wire-name lookup. (`YamlSerializer.Read.Collections.cs:132-145`; `YamlMemberInfo.cs:57,67-78`)
- **NotableDateService memoises nothing** — every query re-scans + re-sorts the immutable resource; **diamond imports are re-parsed/re-resolved** and the **XSD is recompiled on every `Parse`**. (`NotableDateService.cs:126-153`; `NotableDateResourceLoader.cs:311-362`; `NotableDateDocumentParser.cs:59-67`)
- **Memoizer's 2-arg overload allocates a closure+delegate on every call (including cache hits)** and offers no comparer overload (the 1-arg pair does both). (`Memoizer.cs:102-110`)
- **Probabilistic sketches do a 64-bit modulo by a non-power-of-two on every probe** of every add/query. (`BloomFilter{T}.cs:283,334`; `CountMinSketch{T}.cs:312,352`)

**Architecture / consistency**
- **File-feed FX providers (BoE/ECB/RBA) triplicate the 5-ctor funnel + `Load{Range,Feed,Era}CoreAsync` skeleton + coverage bookkeeping** (~250 duplicated lines). Safely extractable into a `FileFeedWebExchangeRateProvider` mirroring `PairWebExchangeRateProvider`, with coverage behind `IsUnitLoaded`/`MarkLoaded` hooks. **BoE additionally tracks coverage as an unbounded `List<(from,to)>` with single-range containment** (misses windows spanning two loaded ranges; never merges) instead of the purpose-built `DateRangeCoverage`. (`BoeExchangeRateProvider.cs:72,362-371`; the three `Load*CoreAsync`)
- **The four JSON-feed parsers duplicate `ParseDocument`/`NoData`/`TryReadUnix*`/`TryReadRate` and the Unix-range clamp constants** — extract a stateless `JsonFeedParsing` helper. (Yahoo/Oanda/Ofx/Xe parsers)
- **The async waiter-queue primitives duplicate an identical `LinkedList<TCS>` + `CancelWaiter` + FIFO-grant skeleton** with no shared base (byte-identical `CancelWaiter` in three files) — this is where the cancellation-race drift crept in. (`AsyncLock.cs:227-236`, `AsyncSemaphore.cs:308-317`, `AsyncAutoResetEvent.cs:192-201`)
- **YAML's serializer diverges from the Bencode/Toml converter/metadata architecture** (inline reflection vs `ObjectConverter<T>` + `TypeMetadata`), so cross-cutting serializer fixes must be written twice and consumers meet different capabilities across the three "STJ-shaped" libs. Do **not** merge them (they are intentionally independent) — align the seam or document the gap. (`YamlSerializer.Read.Collections.cs:118-155`)
- **`Deque.Grow` still hand-rolls its doubling** instead of using the new `CollectionCapacity.Grow`, and its clamp/floor ordering differs (MaxLength-wins vs minimum-wins) — reconcile the ordering, then migrate. (`Deque{T}.cs:629-638`)
- **The `Bodu.Collections` split left `ShuffleHelpers`/`SequenceUtility` in `Bodu.Core` under `namespace Bodu.Collections.Generic[.Internal]`**, so that namespace now straddles two assemblies (documented as a forced deviation; cleaner to relocate the two helpers to a Core-owned namespace). (`ShuffleHelpers.cs:7`)
- **AsiaPacific has no region-common resolver hook** while every other region does — benign today (no `asiapacific-common.xml` exists) but a latent trap: adding one would silently fail to resolve. Mirror the seam or document the intentional absence. (`AsiaPacificCalendarData.cs:72`)

### 4.3 Low severity (abridged)

Crypto: six identical `Xor` span loops and the RFC 4493 CMAC subkey / `GfDouble` (`0x87`) are genuinely mergeable; GCM-SIV tag-derivation comments misdescribe which bits are cleared (code is correct); no windowed-table GHASH (throughput follow-up). CFB: unchecked `int` size arithmetic in the mini path (vs `checked` in `BuildFat`); `Biff8SharedStringTable` skip-length can overflow `int` to a silent no-op (misparse rather than reject). Excel: char-by-char compressed-string decode; `ExcelWorksheetReader.TryStep` duplicates `Biff8RecordCursor.TryRead`. Threading: `AsyncDebouncer.DrainAsync` builds a LINQ array under the gate; `AsyncManualResetEvent` allocates a fresh `Task`+registration per contended wait (vs the queue family's reuse); two different waiter mechanisms coexist in one suite. Probabilistic: `UnionWith` vs `MergeWith` naming inconsistency and a duplicated `IsCompatibleWith`; all sketches derive entropy from a 32-bit comparer hash, capping accuracy below advertised precision for large configs (disclosed; optional 64-bit hook). Numerics: `LunarPhaseCalculator` Meeus series recomputed per query (cacheable). Core: stale `InternalsVisibleTo` audit comment in `Bodu.Core.csproj` (ConcurrentHashSet moved to `Bodu.Collections`). Financial: `PairWebExchangeRateProvider` interpolates the single-flight key string on every uncovered call. DotEnv: `MaxPendingLength` cap applies to the streaming reader but not to `DotEnv.Parse` (documented-equivalent APIs with different acceptance). Text: the Bencode/YAML `ref struct` readers alias heap frame state and a `readonly` method mutates it, so the "stack-only / allocation-free" doc claim is not actually met.

## 5. Cross-cutting themes and what NOT to do

Four architectural themes recur, and one important **anti-recommendation** carries over from the first review and is reconfirmed here:

1. **Apply hardening to every sibling, not just the reported one.** The CFB stream-size path and the calendar XML entry point are the two untrusted-input paths that the first hardening wave did not reach. A short "sibling audit" (every `new byte[n]`/`new T[n]` where `n` derives from an untrusted field; every untrusted-document parse) would close the class.
2. **De-duplicate the *genuinely identical* primitives** — and only those. Safe extractions: the CMAC/`GfDouble`/`Xor` crypto helpers (keeping SIV's zeroization and the constant-time GF core), the `FileFeedWebExchangeRateProvider` base + `JsonFeedParsing` helper, and a shared async `WaiterQueue`. Each removes real drift risk (H-1 and the EAX-zeroization bug are exactly such drift).
3. **Do NOT merge the things that only look identical.** Reconfirmed: GCM's GHASH and GCM-SIV's POLYVAL use different field representations (POLYVAL reflects) and different counters (GCM `inc32` vs full-block CTR) — the fix for H-1 is to share the *constant-time core multiply on already-reflected inputs*, preserving POLYVAL's reflection wrapper, **not** to merge the two MAC paths. Likewise the three `System.Text.Json`-shaped text libraries are intentionally independent (per `CLAUDE.md`) — align seams, don't merge.
4. **Warm-path re-materialisation is the dominant perf cost.** Both High perf items (FX `RebuildSnapshot`, calendar per-day resolution) are the same shape: an expensive immutable projection rebuilt on every call in a loop. Batch-and-rebuild-once (FX) and memoise-per-(territory,year) (calendar) are the direct fixes.

## 6. Suggested remediation sequencing

Each batch is independently shippable, test-first, one commit.

- **Batch A (security, High):** H-1 constant-time GF core shared by GCM/GCM-SIV (+ SIV-style zeroization); EAX CMAC exception-path zeroization. Add RFC 8452 C.2 KAT.
- **Batch B (untrusted-input, High/Med):** CFB stream/mini-FAT size bound + `Minimal`-level clamp (H-2); `PropertySetReader` overflow→`CompoundFileFormatException` + `entryCount` bound; calendar XML `XmlReaderSettings` + size/depth ceiling.
- **Batch C (correctness gaps, Med):** `BigDecimal.Pow` scale-overflow guard; `IntervalTree` per-pop version check; `TryGetGuid` resx message; plugin `LoadFrom(Assembly)` `<remarks>` + optional collectible-handle return.
- **Batch D (warm-path perf, High):** FX batch-load-then-rebuild-once (H-3); calendar per-(territory,year) memo + span-once working-day sweep (H-4); cache the compiled XSD; memoise diamond imports.
- **Batch E (consolidation, Med — safe extractions only):** `FileFeedWebExchangeRateProvider` + `DateRangeCoverage` for BoE; `JsonFeedParsing`; async `WaiterQueue` base (fixes the cancellation-race drift); crypto `Cmac`/`GfDouble`/`Xor`; `Deque`→`CollectionCapacity.Grow`.
- **Batch F (targeted perf, Med/Low):** `RangeSet.Union/Except` and `IntervalSet.Union` two-pointer merges; GCM-SIV reflect-key-once; CCM hoist per-block allocation; Fletcher NMAX batching; YAML wire-name map cache; probabilistic power-of-two/Lemire reduction; Memoizer 2-arg closure + comparer overload.
- **Batch G (consistency/docs, Low):** relocate `ShuffleHelpers`/`SequenceUtility`; AsiaPacific hub seam or doc; sketch merge-verb unification; ref-struct reader doc claim; stale `InternalsVisibleTo` comment; DotEnv `Parse`/reader cap parity note.

## 7. Verdict

The #593 remediation wave was **effective**: no fix was found to be wrong, and the security-critical ones (plugin trust, AEAD length guards, password-hash memory ceilings, parser bounds) hold. The residual risk is concentrated in a small number of **un-covered sibling paths** (CFB stream sizing, calendar XML, `BigDecimal.Pow`) and **one genuine new security finding** (H-1, the GCM-SIV timing side-channel) that the first review did not reach. The newest surface is high quality. The largest *non-security* opportunities are the two warm-path re-materialisation costs (H-3, H-4) and the now-safe consolidations that would retire the drift this review found.
