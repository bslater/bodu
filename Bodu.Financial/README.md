# Bodu.Financial

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Financial primitives for .NET — runtime-tagged and strongly-typed money, the full ISO 4217 currency catalogue (with numeric codes and an active-currency enum), multi-currency aggregates, and a foreign-exchange provider stack with both runtime and typed rate forms.

References **[Bodu.Numerics](https://www.nuget.org/packages/Bodu.Numerics)** for the exact-arithmetic escape hatch (`Money<T>.ToFraction()` / `FromFraction` / `MultiplyExact`) through `Fraction<BigInteger>`.

## Installation

```shell
dotnet add package Bodu.Financial
```

Targets `net8.0`.

## `Money` — runtime-tagged primary

The runtime `Money` type carries the currency as ISO 4217 data alongside the amount. Use it when the currency varies at runtime — bank-feed imports, broker exports, JSON DTOs, multi-currency portfolios, ledger reads. Cross-currency operations surface `InvalidOperationException` at runtime.

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money price = Money.From(19.995m, CurrencyCode.AUD);   // rounds to 20.00 (2 minor units)
Money tax   = price * 0.1m;                            // 2.00 AUD
Money total = price + tax;                             // 22.00 AUD

// Money.From(...) overloads: string, CurrencyInfo, CurrencyCode
Money usd = Money.From(50m, "USD");
Money eur = Money.From(100m, CurrencyRegistry.Get("EUR"));
```

Highlights:

- Construction by ISO code, `CurrencyInfo`, or the generated `CurrencyCode` enum.
- Same-currency arithmetic and comparison; mixed-currency operations throw `InvalidOperationException`.
- `Allocate(int parts)` / `Allocate(ReadOnlySpan<decimal> ratios)` distribute integer minor-unit shares that sum exactly to the original, sign-stable.
- Round-trip parsing and rich formatting — see [format specifiers](#format-specifiers).
- `RoundToCash(MidpointRounding)` snaps to cash denominations.
- Bridge to the typed form via `money.As<TCurrency>()` / `money.TryAs<TCurrency>(out)` and the `(Money<USD>)money` explicit cast.

## `Money<TCurrency>` — strongly-typed specialisation

When an operation, account, or contract is invalid in any currency other than one fixed by the domain, use `Money<TCurrency>`. The currency is encoded as a type parameter via an `ICurrency` tag, so cross-currency arithmetic is a **compile error** rather than a runtime exception.

```csharp
Money<USD> price = Money.Of<USD>(19.995m);   // rounds to 20.00 (2 minor units)
Money<USD> tax   = price * 0.1m;             // 2.00
Money<USD> total = price + tax;              // 22.00

// Money<USD> + Money<JPY>  // compile error — currencies cannot be mixed

Money runtime = total;                       // implicit upcast to runtime Money
Money<USD> back = (Money<USD>)runtime;       // explicit; throws on currency mismatch
```

Highlights:

- Same-currency arithmetic and comparison operators, scalar multiply/divide (`Money<T> * decimal`), and dimensionless ratio (`Money<T> / Money<T> → decimal`).
- `Allocate(int parts)` / `Allocate(ReadOnlySpan<decimal> ratios)` — same semantics as the runtime form.
- Cross-currency conversion via either a decimal rate (`Convert<TTarget>(rate, rounding)`) or a typed rate (`Convert(ExchangeRate<TCurrency, TQuote>, rounding)`); the typed form catches direction errors at compile time.
- Exact-arithmetic escape via `ToFraction()` / `FromFraction(...)` / `MultiplyExact(...)` so chained calculations defer rounding to the final step.
- Rich formatting (`IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable`) — shared specifier vocabulary with `Money`.
- `RoundToCash(MidpointRounding)` snaps to cash denominations (CHF 5 rappen, CAD/AUD 5¢, NZD 10¢, SEK/NOK whole-krona).

## When to use which

| Scenario | Type |
|---|---|
| Deserialising JSON / database row / bank feed | `Money` |
| Generic invoicing engine accepting multiple currencies | `Money` |
| Multi-currency portfolio | `Money` (+ `MoneyBag` for aggregation) |
| Account fixed to a single currency | `Money<TCurrency>` |
| Tax calculation in a single jurisdiction | `Money<TCurrency>` |
| FX conversion contract pinned to specific base/quote | `Money<TCurrency>` + `ExchangeRate<TBase, TQuote>` |
| Generic FX conversion service over runtime pairs | `Money` + `ExchangeRate` |

Bridge between them: typed → runtime is implicit (lossless); runtime → typed is explicit via `As<T>()` / `TryAs<T>(...)` / `(Money<T>)money`.

### Settlement vs calculation

There are three precision models; pick by how rounding should behave:

| Type | Purpose | Precision | Rounding boundary |
|---|---|---|---|
| `Money` / `Money<TCurrency>` | Settlement amount — safe to persist, display, post | Rounded to the currency's minor units | At every construction/operation |
| `CalculatedMoney` | Intermediate calculation chain (interest, unit-rate, apportionment) | High-precision `decimal` (not exact rational) | Once, at `RoundToMoney(...)` |
| `Fraction` APIs (`Money<TCurrency>.FromFraction` / `MultiplyExact`) | Calculations that must be mathematically exact | Exact rational | Once, on conversion to `Money<TCurrency>` |

Scalar `*` and `/` on `Money` use banker's rounding (`MidpointRounding.ToEven`); use `Multiply(factor, rounding)` / `Divide(divisor, rounding)` for any other rule.

## `CurrencyInfo` and `CurrencyCode`

`CurrencyInfo` is the canonical runtime metadata record: ISO 4217 alphabetic and numeric codes, minor-unit precision, cash-rounding increment, historicity flag, demonetization date, English name. Looked up via `CurrencyRegistry.Get("AUD")` / `TryGet(...)`; custom currencies register via `CurrencyRegistry.Register(...)`.

`CurrencyCode` is a source-generated enum over the active ISO 4217 currencies — discoverable via IntelliSense, type-safe at compile time. Each member is the three-letter ISO code; each value is the ISO 4217 numeric code.

```csharp
CurrencyInfo aud = CurrencyInfo.FromCurrencyCode(CurrencyCode.AUD);
bool ok = CurrencyInfo.TryGetCurrencyCode("AUD", out CurrencyCode code);
CurrencyCode jpy = aud.ToCurrencyCode();   // throws for historic / custom currencies
```

Historic currencies (DEM, FRF, ITL, etc.) remain accessible through `CurrencyRegistry` and the `Bodu.Financial.Currencies` tag classes; they are intentionally excluded from `CurrencyCode` to keep the enum stable when ISO retires a code.

## Foreign exchange

The exchange-rate surface lives in the `Bodu.Financial.ExchangeRates` namespace (`using Bodu.Financial.ExchangeRates;`). Both runtime and typed rate forms are first-class:

```csharp
// Runtime rate — direction is data
var runtime = new ExchangeRate("USD", "AUD", new DateOnly(2026, 1, 15), 1.52m, "ECB");

// Typed rate — direction is fixed at compile time; reverse direction is a compile error
var typed = new ExchangeRate<USD, AUD>(1.52m, new DateOnly(2026, 1, 15), "ECB");

Money<USD> source = Money.Of<USD>(100m);
Money<AUD> result = source.Convert(typed);              // typed Convert overload
ExchangeRate<AUD, USD> reverse = typed.Inverse();       // reciprocal rate, typed
```

Bridge between forms via `typed.ToRuntime()` and `ExchangeRate<TBase, TQuote>.FromRuntime(runtime)` (the latter throws when the runtime ISO codes do not match the type parameters).

Provider stack:

- Timeless: `IRateProvider` with `FixedRateTable` (inverse-rate fallback).
- Dated: `IDatedRateProvider`, `FixedDatedRateProvider`, `RateBook`, `RateSeries`, and `DatedRateProviderAdapter`. Grouping several providers (priority-fallback, averaging, per-pair routing) lives in `Bodu.Financial.ExchangeRates.Caching` as `AggregatingRateProvider`.

Lookup surface — `IDatedRateProvider` exposes a symmetric matrix of single-date and range getters, each with a synchronous and an asynchronous form:

- Single-date `GetRate` / `GetRateAsync` (timeless and dated overloads) return a `RateLookupResult` — the resolved `ExchangeRate` plus the `RequestedDate`, the `RateDateResolution` that was applied, and the `OffsetDays` between the requested and resolved dates. Resolution and tolerance are controlled per call through `RateLookupOptions`.
- Range `GetRates` / `GetRatesAsync` return a `RateRangeResult`: the observations within the window ordered by date, the requested window (`RequestedStartDate` / `RequestedEndDate`), and the observed span (`FirstObservedDate` / `LastObservedDate`, plus `Count` / `IsEmpty`). The result *is* the rate sequence — it implements `IReadOnlyList<ExchangeRate>`, so it indexes, enumerates, and composes with LINQ directly, while comparing the requested window to the observed span reveals how much of the window carried data.

HTTP-backed providers (`Bodu.Financial.ExchangeRates.{Yahoo,Boe,Ecb,Rba,Ofx,Xe,Oanda}`) share the `WebRateProvider` base from the separate [`Bodu.Financial.ExchangeRates`](../Bodu.Financial.ExchangeRates) infrastructure package (this core package carries no HTTP machinery), which centralizes accumulation, the snapshot/lookup matrix, on-demand fetch coalescing, and the `HistoryAvailability` each provider advertises (how far back it serves rates — unbounded, a fixed earliest date, or a rolling window such as OANDA's ~180 days). Each provider offers two constructors and is `IDisposable`:

- `new XProvider(options, ...)` — the provider builds, owns, and disposes its own `HttpClient` (created via `RateProviderHttpClientFactory.Create`). Dispose the provider to release it.
- `new XProvider(httpClient, options, ...)` — the caller supplies the client and owns its lifetime; the provider never disposes a borrowed client. This is the form the `*.DependencyInjection` packages use with `IHttpClientFactory`.

## `MoneyBag`

Immutable mixed-currency aggregate for portfolios and ledgers. Per-currency balances are tracked separately, zero balances pruned automatically, and the bag converts to a single target currency through `IRateProvider`.

```csharp
var bag = new MoneyBag(new[]
{
    Money.From(100m, "USD"),
    Money.From(85m, "EUR"),
    Money.Of<JPY>(15_000),
});
```

## Allocation

Both `Money` and `Money<TCurrency>` support exact integer-minor-unit allocation that sums to the original amount:

```csharp
Money split = Money.From(0.10m, "USD");
Money[] shares = split.Allocate(3);                              // [0.04, 0.03, 0.03]

Money pro = Money.From(100m, "USD");
Money[] proportional = pro.Allocate(new[] { 1m, 2m, 3m });       // largest-remainder method
```

## Format specifiers

`Money` and `Money<TCurrency>` share a case-insensitive specifier vocabulary:

| Specifier | Behaviour | Example (`Money.From(1234.56m, "USD")`, en-US) |
|---|---|---|
| `G`, `(null)`, `""` | ISO code + culture-grouped number | `USD 1,234.56` |
| `C` | Culture-native symbol; ISO-substituted in the culture's currency-position slot on culture/currency mismatch | `$1,234.56` (`Money` carrying `JPY` in en-US: `JPY 1,234`) |
| `L` | Amount followed by the English currency name; falls back to ISO when no name is supplied | `1,234.56 US Dollar` |
| `R` | Invariant round-trip (always invariant culture, no grouping, natural precision); ignores `provider`; rejects `~` and precision suffixes | `USD 1234.56` |
| `N` | Number with culture grouping, no currency designator | `1,234.56` |
| `F`, `D` | Fixed-point, no grouping, no currency designator | `1234.56` |

- Prefix `~` on `C`, `G`, or `L` elides the currency designator when the culture's region currency matches the money's currency, while keeping it when they differ.
- A numeric suffix on `C`, `G`, `L`, `N`, `F`, or `D` overrides the currency's natural precision (e.g. `"C0"` → `$20`, `"L4"` → `19.9900 US Dollar`).

Compact-notation overloads (`ToCompactString(...)`) add a K/M/B/T magnitude suffix to the numeric portion, preserving the chosen specifier's symbol position.

> **Format-specifier reference.** `G` is the ISO-code-prefixed form, `C` is the culture-native symbol form, `L` emits the English currency name, and `R` is the invariant round-trip form. (Earlier builds spelled the ISO-code-prefix form `C` and the culture-native form `L`; code written against those should rename `C` → `G` and `L` → `C`.)

## Serialization

`Bodu.Financial` is serialization-agnostic — the monetary types carry no `[JsonConverter]` attribute and the core library ships no `System.Text.Json` integration of its own. JSON converters for `Money`, `Money<TCurrency>`, `MoneyBag`, `ExchangeRate`, and `CurrencyPair` live in the companion [`Bodu.Financial.Serialization.Json`](https://github.com/bslater/bodu/tree/master/Bodu.Financial.Serialization.Json) package; registration is required:

```csharp
using Bodu.Financial.Serialization.Json;

var options = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Strict);
```

Three policies control the wire shape:

- **Strict** (default) — object form `{ "amount": 19.99, "currency": "AUD" }`; rejects duplicate keys and mismatched / lowercase ISO codes.
- **Lenient** — accepts lowercase currency codes and surrounding whitespace.
- **Compact** — accepts the round-trip string form `"19.99 AUD"`.

> **Migration note.** Earlier builds shipped the converters inside `Bodu.Financial` and serialized through type-level `[JsonConverter]` attributes with zero configuration. Without the registration above, `JsonSerializer` now falls back to reflection-shaped output instead of the canonical shapes.

## Currency catalogue

Covers the ~155 active ISO 4217 currencies plus 29 historic / demonetised currencies under `Bodu.Financial.Currencies` (`USD`, `EUR`, `GBP`, `JPY`, `BHD`, `KWD`, the Euro-zone predecessors `DEM` / `FRF` / `ITL` / `ESP` / …, `VEF`, `ZWL`, …). Tag types, the `CurrencyCode` enum, and the metadata registration list are source-generated from `currencies.json` by `tools/CurrencyCatalogueGenerator`; consumers add custom currencies by implementing `ICurrency` directly and registering them on `CurrencyRegistry`.

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for the financial packages — money basics, static-file rate providers, caching, aggregation, DI wiring, and a custom provider with contract tests — under [`samples/Financial/`](https://github.com/bslater/bodu/tree/master/samples/Financial).

## License

MIT. © Bodu Pty. Ltd.
