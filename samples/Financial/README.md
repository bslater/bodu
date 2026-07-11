# Financial Samples

Console applications demonstrating the `Bodu.Financial` package family. Each sample is a
standalone project; run one with:

```bash
dotnet run --project samples/Financial/<SampleName>
```

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Financial.Samples.MoneyBasics` | Three-tier rounding (`Money<T>` → `CalculatedMoney` → `Fraction<BigInteger>`), typed↔runtime bridges, largest-remainder allocation, cash rounding, formatting/parsing, `MoneyBag` ledgers with conversion audit, JSON policies | `Bodu.Financial` |
| `Bodu.Financial.Samples.OfflineRates` | **The offline static-rate-file pattern**: CSV → `RateTableBuilder` → `RateBook` → `FixedDatedRateProvider`; `RateLookupOptions` date-resolution modes; converting money with dated rates | `Bodu.Financial` |
| `Bodu.Financial.Samples.CachedRates` | Read-through caching (`CachingRateProvider`), coverage-based range serving (incl. negative caching of empty windows), tiered cache stacking (memory over file), history-availability clamping | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.Caching` |
| `Bodu.Financial.Samples.AggregatedRates` | Multi-provider aggregation: priority fallback, averaging, per-pair routing, and the `AddAggregatedRateProvider` DI builder | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial.DependencyInjection` |
| `Bodu.Financial.Samples.CurrencyServices` | Ambient currency resolution (`CurrencyResolution`), named `MonetaryContext`s, the `AddFinancialService` host wiring | `Bodu.Financial`, `Bodu.Financial.DependencyInjection` |
| `Bodu.Financial.Samples.CustomProvider` (+ `.Test`) | Writing your own `IDatedRateProvider` and validating it with the shipped contract-test bases | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial.ExchangeRates.Testing` |
| `Bodu.Financial.Samples.LiveRates` | **Online** (the one exception): fetch real published rates from a live provider — ECB active, RBA/BoE/Yahoo/OFX/OANDA/XE/Fixer/exchangerate.host/FRED/IMF comment-switchable — for a buffered "last Wednesday" date and its trailing week | `Bodu.Financial.ExchangeRates.Ecb` (+ the other ten provider packages) |

## Offline by default, live by choice

The exchange-rate samples never touch the network. They build a
`FixedDatedRateProvider` from a committed CSV of illustrative daily rates
(`Data/aud-daily-2024H1.csv` — synthetic values approximating published 2024 H1 levels),
so output is deterministic and the samples work behind any firewall.

Every place a live feed *could* be used carries a fenced comment block like this:

```csharp
// --- To use the live Reserve Bank of Australia feed instead -----------------
// 1. dotnet add package Bodu.Financial.ExchangeRates.Rba
// 2. Replace the offline source above with:
//
//     using var source = new RbaRateProvider(new RbaRateProviderOptions());
//     await source.LoadRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
// ----------------------------------------------------------------------------
```

Uncomment, add the package, and the rest of the sample works unchanged — every provider
in the family serves the same `IDatedRateProvider` / `IRateProvider` contracts.
