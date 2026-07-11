# Remediation Plan

Prioritized backlog derived from the workstream findings. Ordered by severity within each tier; effort and blast-radius are rough estimates to aid sequencing. Nothing here has been applied — this session produced findings only.

## Tier 1 — Security & reliability (do first)

| # | Finding | Where | Effort | Blast radius | Fix summary |
|---|---|---|---|---|---|
| R1 | `AsyncDebouncer` CTS dispose race (High) | `03-concurrency.md` #1 · `AsyncDebouncer.cs:278-279,304-305,398` | S | 1 file | Stop `CancellationTokenSource.Cancel()` racing a concurrent `Dispose()`: wrap the `Cancel()` calls in `try/catch (ObjectDisposedException)`, or keep the per-run CTS undisposed, or gate the dispose behind a cancelling flag. Add a concurrency test that cancels while a callback is completing. |
| R2 | CFB sector-chain allocation amplification / DoS (Medium) | `02-parsers.md` #1 · `CfbSectorReader.cs:148-170,255-288` | M | 1 file (2 methods) | Replace the global `guard > _fat.Length` counter with a per-chain visited bitset (each sector at most once) and/or cap accumulated bytes at `_source.Length`. Factor a single shared visited-set chain walker used by both `ReadChainToEnd` and `ReadMiniChain`. Add a malformed-container test (self-loop FAT). Independent of `CompoundValidationLevel`. |
| R3 | Empty `CompositePluginTrustPolicy` fails open (Medium) | `05-plugin-trust.md` #1 · `CompositePluginTrustPolicy.cs:24-45` | S | 1 file | Fail closed: reject an empty/all-null policy set in the ctor, or return `Rejected` when `_policies.Length == 0`. Add a test asserting the empty combinator denies. |
| R4 | `LoadFrom(Assembly)` weaker trust guarantee (Medium) | `05-plugin-trust.md` #2 · `NotableDatePluginLoader.cs:80-94` | S | 1 file + docs | Document that this overload trusts an already-loaded assembly (code may have run) and hashes on-disk bytes, not the loaded image; steer untrusted input to the path overload; optionally tag `PluginTrustContext.FileHash` provenance. |
| R5 | Cache-filename path traversal, config-reachable (Low–Med) | `04-network-filesystem.md` #1 · `FileSystemByteCache{T}.cs`, RBA/ECB `GetFileName` | S | core + 2 providers | Sanitize the `GetFileName` segment in `FileSystemByteCache` (reject `Path.GetInvalidFileNameChars`, `..`, rooted paths) before `Path.Combine`, or validate era labels / feed names in options `Validate()`. Mirror `RateCacheFileLayout.Sanitize`. |
| R6 | Yahoo/OFX unescaped alias→URL substitution (Low) | `04-network-filesystem.md` #2 · `YahooChartRateSource.cs:69`, `OfxRateProviderOptions.cs:134-139` | S | 2 providers | Escape the mapped token, or validate alias values `[A-Za-z0-9]` in `WebRateProviderOptions.TryValidate`. Align with the escaping XE/OANDA/BoE already do (a shared `AppendCurrency` helper closes the divergence). |
| R7 | Missing HTTP response-size caps (Low) | `04-network-filesystem.md` #3 · all provider sources | S | providers + factory | Set `MaxResponseContentBufferSize` on provider-owned clients (document for DI clients), or stream + length-guard. |

## Tier 2 — Correctness

| # | Finding | Where | Effort | Blast radius | Fix summary |
|---|---|---|---|---|---|
| R8 | ~~`ExchangeRate(isInverted:true)` precision loss (Medium)~~ **REJECTED — intended design** | `07-...financial.md` #1 · `ExchangeRate.cs:74` | — | none | Investigated and rejected during implementation. The public ctor's `observedRate = 1m/rate` deliberately recovers the **native reverse-pair rate** for an inverted rate, so `Convert` divides by it. This is a real, depended-upon invariant: `ToBook` reads `ObservedRate` to recover the native quote direction (`ExchangeRateEnumerableExtensionsTests.ToBook.cs` asserts the `0.68` native rate is recovered) and serialization persists it so the precise divisor round-trips (`ExchangeRateJsonConverterPolicyTests.CompactPolicy.cs`). The apparent "precision loss" only arises for the artificial case of an *exact* forward multiplier flagged inverted; in the real FX case (an inverted rate that is itself a rounded reciprocal of a native observation) dividing by the recovered native rate is intentionally *more* faithful than multiplying by the rounded forward value. Storing `observedRate = rate` regressed both dependents. No change made. |
| R9 | Async cancel-race policy inconsistency (Medium) → **resolved (success-wins)** | `03-concurrency.md` #2 · `AsyncLock` | S | 1 file | Per maintainer decision, standardized on BCL `SemaphoreSlim.WaitAsync` "success-wins": removed `AsyncLock`'s post-grant cancellation re-check so all three primitives agree, and documented the shared policy. The specific grant-vs-cancel race is not deterministically testable; all 61 primitive tests stay green. |
| R10 | Vyukov non-power-of-two slot caveat (Low, theoretical) → **resolved by documenting** | `03-concurrency.md` #3 · `ConcurrentCircularBuffer{T}.cs` | S | 1 file | Rounding capacity up to a power of two was rejected: the type deliberately supports and **tests** exact non-power-of-two capacities (`Capacity == 9/10/6/5/3/7`), and `Capacity` is documented as the exact requested value — rounding would break that contract. A full fix without changing capacity would require widening the 32-bit counters to 64-bit (a larger, riskier concurrency change, out of scope). Resolved by documenting the ~4.29-billion-operation caveat in the class remarks and at `SlotIndex`, and recommending a power-of-two capacity for extreme-longevity buffers. |
| R11 | Ed25519 duplicated small-order check (Low) | `01-cryptography.md` #1 · `Ed25519.cs:373-376` | XS | 1 file | Delete the second verbatim copy of the rejection block. |
| R12 | Currency lookup case-sensitivity (Low) → **resolved by documenting** | `07-...financial.md` #2 · `CurrencyRegistry.cs` | XS | 1 file + test | Investigation showed the codebase enforces an **uppercase-only** ISO-code contract elsewhere (`FixedDatedRateProviderTests` asserts a lower-case code throws `ArgumentException` as malformed). Switching the registry to `OrdinalIgnoreCase` would contradict that (registry `Get("usd")` would succeed while `TryGetRate("usd",…)` throws), a worse inconsistency. Resolved by **documenting** the uppercase-only contract on `Get`/`TryGet` and pinning it with a characterization test, keeping resolution uniform. No comparer change. |
| R13 | BoE/ECB URL-building brittleness (Low) | `04-network-filesystem.md` #4 · `EcbEndpointOptions.cs:65`, `BoeEndpointOptions.cs:79` | S | 2 providers | Escape BoE series codes individually and join with a literal `,`; validate ECB feed filenames are relative and dotless (overlaps R5). |

## Tier 3 — Convention & documentation

| # | Finding | Where | Effort | Fix summary |
|---|---|---|---|---|
| R14 | Yaml hard-coded exception messages (High, convention) | `06-...duplication.md` #3 | S | Migrate the 5 literals to `YamlResourceStrings` keys (`Arg_*`/`Op_*`). |
| R15 | Text serializers lack a domain `ThrowHelper` (Medium) | `06-...duplication.md` #5 | S | Add one shared serialization `ThrowHelper` (fold into R17). |
| R16 | Docs/resx-placement housekeeping (Low) | `06-...duplication.md` #6,#7 | XS | Add `Bodu.Text.Yaml` + `Bodu.Security.Cryptography.Simd.Test` to the CLAUDE.md table; normalize resx placement. Fix the out-of-scope `Bodu.Financial/bench/` compile errors (stale `CurrencyPair`/`RateSeries` namespace) while here. |

## Consolidation strategy — `Bodu.Core` is the central substrate

**Decision (accepted):** `Bodu.Core` is the designated shared implementation substrate for the solution. New cross-cutting mechanics are added to `Bodu.Core` and reused; the tree does **not** stand up a separate `Bodu.Internal` package.

Rationale — this is already the de-facto architecture, so the work is to *extend* it, not create it:

- **`ThrowHelper` is the proof of the pattern.** There is one canonical implementation in `Bodu.Core` (11 category partials — `Null`/`Numeric`/`Span`/`Stream`/`Array`/`Collection`/`Comparison`/`Equality`/`String`/`Type`/`Ascii`, each with `CallerExpression`/`NetStandard` variants), and every domain (`Financial`, `IO.Hashing`, `IO.Compound`, `Numerics`, `Security.Cryptography`, `Text.Configuration`, `ExchangeRates`) carries only a **thin domain-specific helper** on top. That "shared mechanics + domain specifics" split is the target model, already realized.
- **Pooled buffers (`PooledBufferBuilder`), buffer/endian converters, ASCII/text helpers, and the `Bodu.Threading` primitives** already live in `Bodu.Core` and are reused. These substrate slots are filled; do not re-extract them.
- **Public-surface note.** `Bodu.Core`'s substrate is a **public** API (e.g. `ThrowHelper.ThrowIf*` is public by convention). Consolidating a new mechanic into `Bodu.Core` therefore grows public contract — so it must go through an API-compatibility gate (public-API baseline / `PublicApiAnalyzers`) and be classified `Stable`/`Preview` accordingly. Mechanics that must stay unexported should be placed behind `internal` types in `Bodu.Core` exposed via `InternalsVisibleTo`, not published.

**What belongs in the substrate vs. what stays in its domain.** Add to `Bodu.Core` only genuinely-common mechanics with identical invariants and failure semantics (guards, checked arithmetic, pooled-buffer ownership, ASCII/hex helpers, bounded stream readers, parser-location primitives). Keep domain behaviour in its own package — money rounding/allocation, calendar rule evaluation, TOML/Bencode syntax semantics, cryptographic state, compound-file sector handling, Excel BIFF interpretation. Two implementations that merely *look* similar (a TOML parser cursor and a CFB sector reader are both "readers") are not merged unless they share the same contract.

**Two consolidations sit *above* the substrate, not in it.** The largest duplication in the tree — the Bencode↔Toml serializer clones (R17) — needs a **mid-level `Bodu.Text.Serialization` core**, and the FX cache scaffolding (R18) is provider infrastructure; neither is low-level `Bodu.Core` material. Prioritize them on their own merits (below); the substrate work is smaller.

**The one real substrate gap** is R20 — a canonical checked-arithmetic helper — which is also the fix vehicle for the CFB overflow/allocation class (R2) and the general `F-003` size/offset discipline.

## Tier 4 — Strategic refactors (largest value, plan deliberately)

| # | Refactor | Where | Effort | Blast radius | Notes |
|---|---|---|---|---|---|
| R17 | Extract shared `Bodu.Text.Serialization` core | `06-...duplication.md` #1,#4,#5 | L | 3 packages (Bencode, Toml, Yaml) + tests | Eliminates the largest duplication surface. Move the metadata layer, attribute family, naming policies, enums, callback interfaces, converter-factory pattern, `SerializerEngine`, and `WriteStack` to a core; keep `Utf8*Reader/Writer`, scalar converters, `*Limits`, and exception types format-specific. Do Bencode+Toml first (they already share the shape), then migrate Yaml onto the same model (resolves the structural divergence). |
| R18 | Converge FX cache scaffolding | `06-...duplication.md` #2 (+ WS-4 reconciliation) | M | 3 provider packages + DI + tests | Add generic `IByteCache<TKey>`/`NullByteCache<TKey>` to core; delete the three per-provider `I<X>Cache`/`Null<X>Cache`/wrapper trios; keep the thin `GetFileName` overrides. Combine with R5's sanitization. |
| R19 | De-duplicate cross-cutting helpers | `03` dup note, `04` arch note | S–M | scattered | Promote the verbatim `OnStorageFailureSwallowed` routine (Sqlite/Distributed caches) to a shared helper; consider a shared FIFO-waiter-queue helper for the async primitives' `CancelWaiter`/grant loop. |
| R20 | Add canonical checked-arithmetic guards to `Bodu.Core` substrate | `06-...duplication.md`; `F-003` | S | `Bodu.Core` + call sites | The one genuine substrate gap. Implemented as void `ThrowHelper.ThrowIfAddOverflows`/`ThrowIfMultiplyOverflows` (`int`+`long`) — matching `ThrowHelper`'s void-throwing surface rather than value-returning `CheckedAdd`/`CheckedMultiply` (which would introduce a new pattern), siblings to the existing `ThrowIfSequenceRangeOverflows`. Available at input-controlled size/offset boundaries for the general overflow discipline. Note: CFB (R2) keeps its own `CompoundFileError`/format-exception model for internal offset checks (widened `long` compare) rather than these `ArgumentOutOfRangeException` guards. Public surface → goes through the API gate. |

## Suggested sequencing

1. **R1–R7** (Tier 1) as a first security/reliability PR — small, isolated, each with a regression test.
2. **R8–R13** (Tier 2 correctness) as a second PR.
3. **R14–R16** (convention) — quick, can ride alongside Tier 2.
4. **R20** (checked-arithmetic substrate helper) early — it lands in `Bodu.Core` and is the fix vehicle for R2, so do it before or with the Tier 1 CFB fix.
5. **R17/R18** (strategic) — separate, deliberately-scoped efforts; R18 first (mechanical, lower risk), then R17 (the serializer core) staged Bencode→Toml→Yaml. Both sit above the `Bodu.Core` substrate, not in it.
