---
uid: Bodu.Financial.Extensions
---

![Bodu.Financial](~/images/hero-financial.svg)

# Bodu.Financial.Extensions

## Purpose

**Bodu.Financial.Extensions** is the extension-member namespace of the core [`Bodu.Financial`](Bodu.Financial.md) package. It carries the derived helpers that are deliberately kept off the value types themselves — sign and magnitude tests, clamping and comparison, compact `"$1.2K"` formatting, dated-provider conversion, audit conveniences on a lookup result, and the materializers that turn observations into an immutable book or a fixed provider — so that <xref:Bodu.Financial.Money>, <xref:Bodu.Financial.Money`1>, and <xref:Bodu.Financial.ExchangeRates.RateLookupResult> stay focused on construction, arithmetic, equality, and formatting.

Every member is a thin projection over the public surface of the type it extends and carries no state of its own. Add `using Bodu.Financial.Extensions;` to bring them into scope.

When the library is compiled with a tool-chain that supports C# 14 extension members (the .NET 10 SDK the repository pins), the instance helpers are exposed as extension **properties** — `money.IsZero`, `result.IsExactDate` — and otherwise as classic extension **methods** — `money.IsZero()`, `result.IsExactDate()`. Both spellings appear in the guides; the property form is the one the shipped packages expose. The comparison helpers `Min`, `Max`, and `Clamp` take two or more operands and are therefore ordinary static methods (`MoneyOfTCurrencyExtensions.Clamp(value, min, max)`) under either tool-chain.

## Static documentation

- **[Working with `Money<TCurrency>`](~/guides/financial/money.md)** — the value types these helpers extend, including [compact formatting](~/guides/financial/money.md#compact-formatting).
- **[Working with exchange rates](~/guides/financial/exchange-rates.md)** — dated lookups, provenance, and the [audit-grade conversion](~/guides/financial/exchange-rates.md#audit-grade-conversion-through-moneytcurrency) these extensions provide.
- **[Exchange-rate lookups on a known dataset](~/guides/financial/exchange-rate-lookups.md)** — reading the resolution metadata the `RateLookupResult` helpers summarize.
- **[Built-in exchange-rate providers](~/guides/financial/exchange-rate-providers.md#snapshotting-and-exporting-rates)** — snapshotting a provider's book with `ToBook` / `ToFixedProvider` / `ToFixedProviderAsync`.

## Key types

**Sign and magnitude**

- <xref:Bodu.Financial.Extensions.MoneyExtensions> — for the runtime-tagged <xref:Bodu.Financial.Money>: `Abs`, `Sign` (`-1` / `0` / `1`), `IsZero`, `IsPositive`, `IsNegative`.
- <xref:Bodu.Financial.Extensions.MoneyOfTCurrencyExtensions> — the same five members for <xref:Bodu.Financial.Money`1>, plus the static comparison helpers `Min(left, right)`, `Max(left, right)`, and `Clamp(value, min, max)` (throws `ArgumentException` when `min > max`). Being generic over `TCurrency`, they only ever compare amounts in the same currency.

**Formatting**

- <xref:Bodu.Financial.Extensions.MoneyCompactFormattingExtensions> — `ToCompactString(format = "C", provider = null, precision = 1)` for both money types: scales by thousands, millions, billions, or trillions and appends `K` / `M` / `B` / `T` to the numeric portion, honoring the format specifier's currency placement per culture (`"$1.2K"`, `"1,2K €"`). The round-trip `R` specifier is rejected with `FormatException` because compact output cannot round-trip.

**Conversion through a dated provider**

- <xref:Bodu.Financial.Extensions.MoneyOfTCurrencyExchangeRateExtensions> — `ConvertTo<TSource, TTarget>(provider, date, options, rounding)` returns the converted <xref:Bodu.Financial.Money`1>; `ConvertToWithRate<TSource, TTarget>(…)` returns a <xref:Bodu.Financial.MoneyConversionResult`2> carrying `SourceAmount`, `TargetAmount`, and the full `ExchangeRate` lookup result for the audit trail.
- <xref:Bodu.Financial.Extensions.MoneyExchangeRateExtensions> — the runtime-tagged counterparts on <xref:Bodu.Financial.Money>: `ConvertTo(provider, targetIsoCode, date, …)` and `ConvertTo<TTarget>(provider, date, …)` return the converted amount; `ConvertToWithRate(provider, targetIsoCode, date, …)` returns a `(Money Target, RateLookupResult Rate)` tuple.

All four resolve the rate with `IDatedRateProvider.GetRate(from, to, date, options)`, so a miss surfaces as `KeyNotFoundException`, and round the product at the destination currency's precision with the supplied `MidpointRounding` (banker's rounding by default).

**Lookup-result audit**

- <xref:Bodu.Financial.Extensions.RateLookupResultExtensions> — derived views over <xref:Bodu.Financial.ExchangeRates.RateLookupResult>: `ResolvedDate` (the observed date, `Rate.Date`), `SignedOffsetDays` (negative when the observation predates the request, positive when it post-dates it, zero on an exact match), `IsExactDate`, `IsPreviousDate`, and `IsFutureDate`.

**Materializing books and providers**

- <xref:Bodu.Financial.Extensions.ExchangeRateEnumerableExtensions> — `ToBook()` on any `IEnumerable<ExchangeRate>`: one series per `(pair, provider)`, upsert semantics for duplicate dates, inverse-resolved rows stored under their natively quoted direction, and each series' `FetchedAtUtc` set to the latest instant seen — so aggregated or multi-source range results round-trip without error.
- <xref:Bodu.Financial.Extensions.RateBookExtensions> — `ToFixedProvider()` wraps a <xref:Bodu.Financial.ExchangeRates.RateBook> in a <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider> (throws when a pair has two providers); `ToFixedProvider(providerPriority)` resolves that ambiguity with an ordered provider list.
- <xref:Bodu.Financial.Extensions.DatedRateProviderExtensions> — `ToFixedProviderAsync(pairs, startDate, endDate, cancellationToken)` fetches each distinct pair's window through `GetRatesAsync` (sequentially — the web providers already coalesce) and materializes an immutable, source-independent <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>.

## Example

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

Money<USD> balance = new Money<USD>(-1_250_000m);

bool overdrawn = balance.IsNegative;                       // true  (IsNegative() under the method form)
Money<USD> magnitude = balance.Abs;                        // USD 1,250,000.00
string compact = magnitude.ToCompactString();              // "$1.3M" in en-US

Money<USD> fee = MoneyOfTCurrencyExtensions.Clamp(
    new Money<USD>(3.25m),
    new Money<USD>(1m),
    new Money<USD>(2.50m));                                // USD 2.50 — clamped to the upper bound

// Audit-grade conversion through a dated provider.
IDatedRateProvider provider = new FixedDatedRateProvider(new[]
{
    new ExchangeRate(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 14), 0.93m, "Test"),
});

MoneyConversionResult<USD, EUR> converted = new Money<USD>(100m).ConvertToWithRate<USD, EUR>(
    provider,
    new DateOnly(2024, 6, 16),
    RateLookupOptions.PreviousWithin(7));

Console.WriteLine(converted.TargetAmount);                 // EUR 93.00
Console.WriteLine(converted.ExchangeRate.SignedOffsetDays); // -2 — the Friday observation served a Sunday request
Console.WriteLine(converted.ExchangeRate.IsExactDate);      // False

// Freeze a range of observations into an offline provider.
RateRangeResult window = provider.GetRates("USD", "EUR", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30));
FixedDatedRateProvider offline = window.ToBook().ToFixedProvider();
```

## Notes

- **Extension properties vs methods.** Under the C# 14 tool-chain the instance helpers are properties (`money.IsZero`); under an older tool-chain they are methods (`money.IsZero()`). The static `Min` / `Max` / `Clamp` helpers are called through the class name in both cases.
- **Same-currency only.** `Min`, `Max`, and `Clamp` are generic over one `TCurrency`, so mixing currencies is a compile error, not a runtime check; the runtime-tagged <xref:Bodu.Financial.Money> has no comparison helpers because a cross-currency comparison has no meaningful answer.
- **Conversion rounds once.** Each `ConvertTo` multiplies the source amount by the resolved rate and rounds once at the destination precision; the unrounded rate is available on the returned lookup result when an audit needs it.
- **`ToBook` keeps multiple providers apart.** Unlike the `FixedDatedRateProvider(IEnumerable<ExchangeRate>)` constructor, which requires one provider per pair, `ToBook()` accepts rates for the same pair from several providers and keeps them as separate series; choose which wins with `ToFixedProvider(providerPriority)`.
- **See also:** the [`Bodu.Financial` reference](xref:Bodu.Financial) and the [`Bodu.Financial.ExchangeRates` reference](xref:Bodu.Financial.ExchangeRates).
