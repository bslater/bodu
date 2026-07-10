# WS-7 — Numerics, Calendar Engine & Financial Core

**Scope:** `Bodu.Numerics/src/` + `Bodu.Numerics.Serialization.Json/src/`; `Bodu.Globalization.Calendar/src/`, `.Builder/src/`, `.DependencyInjection/src/`, and the five `.Data.<Region>/src/` bundles; `Bodu.Financial/src/` + `.DependencyInjection/src/`; a lighter structural pass over `Bodu.Core/src/` and `Bodu.Collections/src/`.

**Overall assessment: unusually high quality.** Exact `BigInteger`-backed rational arithmetic, carefully reasoned interval set-algebra, hardened XML loading, exact-reconciliation money allocation, and an immutable/thread-safe calendar engine. No Critical or High confirmed defect.

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `Bodu.Financial.ExchangeRates/ExchangeRate.cs:74` | Correctness | Medium | ~~PLAUSIBLE~~ **REJECTED on implementation — intended design** | The public ctor's `isInverted:true` path sets `observedRate = 1m/rate`, and `Convert` then does `amount / (1/rate)`. This *looked* like a lossy double reciprocal, but it is a deliberate design: `observedRate` recovers the **native reverse-pair rate**, and `Convert` divides by it. That invariant is depended upon — `ToBook` reads `ObservedRate` to recover the native quote direction, and serialization persists it so the precise divisor round-trips. The apparent loss only occurs for the artificial case of an *exact* forward multiplier flagged inverted; in the real FX case (inverted rate = rounded reciprocal of a native observation), dividing by the recovered native rate is *more* faithful. **No change made** — see `remediation-plan.md` R8. |
| 2 | `Bodu.Financial.Currencies/CurrencyRegistry.cs:95,98` | Convention/Usability | Low | CONFIRMED | Lookups use `StringComparer.Ordinal`, so `Get("usd")`/`TryGet` are case-sensitive — only canonical uppercase ISO codes resolve. Internal `ByCulture` is safe (RegionInfo yields uppercase), but a user passing lower/mixed case gets `KeyNotFoundException`. | If case-insensitive lookup is intended, use `StringComparer.OrdinalIgnoreCase`; otherwise document the uppercase-only contract on `Get`/`TryGet`. |
| 3 | `Bodu.Globalization.Calendar/src/Globalization.Calendar/NotableDateService.cs:126` | Performance | Low | CONFIRMED (by design) | Every `Resolve` re-scans the whole resource across `definitions × (yearWindow±1)`; a single-date query iterates all definitions over 3 years and re-sorts. Documented as intentional, but there is no per-(territory,year) memoization for hot repeated queries. | Acceptable as-is; if profiling shows this as hot, add an optional per-(territory,year) candidate cache keyed off the immutable resource. |
| 4 | `Bodu.Financial.ExchangeRates/ExchangeRate.cs:284,296` | Correctness | Low | PLAUSIBLE | `Equals`/`GetHashCode` include the public `Rate` but exclude the internal `ObservedRate`. Two inverted rates with equal rounded `Rate` but different `ObservedRate` compare equal yet `Convert` differently. Documented as provenance, but a subtle equality/behaviour divergence. | Keep, but ensure the XML-doc caveat stays prominent (it currently is). |
| 5 | `Bodu.Globalization.Calendar.Builder/.../NotableDateDocumentBuilder.Xml.cs:108` | Security | Low | CLEARED | The Builder uses bare `XDocument.Parse(xml)` without explicit hardened settings. In .NET Core the default `XmlReaderSettings.DtdProcessing` is `Prohibit`, so DTD/XXE is rejected by default — safe. Noted only for consistency with the main loader, which sets it explicitly. | Optionally mirror the loader's explicit `DtdProcessing.Prohibit`/`XmlResolver=null` for defense-in-depth. |

## Hot-path notes

- **`Fraction<T>`** (`Fraction{T}.Operators.cs`, `.cs`) promotes every operand to `BigInteger`, reduces via `BigInteger.GreatestCommonDivisor`, and narrows with a bounded range check (`TryNarrow`) — **overflow, signed-min-value magnitude, and divide-by-zero are all correctly handled**; `Pow(int.MinValue)` is guarded via `-(long)exponent`. Equality, `GetHashCode`, and `Compare` are defined over canonical components (cross-multiplication for compare) and mutually consistent. Parse routes numeric components through `BigInteger.TryParse(..., provider)`; compact JSON write uses `InvariantCulture`. No defects found.
- **`MoneyMath`** allocation (`AllocateEvenly`, `AllocateByRatios`): minor-unit counts computed in `BigInteger`, residual distributed by sign (even split) and largest-remainder/Hamilton (ratios), zero-ratio slots excluded — **shares reconcile exactly; no lost or created cents**. `Money<T>` construction rounds banker's-by-default via `decimal.Round(ToEven)`.
- **Easter** (Western Anonymous Gregorian; Orthodox Meeus-Julian + `JulianCalendar` conversion) matches the canonical algorithms. **WeekdayMath** nth-weekday and `SeekOrNull` correctly return `null` for non-existent ordinals and year-boundary overflow. **LunarPhaseCalculator** is a faithful Meeus ch.49 series; the forward-only 3-step search is safe because nearest-`k` bounds the true first-on-or-after phase.
- XXE: the main XML loader (`NotableDateDocumentParser.cs:30-33`) sets `DtdProcessing.Prohibit` + `XmlResolver=null` — **hardened**.

## Architecture / alignment notes

- Interval algebra (`Interval{T}.SetOperations.cs`) is rigorous: empty-set semantics, inclusivity tie-breaking (open wins on intersect, closed wins on union), unbounded endpoints, and cut-flip inclusivity on `Difference` are all handled explicitly and match the documented contracts.
- JSON converters (`FractionJsonConverter{T}.cs`) round-trip `BigInteger`-magnitude components losslessly by reading number tokens as raw UTF-8 and writing via `WriteRawValue`, and reject duplicates/missing/zero-denominator — good malformed-input handling.
- `NotableDateService` is immutable and correctly documented thread-safe; two-phase occupied-day resolution with deterministic placement ordering is sound.

## Duplication notes

- `Fraction<T>` reduction/narrowing is intentionally split: throwing (`FromBigInteger`, ctor) vs non-throwing (`TryReduceToCanonical`) share the GCD/sign core but duplicate the narrowing to preserve framework overflow messages — a reasonable, documented trade-off.
- `ExchangeRate` has three construction paths (public ctor, `FromObservedRate`, `FromComponents`) with overlapping validation; each has a distinct precision/rehydration purpose, but they are the root of findings #1 and #4.

## Region-bundle confirmation

All five region data-bundle factories were opened — `AmericasCalendarData`, `AsiaPacificCalendarData`, `EuropeCalendarData`, `AfricaCalendarData`, `MiddleEastCalendarData` — and each exposes the identical uniform shape (`SupportedCountries` property, `LoadResource(string territory)`, `CreateService(string) => new(LoadResource(territory))`). No structural inconsistency found.
