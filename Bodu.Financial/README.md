# Bodu.Financial

Financial primitives for .NET — type-safe money, the full ISO 4217 currency catalogue, multi-currency aggregates, and a foreign-exchange provider stack.

> **API stability: Preview.** This is the initial 1.0 release. The public surface is expected to be stable, but minor breaking changes may still occur before the API is promoted to *Stable*.

References **[Bodu.Numerics](https://www.nuget.org/packages/Bodu.Numerics)** for the exact-arithmetic escape hatch (`Money<T>.ToFraction()` / `FromFraction` / `MultiplyExact`) through `Fraction<BigInteger>`.

## Installation

```shell
dotnet add package Bodu.Financial
```

Targets `net8.0`.

## `Money<TCurrency>`

The currency is encoded as a type parameter via an `ICurrency` tag, so cross-currency arithmetic is a **compile error**, not a runtime exception. The amount is stored as a `decimal`, rounded on construction to the currency's minor units using banker's rounding by default.

```csharp
using Bodu;
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money<USD> price = Money.Of<USD>(19.995m);   // rounds to 20.00 (2 minor units)
Money<USD> tax   = price * 0.1m;             // 2.00
Money<USD> total = price + tax;              // 22.00

// Money<USD> + Money<JPY>  // compile error — currencies cannot be mixed

string display = total.ToString("C", CultureInfo.GetCultureInfo("en-US")); // "$22.00"
```

Highlights:

- Same-currency arithmetic and comparison operators, scalar multiply/divide (`Money<T> * decimal`), and dimensionless ratio (`Money<T> / Money<T> → decimal`).
- `Allocate(int parts)` / `Allocate(ReadOnlySpan<decimal> ratios)` distribute an amount as integer minor-unit shares that sum exactly to the original, sign-stable.
- Cross-currency conversion exclusively through the explicit `Convert<TTarget>(rate, rounding)` method.
- Exact-arithmetic escape via `ToFraction()` / `FromFraction(...)` / `MultiplyExact(...)` so chained calculations defer rounding to the final step.
- Rich formatting (`IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable`) — see [format specifiers](#format-specifiers) — plus strict parsing.
- Compact notation (`$1.2K`, `€1.5M`, `USD 2.3B`) via `ToCompactString(...)` extension methods.
- `RoundToCash(MidpointRounding)` snaps to cash denominations (CHF 5 rappen, CAD/AUD 5¢, NZD 10¢, SEK/NOK whole-krona).

## Format specifiers

`Money<TCurrency>` and `MoneyValue` share a case-insensitive specifier vocabulary:

| Specifier | Behaviour | Example (`Money<USD>(1234.56m)`, en-US) |
|---|---|---|
| `G`, `(null)`, `""` | ISO code + culture-grouped number | `USD 1,234.56` |
| `C` | Culture-native symbol; ISO-substituted in the culture's currency-position slot on culture/currency mismatch | `$1,234.56` (`Money<JPY>` in en-US: `JPY 1,234`) |
| `L` | Amount followed by the English currency name; falls back to ISO when no name is supplied | `1,234.56 US Dollar` |
| `R` | Invariant round-trip (always invariant culture, no grouping, natural precision); ignores `provider`; rejects `~` and precision suffixes | `USD 1234.56` |
| `N` | Number with culture grouping, no currency designator | `1,234.56` |
| `F`, `D` | Fixed-point, no grouping, no currency designator | `1234.56` |

- Prefix `~` on `C`, `G`, or `L` elides the currency designator when the culture's region currency matches `TCurrency`, while keeping it when they differ.
- A numeric suffix on `C`, `G`, `L`, `N`, `F`, or `D` overrides the currency's natural precision (e.g. `"C0"` → `$20`, `"L4"` → `19.9900 US Dollar`).

Compact-notation overloads (`ToCompactString(...)`) add a K/M/B/T magnitude suffix to the numeric portion, preserving the chosen specifier's symbol position.

> **Breaking change (Preview → Stable).** The `C` and `L` specifiers changed meaning. The pre-1.0 `C` (ISO-code prefix) is now `G`. The pre-1.0 `L` (culture-native symbol) is now `C`. The new `L` emits the English currency name. The new `R` is the invariant round-trip form. Code that depended on the previous semantics should rename `C` → `G` (when ISO-prefix output was required) and `L` → `C` (when culture-native output was required).

## `MoneyValue`, `MoneyBag`, and `CurrencyRegistry`

- **`MoneyValue`** — runtime-tagged sister type for when the currency is data rather than part of the type (JSON deserialisation, generic invoicing). Cross-currency operations surface `InvalidOperationException` at runtime. Bridge via `ToTyped<TCurrency>()` / `FromTyped(...)`.
- **`MoneyBag`** — immutable mixed-currency aggregate for portfolios and ledgers. Per-currency balances are tracked separately, zero balances pruned, and the bag converts to a single target currency through `IExchangeRateProvider`.
- **`CurrencyRegistry`** — runtime ISO-to-metadata lookup over a frozen, source-generated catalogue (no runtime reflection scan), with custom-currency registration.

## Currency catalogue

Covers the ~155 active ISO 4217 currencies plus 29 historic / demonetised currencies under `Bodu.Financial.Currencies` (`USD`, `EUR`, `GBP`, `JPY`, `BHD`, `KWD`, the Euro-zone predecessors `DEM` / `FRF` / `ITL` / `ESP` / …, `VEF`, `ZWL`, …). Tag types are source-generated; consumers add custom currencies by implementing `ICurrency` directly.

## Foreign exchange

- Timeless: `IExchangeRateProvider` with `FixedExchangeRateTable` (inverse-rate fallback).
- Dated: `IDatedExchangeRateProvider`, `FixedDatedExchangeRateTable`, `CompositeDatedExchangeRateProvider`, `ExchangeRateSeries`, and `DatedExchangeRateProviderAdapter`.

## License

MIT. © Bodu Pty. Ltd.
