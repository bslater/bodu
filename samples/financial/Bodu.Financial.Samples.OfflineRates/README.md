# Bodu.Financial.Samples.OfflineRates

The flagship **offline static-rate-file** sample: build a fully functional dated exchange-rate
provider from a committed CSV — no network, no API keys — and use it to resolve rates and
convert money.

```bash
dotnet run --project samples/financial/Bodu.Financial.Samples.OfflineRates
```

## What it demonstrates

- `StaticRates.cs` — the CSV → `RateTableBuilder` → `RateBook` → `FixedDatedRateProvider`
  pipeline (the mutable-builder → immutable-store → provider pattern).
- `Scenarios/LookupModes.cs` — the four `RateLookupOptions` date-resolution modes
  (`Exact`, `PreviousWithin`, `NextWithin`, `NearestWithin`) and tolerance bounds.
- `Scenarios/ConvertMoney.cs` — converting `Money<T>` and runtime `Money` through a dated
  provider, including the typed `ExchangeRate<TBase, TQuote>.FromRuntime` bridge.

## Data

`Data/aud-daily-2024H1.csv` holds illustrative AUD-based business-day rates for 2024 H1
(synthetic values approximating published levels — see the file header). `Program.cs` shows
the commented-out switch to the live `RbaRateProvider`.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
# optional, for the live feed:
dotnet add package Bodu.Financial.ExchangeRates.Rba
```
