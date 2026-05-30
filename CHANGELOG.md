# Changelog

All notable changes to this repository are documented here. Version numbers refer to NuGet package releases.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this repository follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Bodu.Numerics — 1.0.0

Initial release of a new numeric-primitives library. Ships two header types: `Fraction<T>`, an immutable exact-rational value type generic over any `IBinaryInteger<T>` backing component; and `Interval<T>`, an immutable bounded interval generic over any `INumber<T>` endpoint type.

> **Note.** Money, currency, and foreign-exchange types now ship in the companion **Bodu.Financial** package. The two were originally a single package; the split keeps `Bodu.Numerics` focused on generic numeric primitives without the ~185-currency catalogue, JSON converters, and FX provider stack a consumer of just `Fraction<T>` would otherwise pull in.

### Bodu.Financial — 1.0.0

Initial release of the financial-primitives library, split out of the pre-release `Bodu.Numerics` assembly. References `Bodu.Numerics` for the exact-arithmetic escape hatch (`Money<T>.ToFraction()` / `FromFraction` / `MultiplyExact`).

#### Added — `Fraction<T>`

- `Fraction<T>` is always held in canonical form (strictly positive denominator, sign on the numerator, fully reduced). Arithmetic is exact: intermediate results are evaluated with `BigInteger` precision and narrowed back to `T`, throwing `OverflowException` when a fixed-width component cannot represent the canonical result.
- Arithmetic and comparison operators, named arithmetic methods (`Add`, `Negate`, `Abs`, `Reciprocal`, `Pow`, `Remainder`), and `GreatestCommonDivisor` / `LeastCommonMultiple` helpers.
- Conversions to and from `decimal` and `double` with exact `FromDecimal` / `FromDouble` factories, plus `As<TOther>()` for retyping the backing component.
- Continued-fraction expansion (`ToContinuedFraction`, `FromContinuedFraction`) and bounded best-rational approximation (`LimitDenominator`).
- Parsing of integer, ratio, mixed-number, Unicode vulgar-fraction, and percent forms across `string`, `ReadOnlySpan<char>`, and UTF-8 inputs (`IParsable`, `ISpanParsable`, `IUtf8SpanParsable`); formatting with general, mixed (`M`), Unicode (`U`), and percent (`P`) specifiers (`IFormattable`, `ISpanFormattable`, `IUtf8SpanFormattable`).
- The full generic-math surface — `INumber<Fraction<T>>`, `INumberBase<Fraction<T>>`, `ISignedNumber<Fraction<T>>` — so `Fraction<T>` composes with `INumber<T>`-constrained code.
- XML serialization (`IXmlSerializable`) and `System.Text.Json` support through a converter that round-trips the value as its `numerator/denominator` string form.

#### Added — `Interval<T>`

- Immutable bounded interval `Interval<T>` generic over any `INumber<T>` endpoint type — `int`, `long`, `double`, `decimal`, `BigInteger`, or any consumer-defined numeric type. Endpoint inclusivity is independent on each side: closed-closed `[a, b]`, open-open `(a, b)`, closed-open `[a, b)`, and open-closed `(a, b]`.
- Static factory methods (`Interval<T>.Closed`, `Open`, `ClosedOpen`, `OpenClosed`, `Singleton`, `Empty`) plus a non-generic `Interval` helper class that infers `T` from its arguments (`Interval.Closed(1, 5)`).
- Set algebra: `Contains(T)`, `Contains(Interval<T>)`, `Overlaps`, `Intersect`, and `TryUnion`. `TryUnion` succeeds when the operands are overlapping or adjacent and the result is a single contiguous interval; it returns `false` for disjoint operands rather than producing two intervals.
- `IsEmpty`, `IsDegenerate`, and `Length` (the algebraic length, equal to `Upper - Lower` for non-empty intervals and `T.Zero` for empty ones). All empty intervals compare equal to `Empty` regardless of the bounds used to construct them.
- ISO 31-11 bracket-notation formatting via `ToString()`, `IFormattable`, `ISpanFormattable`, and `IUtf8SpanFormattable`; matching parsing via `IParsable<Interval<T>>` and `ISpanParsable<Interval<T>>`. Empty intervals format and parse as the U+2205 EMPTY SET glyph.

#### Added — `Money<TCurrency>`

- Immutable monetary value `Money<TCurrency>` with the currency encoded as a type parameter via an `ICurrency` tag. Cross-currency arithmetic (`Money<USD> + Money<JPY>`) is a compile error, not a runtime exception; cross-currency conversion is available exclusively through the explicit `Convert<TTarget>(rate, rounding)` method. The amount is stored as a `decimal` rounded on construction to `TCurrency.MinorUnits` using banker's rounding (default), with a constructor overload for any other `MidpointRounding` rule.
- Same-currency arithmetic and comparison operators (`+`, `-`, unary `-`, `<`, `<=`, `>`, `>=`, `==`, `!=`); scalar multiplication and division (`Money<T> * decimal`, `Money<T> / decimal`) that round the result to the currency's minor-unit precision; and dimensionless ratio (`Money<T> / Money<T> → decimal`). `IEquatable<Money<TCurrency>>`, `IComparable<Money<TCurrency>>`, and `IComparable` are implemented; the boxed `Equals(object)` returns `false` for a cross-currency instance and `IComparable.CompareTo(object)` throws `ArgumentException` for one.
- `Allocate(int parts)` and `Allocate(ReadOnlySpan<decimal> ratios)` distribute an amount as integer minor-unit shares that sum exactly to the original. The residual is sign-stable (negative amounts produce negative shares) and zero-ratio slots are honored.
- Exact-arithmetic escape via `ToFraction()` / `FromFraction(Fraction<BigInteger>, MidpointRounding)` / `MultiplyExact(Fraction<BigInteger>, MidpointRounding)` so chained calculations can defer rounding to the final conversion back to `Money`.
- Formatting through `IFormattable`, `ISpanFormattable`, and `IUtf8SpanFormattable`. The default (`"G"` or `"C"`) renders the ISO code followed by the amount at minor-unit precision with culture-aware grouping (e.g. `"USD 1,234.56"`, `"JPY 100"`, `"BHD 12.345"`); `"L"` (locale) renders the culture's native currency symbol in its `CurrencyPositivePattern` slot when the culture's region currency matches `TCurrency` (e.g. `"$1,234.56"` in en-US for USD, `"1.234,56 €"` in de-DE for EUR) and substitutes the ISO code into the same slot when the currencies differ (e.g. `"JPY 1,234"` in en-US for JPY, `"1.234,56 USD"` in de-DE for USD); `"N"` strips the ISO code; `"F"` / `"D"` strip the ISO code and grouping. A `"~"` prefix on `C`/`G`/`L` elides the currency designator when the formatter's culture-region currency matches `TCurrency`, keeping the explicit ISO code otherwise (e.g. `"~C"` renders `Money<USD>` as `"1,234.56"` in en-US but as `"JPY 1,234"` for `Money<JPY>` in the same culture). All specifiers accept an explicit precision suffix (`"C4"`, `"L0"`, `"~C2"`, `"F0"`) that overrides the currency's natural precision.
- Strict parsing via `IParsable<Money<TCurrency>>` and `ISpanParsable<Money<TCurrency>>`. A bare decimal, `"<ISO> <decimal>"`, or `"<decimal> <ISO>"` is accepted; an ISO code that does not match `TCurrency.IsoCode` exactly throws `FormatException`. Currency symbols (`$`, `¥`) are rejected because they are ambiguous across currencies.
- `System.Text.Json` support through `MoneyJsonConverterFactory`, serializing each amount as `{ "amount": <number>, "currency": "<ISO>" }`. Deserialization verifies the `"currency"` field matches `TCurrency.IsoCode` and throws `JsonException` on mismatch.
- Non-generic `Money` static helper with `Of<TCurrency>(decimal)`, `Of<TCurrency>(decimal, MidpointRounding)`, and `Zero<TCurrency>()` so the type parameter need not be written twice at call sites.
- Currency catalogue covering the ~155 active ISO 4217 currencies plus 29 historic / demonetised currencies under `Bodu.Financial.Currencies` (`USD`, `EUR`, `GBP`, `JPY`, `BHD`, `KWD`, the twenty Euro-zone predecessors `DEM`/`FRF`/`ITL`/`ESP`/etc., `VEF`, `ZWL`, …). Tag types are source-generated from `Bodu.Financial/src/Currencies/currencies.json` by `tools/CurrencyCatalogueGenerator`; consumers add custom currencies by implementing `ICurrency` directly and optionally registering them with `CurrencyRegistry`.
- `ICurrency` is extended with `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, and `SuccessorIsoCode` (all `static virtual` with sensible defaults so existing custom implementations stay source-compatible). `Money<TCurrency>.RoundToCash(MidpointRounding)` snaps amounts to the nearest cash denomination — useful for CHF (5 rappen), CAD/AUD cash totals (5¢), NZD cash totals (10¢), and SEK/NOK whole-krona cash totals. `Money<TCurrency>.Multiply(decimal, MidpointRounding)` / `Divide(decimal, MidpointRounding)` give callers explicit rounding-rule control; `RatioTo(Money<TCurrency>)` is the named alternative to the `Money / Money → decimal` operator.
- `MoneyValue` runtime-tagged sister type to `Money<TCurrency>` for the case where the currency is data rather than part of the type — for example, JSON deserialisation, generic invoicing engines, FX rate matrices. Same arithmetic and rounding semantics as `Money<T>`, but cross-currency operations surface `InvalidOperationException` at runtime instead of as compile errors. Bridge in both directions via `ToTyped<TCurrency>()` / `TryToTyped<TCurrency>()` / `FromTyped<TCurrency>(Money<TCurrency>)`.
- `MoneyBag` immutable mixed-currency aggregate for portfolios, multi-currency ledger totals, and FX positions. Per-currency balances are tracked separately, zero balances pruned automatically, enumeration is ISO-code lexicographic, and the bag converts to a single target currency through `IExchangeRateProvider` (with `FixedExchangeRateTable` providing a dictionary-backed implementation that includes inverse-rate fallback).
- `CurrencyRegistry` static catalogue of `CurrencyInfo` records — runtime ISO-to-metadata lookup, custom-currency registration, frozen-dictionary-backed shipped catalogue populated from a source-generated registration list (no runtime reflection scan).

### Bodu.Globalization.Calendar — 1.1.0

#### Added

- `XmlResourceNotableDateRuleProvider` gains a new constructor accepting an ordered `IEnumerable<Assembly>`. The provider walks the chain when resolving manifest resources, so a `<UseFrom>` directive declared in one assembly can cherry-pick rules from another. The legacy single-assembly constructor is preserved as a thin shim and remains source-compatible.
- A new embedded `default-minimal.xml` resource carrying a single rule for New Year's Day, so the parameterless `new NotableDateService()` constructor still produces a usable service when no companion data pack is referenced.
- Resolve-entry metadata API on `NotableDateService` exposes the originating rule, provider, and assembly chain for each resolved occurrence, so consumers can audit *why* a date was produced.
- Parser policy hooks and a redesigned exception hierarchy let consumers customize how malformed rules are reported and recovered from during parse.

#### Changed

- **Behaviour change — please re-test.** The parameterless `new NotableDateService()` constructor no longer loads the embedded global rule set. It now loads only `default-minimal.xml` (currently New Year's Day). National public-holiday data must be supplied by referencing one of the new `Bodu.Globalization.Calendar.Data.*` companion packages and passing its provider(s) to the full constructor. The constructor signature itself is unchanged, so the source-level surface is preserved; only the rule set produced at runtime has shifted.
- The `FileNotFoundException_EmbeddedXmlResourceNotFound` diagnostic message now reads `…not found in any of the searched assemblies: {1}.` to remain grammatical when the provider's assembly chain has more than one entry.
- Calendar exception messages are now centralized in `.resx` resources, so diagnostics localize and stay consistent across providers.

#### Removed

- All 17 `region-*.xml` resources (au, ca, cn, de, es, fr, gb, ie, in, it, jp, kr, my, nl, nz, se, sg, us) are no longer embedded in `Bodu.Globalization.Calendar.dll`. They ship in the new region-specific data packs listed below.

#### Migration

If your code constructs `new NotableDateService()` and queries regional holidays, install the relevant data pack(s) and pass providers from the matching `<Pack>CalendarData` factory to the full constructor:

```csharp
// Before — relied on the main library shipping every region's rules:
var service = new NotableDateService();
var auDates = service.GetNotableDates(2026, "AU");

// After — install Bodu.Globalization.Calendar.Data.AsiaPacific and pass its provider:
using Bodu.Globalization.Calendar.Data.AsiaPacific;

var service = new NotableDateService(
    ruleProviders:     new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);
var auDates = service.GetNotableDates(2026, "AU");
```

Use `<Pack>CalendarData.CreateProviders()` to enumerate every country in a pack at once. See the [Calendar data packs](docs/guides/calendar/data-packs.md) guide for full composition patterns.

### Bodu.Globalization.Calendar.Data.Americas — 1.0.0

Initial release. Ships embedded notable-date rules for the United States (`US`) and Canada (`CA`). Exposes a static `AmericasCalendarData` factory with per-country `CreateUnitedStatesProvider()` / `CreateCanadaProvider()` and bulk `CreateProviders()` helpers that pre-wire the `[pack, main library]` assembly chain.

### Bodu.Globalization.Calendar.Data.Europe — 1.0.0

Initial release. Ships embedded notable-date rules for Germany (`DE`), Spain (`ES`), France (`FR`), the United Kingdom (`GB`), Ireland (`IE`), Italy (`IT`), the Netherlands (`NL`), and Sweden (`SE`). Exposes a static `EuropeCalendarData` factory with per-country and bulk `CreateProviders()` helpers.

### Bodu.Globalization.Calendar.Data.AsiaPacific — 1.0.0

Initial release. Ships embedded notable-date rules for Australia (`AU`), China (`CN`), India (`IN`), Japan (`JP`), South Korea (`KR`), Malaysia (`MY`), New Zealand (`NZ`), and Singapore (`SG`). Exposes a static `AsiaPacificCalendarData` factory with per-country and bulk `CreateProviders()` helpers.

> **Note.** Several rules in the Asia-Pacific pack (lunar, Hindu, Islamic, Buddhist) require corresponding `INotableDateAlgorithm` implementations registered with the algorithm registry. Without those registrations, the affected rules silently produce no occurrences; the remaining Western/Gregorian rules behave unaffected.

#### Added

- New South Wales Anzac Day weekend-substitute trial (2026–2027) — when 25 April falls on a weekend, the substitute observance is emitted for the following Monday for the duration of the trial.

### Bodu.Globalization.Calendar.DependencyInjection — 1.0.0

Initial release. Provides `IServiceCollection` extensions for registering `NotableDateService` and its rule providers through the standard `Microsoft.Extensions.DependencyInjection` container. Pairs naturally with the new `Bodu.Globalization.Calendar.Data.*` packs — register the data pack's `CreateProviders()` output as services and resolve `NotableDateService` from the container.

### Bodu.Security.Cryptography — *(unversioned, next minor)*

#### Added

- BLAKE3 implementation, including AVX-512 vectorised fast path on capable hardware.
- ASCON-AEAD-128 implementation backed by the shared KAT infrastructure.
- Shared KAT (known-answer test) infrastructure now covers the BLAKE3 and ASCON algorithm families end-to-end.
