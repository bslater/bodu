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
- `System.Text.Json` support through `MoneyOfTCurrencyJsonConverterFactory`, serializing each amount as `{ "amount": <number>, "currency": "<ISO>" }`. Deserialization verifies the `"currency"` field matches `TCurrency.IsoCode` and throws `JsonException` on mismatch.
- `Money` exposes generic static factories `Of<TCurrency>(decimal)`, `Of<TCurrency>(decimal, MidpointRounding)`, and `Zero<TCurrency>()` so the type parameter need not be written twice at call sites.
- Currency catalogue covering the ~155 active ISO 4217 currencies plus 29 historic / demonetised currencies under `Bodu.Financial.Currencies` (`USD`, `EUR`, `GBP`, `JPY`, `BHD`, `KWD`, the twenty Euro-zone predecessors `DEM`/`FRF`/`ITL`/`ESP`/etc., `VEF`, `ZWL`, …). Tag types are source-generated from `Bodu.Financial/src/Currencies/currencies.json` by `tools/CurrencyCatalogueGenerator`; consumers add custom currencies by implementing `ICurrency` directly and optionally registering them with `CurrencyRegistry`.
- `ICurrency` is extended with `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, and `SuccessorIsoCode` (all `static virtual` with sensible defaults so existing custom implementations stay source-compatible). `Money<TCurrency>.RoundToCash(MidpointRounding)` snaps amounts to the nearest cash denomination — useful for CHF (5 rappen), CAD/AUD cash totals (5¢), NZD cash totals (10¢), and SEK/NOK whole-krona cash totals. `Money<TCurrency>.Multiply(decimal, MidpointRounding)` / `Divide(decimal, MidpointRounding)` give callers explicit rounding-rule control; `RatioTo(Money<TCurrency>)` is the named alternative to the `Money / Money → decimal` operator.
- `Money` runtime-tagged sister type to `Money<TCurrency>` (currency identified at runtime by ISO code) for the case where the currency is data rather than part of the type — for example, JSON deserialisation, generic invoicing engines, FX rate matrices. Same arithmetic and rounding semantics as `Money<T>`, but cross-currency operations surface `InvalidOperationException` at runtime instead of as compile errors. Bridge to the typed form via `Money.As<TCurrency>()` / `Money.TryAs<TCurrency>(out Money<TCurrency>)`, and back via `Money<TCurrency>.ToMoney()` or the implicit `Money<TCurrency>` → `Money` conversion.
- `CalculatedMoney` runtime-tagged, high-precision monetary type whose rounding is deferred until it is converted back to a settlement value through `RoundToMoney(MonetaryContext?)`. It is the runtime deferred-rounding form only — there is no generic `CalculatedMoney<TCurrency>`; obtain one from `Money<TCurrency>.ToCalculated()`, or use the `Money<TCurrency>.ToFraction()` / `MultiplyExact` escape hatch when the calculation must be mathematically exact rather than full-`decimal`-precision.
- `CurrencyResolution` ambient currency-lookup seam — `Current`, `SetDefault(ICurrencyLookup)`, and a flow-scoped, `AsyncLocal`-backed `PushScoped(ICurrencyLookup)` override. Runtime `Money` construction, minor-unit resolution, parsing, and formatting resolve currencies through `CurrencyResolution.Current`; the default is the registry-backed `CurrencyLookupService`, so behaviour is unchanged unless a host or test substitutes a catalogue. The companion `Bodu.Financial.DependencyInjection` package adds `IServiceProvider.UseBoduFinancialCurrencyResolution()` to promote the container-registered `ICurrencyLookup` to the ambient default at start-up.
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

// After — install Bodu.Globalization.Calendar.AsiaPacific and pass its provider:
using Bodu.Globalization.Calendar;

var service = new NotableDateService(
    ruleProviders:     new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);
var auDates = service.GetNotableDates(2026, "AU");
```

Use `<Pack>CalendarData.CreateProviders()` to enumerate every country in a pack at once. See the [Calendar data packs](docs/guides/calendar/data-packs.md) guide for full composition patterns.

### Bodu.Globalization.Calendar.Americas — 1.0.0

Initial release. Ships embedded notable-date rules for the United States (`US`) and Canada (`CA`). Exposes a static `AmericasCalendarData` factory with per-country `CreateUnitedStatesProvider()` / `CreateCanadaProvider()` and bulk `CreateProviders()` helpers that pre-wire the `[pack, main library]` assembly chain.

### Bodu.Globalization.Calendar.Europe — 1.0.0

Initial release. Ships embedded notable-date rules for Germany (`DE`), Spain (`ES`), France (`FR`), the United Kingdom (`GB`), Ireland (`IE`), Italy (`IT`), the Netherlands (`NL`), and Sweden (`SE`). Exposes a static `EuropeCalendarData` factory with per-country and bulk `CreateProviders()` helpers.

### Bodu.Globalization.Calendar.AsiaPacific — 1.0.0

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
- Construction-neutral AEAD surface: `IAeadTransform` (`TagSize` plus one-shot `Encrypt`/`Decrypt` with optional associated data) and the `IStreamAeadTransform` marker. `IAeadBlockCipherModeTransform` now derives from `IAeadTransform` (via default-interface bridges), so the existing block-cipher AEAD family also exposes the neutral surface. `AeadTransformExtensions` adds array-returning helpers.
- Extended-nonce Poly1305 AEAD constructions, filling the gap left by the BCL's 12-byte-nonce `ChaCha20Poly1305`: `XChaCha20Poly1305` (RFC 8439 framing over XChaCha20, libsodium `crypto_aead_xchacha20poly1305_ietf`), `XSalsa20Poly1305` (NaCl/libsodium `crypto_secretbox`-compatible — no AAD, with `ToLibsodiumCombined` / `FromLibsodiumCombined` layout converters since Bodu emits `ciphertext ‖ tag` versus libsodium's `tag ‖ ciphertext`), and the Bodu-defined `XSalsa20Poly1305Aead` (XSalsa20 under RFC 8439 framing; not an IETF standard and not interoperable). All ship on the public `Poly1305AeadTransform` base implementing `IStreamAeadTransform`, reuse the existing ChaCha20/Salsa20 stream engines and Poly1305 MAC (streamed, not materialised), decrypt verify-before-release, support exact in-place operation while rejecting partial overlap, and clear key material on dispose. Validated against the RFC 8439 §2.8.2, draft-irtf-cfrg-xchacha A.3.1, and libsodium secretbox vectors, plus a derived oracle and frozen vector for the Bodu-defined hybrid.
- Password-hashing KDFs `Argon2d` / `Argon2i` / `Argon2id` (RFC 9106) and `Scrypt` (RFC 7914) — the remaining BCL gap (`HKDF` and `Pbkdf2` already ship in `System.Security.Cryptography`; Microsoft has declined Argon2). Each exposes both an instance surface (`new Argon2id(parameters)` / `new Scrypt(N, r, p)` with `GetBytes` / `DeriveKey`) and static one-shot `DeriveKey` helpers, plus PHC encoded-hash `Hash` and constant-time `Verify` for password storage (the `$argon2id$v=19$m=…,t=…,p=…$salt$hash` and `$scrypt$ln=…,r=…,p=…$salt$hash` forms). Argon2 bundles its own arbitrary-length BLAKE2b for the variable-length hash `H'`; scrypt composes the BCL `Rfc2898DeriveBytes.Pbkdf2` (HMAC-SHA256) with a Salsa20/8 core. Working memory and tags are zeroed on completion. Locked against every published RFC vector — the three Argon2d/i/id tags of RFC 9106 §5 and all four scrypt vectors of RFC 7914 §12 (including the ~1 GiB `N=1048576` vector in the Stress tier).

### Bodu.IO.Hashing — *(unversioned, next minor)*

#### Added

- `Verhoeff` check-digit algorithm (dihedral group D5), detecting all single-digit substitution errors and all adjacent-digit transpositions. Exposes the same static `Compute` / `IsValid` and streaming `Append` / `GetCurrentCheckDigit` / `Reset` surface as the other check-digit algorithms.
- `Gumm` check-digit algorithm — H. Peter Gumm's 1985 dihedral-group method (independent co-discovery of Verhoeff's result), detecting all single-digit substitution errors and all adjacent-digit transpositions. Instantiated over D5 with the transform `T(e,x) = (e, e(2−x)+1)` applied at alternating positions; a Regression test exhaustively verifies both detection guarantees over every body of length 1–4.
- `Code39Mod43` check-character algorithm — the modulo-43 self-check defined by the Code 39 (Code 3 of 9) barcode symbology, over the forty-three-symbol alphabet (`'0'`–`'9'`, `'A'`–`'Z'`, and `'-' '.' ' ' '$' '/' '+' '%'`).
- `Crockford32` check-symbol algorithm — Douglas Crockford's Base32 modulo-37 check symbol (the scheme commonly paired with ULID), with case-insensitive decoding, the `'I'`/`'L'`→1 and `'O'`→0 aliases, and the five check-only symbols `'*' '~' '$' '=' 'U'` for the values 32–36.
- `CheckDigitInputAlphabet` and `CheckDigitOutputAlphabet` gain `Code39`, `CrockfordBase32`, and `CrockfordBase32Check` members so the new algorithms can declare their alphabets.
