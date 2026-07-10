# Bodu.Financial.Samples.CustomProvider

Consumer extensibility: write your own `IDatedRateProvider` and prove it with the shipped
contract-test base.

```bash
dotnet run --project samples/financial/Bodu.Financial.Samples.CustomProvider
dotnet test samples/financial/Bodu.Financial.Samples.CustomProvider.Test
```

## What it demonstrates

- `CsvFileRateProvider.cs` — the recommended custom-provider shape: parse your source into a
  `RateTableBuilder`, freeze it into a `RateBook`, and delegate the lookup surface to a
  `FixedDatedRateProvider` so date resolution, inverse fallback, identity rates, and provenance
  come for free.
- `Program.cs` — the custom provider used directly (exact/inverse/weekend lookups), through the
  `ConvertTo` money extensions, and composed under `CachingRateProvider` like any shipped source.
- `../Bodu.Financial.Samples.CustomProvider.Test/CsvFileRateProviderTests.cs` — deriving
  `DatedRateProviderContractTests<CsvFileRateProvider>` from
  `Bodu.Financial.ExchangeRates.Testing`: supply the seeded provider plus known/unknown dates and
  the base validates the entire `IDatedRateProvider` contract (the test project runs in CI with
  the library suites).

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
dotnet add package Bodu.Financial.ExchangeRates.Caching
# test project only:
dotnet add package Bodu.Financial.ExchangeRates.Testing
```
