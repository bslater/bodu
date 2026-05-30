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

string display = total.ToString("L", CultureInfo.GetCultureInfo("en-US")); // "$22.00"
```

Highlights:

- Same-currency arithmetic and comparison operators, scalar multiply/divide (`Money<T> * decimal`), and dimensionless ratio (`Money<T> / Money<T> → decimal`).
- `Allocate(int parts)` / `Allocate(ReadOnlySpan<decimal> ratios)` distribute an amount as integer minor-unit shares that sum exactly to the original, sign-stable.
- Cross-currency conversion exclusively through the explicit `Convert<TTarget>(rate, rounding)` method.
- Exact-arithmetic escape via `ToFraction()` / `FromFraction(...)` / `MultiplyExact(...)` so chained calculations defer rounding to the final step.
- Rich formatting (`IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable`) with ISO-code, locale-symbol, and grouping-controlled specifiers, plus strict parsing.
- `RoundToCash(MidpointRounding)` snaps to cash denominations (CHF 5 rappen, CAD/AUD 5¢, NZD 10¢, SEK/NOK whole-krona).

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
