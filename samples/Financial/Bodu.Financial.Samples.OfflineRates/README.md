# Bodu.Financial.Samples.OfflineRates

The flagship **offline static-rate-file** sample: build a fully functional dated exchange-rate
provider from a committed CSV — no network, no API keys — and use it to resolve rates and
convert money. `StaticRates.cs` is the pattern itself: any rate data you already hold (a file, a
database table, an archived API response) pours through `RateTableBuilder` into an immutable
`RateBook` and serves through `FixedDatedRateProvider` — the same contracts the live web
providers implement, so everything downstream is interchangeable.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.OfflineRates
```

`Program.cs` carries the commented switch to the live `RbaRateProvider` (add the
`Bodu.Financial.ExchangeRates.Rba` package, replace one line, pin a snapshot with
`GetLoadedSnapshot()`).

## Scenarios

### LookupModes (`Scenarios/LookupModes.cs`)

**Intent.** Rate series only carry business-day observations, so any real system immediately
hits the question: what should a lookup on a Saturday, holiday, or missing day do? The four
`RateLookupOptions` date-resolution modes are the explicit answer, and tolerances are hard
bounds — this scenario shows all of it on one weekend.

**What it does.** Resolves AUD/USD for Saturday 2024-03-16 (the data has Friday the 15th and
Monday the 18th, nothing between) under each mode: `Exact` (via `TryGetRate`, which reports the
miss), `PreviousWithin(3)`, `NextWithin(3)`, and `NearestWithin(3)`; then asks for a date beyond
the end of the data with a 3-day tolerance to show the bound holding.

**What to expect.**

```
Exact              2024-03-16 -> no observation (Saturday)
PreviousWithin(3)  2024-03-16 -> 2024-03-15 (PreviousOnOrBefore, offset 1d): 0.6508 [SampleData]
NextWithin(3)      2024-03-16 -> 2024-03-18 (NextOnOrAfter, offset 2d): 0.6517 [SampleData]
NearestWithin(3)   2024-03-16 -> 2024-03-15 (NearestPreferPrevious, offset 1d): 0.6508 [SampleData]
PreviousWithin(3)  2024-07-15 -> outside tolerance, no result
```

Each resolved line shows *how* the date was resolved (the resolution kind and the offset in
days) plus the serving provider — the metadata an auditable valuation needs. `NearestWithin`
prefers the earlier date on ties. The last line proves a tolerance is a bound, not a suggestion:
three days past the dataset finds nothing.

**APIs demonstrated.** `RateLookupOptions.Exact` / `PreviousWithin` / `NextWithin` /
`NearestWithin`, `IDatedRateProvider.GetRate` / `TryGetRate`, `RateLookupResult`
(`RequestedDate`, `Resolution`, `OffsetDays`, `Rate.Provider`).

### ConvertMoney (`Scenarios/ConvertMoney.cs`)

**Intent.** Show the three ways money crosses currencies through a dated provider — the
one-call extension, the explicit lookup-then-typed-rate form (which catches direction mistakes
at compile time), and the runtime form for currencies only known at run time.

**What it does.** Converts a 2,499.95 AUD invoice at value date 2024-03-15 three ways:
`ConvertTo<AUD, USD>` in one call; an explicit `GetRate` for AUD/EUR bridged into a typed
`ExchangeRate<AUD, EUR>` via `FromRuntime` (which validates the runtime rate really is AUD→EUR)
and then `Convert`; and the runtime `Money.ConvertTo(provider, "JPY", …)` where the target
currency is a string. All use `PreviousWithin(5)` to tolerate weekends.

**What to expect.**

```
Invoice: AUD 2,499.95 (value date 2024-03-15)
  -> USD 1,626.97  (ConvertTo extension)
  -> EUR 1,548.97  (typed rate 0.6196, observed 2024-03-15)
  -> JPY 244,945  (runtime Money, target chosen at run time)
```

Each result is rounded to the target currency's minor units (JPY to whole yen). The typed line
prints the rate and its observation date — the two facts the typed bridge preserves and checks.

**APIs demonstrated.** `MoneyOfTCurrencyExchangeRateExtensions.ConvertTo<TSource, TTarget>`,
`ExchangeRate<TBase, TQuote>.FromRuntime`, `Money<T>.Convert(ExchangeRate<T, TQuote>)`,
`MoneyExchangeRateExtensions.ConvertTo(Money, provider, iso, date)`.

## Data

`Data/aud-daily-2024H1.csv` holds illustrative AUD-based business-day rates for 2024 H1
(AUD/USD, AUD/EUR, AUD/JPY — synthetic values approximating published levels; see the file
header). The natural weekend gaps in business-day data are exactly what the LookupModes scenario
exercises.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
# optional, for the live feed:
dotnet add package Bodu.Financial.ExchangeRates.Rba
```
