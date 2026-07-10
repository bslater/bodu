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
| R8 | `ExchangeRate(isInverted:true)` precision loss (Medium) | `07-...financial.md` #1 · `ExchangeRate.cs:74` | S | 1 file | Public ctor should store `observedRate = rate` (treat `rate` as the forward multiplier) regardless of `isInverted`, reserving the divide path for the internal reverse-pair rate — or document that public callers must not pass `isInverted:true`. Add a round-trip precision test. |
| R9 | Async cancel-race policy inconsistency (Medium) | `03-concurrency.md` #2 · `AsyncLock` vs `AsyncSemaphore`/`AsyncReaderWriterLock` | S | 3 files | Choose one grant-vs-cancel policy across the family and document it (either drop `AsyncLock`'s post-grant re-check or add it to the other two). |
| R10 | Vyukov non-power-of-two slot caveat (Low, theoretical) | `03-concurrency.md` #3 · `ConcurrentCircularBuffer{T}.cs:848` | S | 1 file | Require power-of-two capacity (round up + mask on construction) or document the 2³²-operation caveat. |
| R11 | Ed25519 duplicated small-order check (Low) | `01-cryptography.md` #1 · `Ed25519.cs:373-376` | XS | 1 file | Delete the second verbatim copy of the rejection block. |
| R12 | Currency lookup case-sensitivity (Low) | `07-...financial.md` #2 · `CurrencyRegistry.cs:95,98` | XS | 1 file | Switch to `OrdinalIgnoreCase` if case-insensitive lookup is intended, else document the uppercase-only contract. |
| R13 | BoE/ECB URL-building brittleness (Low) | `04-network-filesystem.md` #4 · `EcbEndpointOptions.cs:65`, `BoeEndpointOptions.cs:79` | S | 2 providers | Escape BoE series codes individually and join with a literal `,`; validate ECB feed filenames are relative and dotless (overlaps R5). |

## Tier 3 — Convention & documentation

| # | Finding | Where | Effort | Fix summary |
|---|---|---|---|---|
| R14 | Yaml hard-coded exception messages (High, convention) | `06-...duplication.md` #3 | S | Migrate the 5 literals to `YamlResourceStrings` keys (`Arg_*`/`Op_*`). |
| R15 | Text serializers lack a domain `ThrowHelper` (Medium) | `06-...duplication.md` #5 | S | Add one shared serialization `ThrowHelper` (fold into R17). |
| R16 | Docs/resx-placement housekeeping (Low) | `06-...duplication.md` #6,#7 | XS | Add `Bodu.Text.Yaml` + `Bodu.Security.Cryptography.Simd.Test` to the CLAUDE.md table; normalize resx placement. Fix the out-of-scope `Bodu.Financial/bench/` compile errors (stale `CurrencyPair`/`RateSeries` namespace) while here. |

## Tier 4 — Strategic refactors (largest value, plan deliberately)

| # | Refactor | Where | Effort | Blast radius | Notes |
|---|---|---|---|---|---|
| R17 | Extract shared `Bodu.Text.Serialization` core | `06-...duplication.md` #1,#4,#5 | L | 3 packages (Bencode, Toml, Yaml) + tests | Eliminates the largest duplication surface. Move the metadata layer, attribute family, naming policies, enums, callback interfaces, converter-factory pattern, `SerializerEngine`, and `WriteStack` to a core; keep `Utf8*Reader/Writer`, scalar converters, `*Limits`, and exception types format-specific. Do Bencode+Toml first (they already share the shape), then migrate Yaml onto the same model (resolves the structural divergence). |
| R18 | Converge FX cache scaffolding | `06-...duplication.md` #2 (+ WS-4 reconciliation) | M | 3 provider packages + DI + tests | Add generic `IByteCache<TKey>`/`NullByteCache<TKey>` to core; delete the three per-provider `I<X>Cache`/`Null<X>Cache`/wrapper trios; keep the thin `GetFileName` overrides. Combine with R5's sanitization. |
| R19 | De-duplicate cross-cutting helpers | `03` dup note, `04` arch note | S–M | scattered | Promote the verbatim `OnStorageFailureSwallowed` routine (Sqlite/Distributed caches) to a shared helper; consider a shared FIFO-waiter-queue helper for the async primitives' `CancelWaiter`/grant loop. |

## Suggested sequencing

1. **R1–R7** (Tier 1) as a first security/reliability PR — small, isolated, each with a regression test.
2. **R8–R13** (Tier 2 correctness) as a second PR.
3. **R14–R16** (convention) — quick, can ride alongside Tier 2.
4. **R17/R18** (strategic) — separate, deliberately-scoped efforts; R18 first (mechanical, lower risk), then R17 (the serializer core) staged Bencode→Toml→Yaml.
