# Bodu.Financial.Samples.AggregatedRates

Multi-provider aggregation, demonstrated offline with two static feeds whose coverage is
deliberately complementary.

```bash
dotnet run --project samples/financial/Bodu.Financial.Samples.AggregatedRates
```

## What it demonstrates

- `Scenarios/PriorityFallback.cs` — `AggregatingRateProvider` with the default
  `PriorityFallbackStrategy`: first success wins, unquoted pairs fall through to the next source.
- `Scenarios/Averaging.cs` — `AverageStrategy`: the mean of all contributing sources under a
  synthetic provider label (not for auditable conversions).
- `Scenarios/PerPairRouting.cs` — `CurrencyPairRoute`: per-pair provider order and per-pair
  strategy overrides, with unrouted pairs keeping the defaults.
- `Scenarios/DependencyInjection.cs` — the whole stack via
  `AddFinancialService().AddAggregatedRateProvider(...)`: cached children, fluent `MapPair`
  routes, and keyed per-child resolution.

## Data

`Data/central-bank-a.csv` (AUD/USD, AUD/EUR) and `Data/central-bank-b.csv` (AUD/USD, AUD/JPY)
hold illustrative Q1 2024 business-day rates; Bank B's USD fix visibly differs from Bank A's so
priority vs averaging produce distinguishable output. `Program.cs` shows the commented switch to
live `Ecb`/`Rba` providers via `AddCachedChild<TProvider>`.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.ExchangeRates.Caching
dotnet add package Bodu.Financial.DependencyInjection
```
