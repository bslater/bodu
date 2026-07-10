# Bodu.Financial.Samples.AggregatedRates

Multi-provider aggregation, demonstrated offline with two static feeds whose coverage is
deliberately complementary: **BankA** quotes AUD/USD and AUD/EUR, **BankB** quotes AUD/USD (a
visibly different fix) and AUD/JPY. That overlap-plus-gap layout gives fallback, averaging, and
routing something real to do, and BankB's different USD fix makes every strategy's choice
visible in the output.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.AggregatedRates
```

`Program.cs` carries the commented switch to aggregating live feeds
(`AddEcbExchangeRates()` / `AddRbaExchangeRates()` + `AddCachedChild<TProvider>`).

## Scenarios

### PriorityFallback (`Scenarios/PriorityFallback.cs`)

**Intent.** The default aggregation strategy: children are consulted in order and the first
success wins. The consumer sees *one* provider; pairs the primary source does not quote fall
through automatically, and provenance always names the source that actually answered.

**What it does.** Groups BankA (preferred) and BankB (fallback) in an
`AggregatingRateProvider` with default options, then resolves three pairs on the same date:
AUD/USD (both quote it), AUD/EUR (only BankA), and AUD/JPY (only BankB).

**What to expect.**

```
AUD/USD: 0.6568  served by BankA  (both quote it; priority wins)
AUD/EUR: 0.6028  served by BankA
AUD/JPY: 102.02  served by BankB  (fallback)
```

AUD/USD comes from BankA even though BankB also quotes it — priority order decides, not data
availability. AUD/JPY silently falls through to BankB; the caller code is identical for all
three lookups.

**APIs demonstrated.** `AggregatingRateProvider(children)`, `NamedDatedRateProvider`,
`PriorityFallbackStrategy` (the default), `RateLookupResult.Rate.Provider` provenance.

### Averaging (`Scenarios/Averaging.cs`)

**Intent.** When several comparable sources quote the same pair, the mean smooths their small
discrepancies — but the result is *synthetic*: it may equal a rate no source ever published, so
it is deliberately labelled and explicitly not for audit-grade conversions.

**What it does.** Prints each bank's AUD/USD fix, then resolves the same pair through an
aggregator configured with `DefaultStrategy = new AverageStrategy()`, and finally resolves
AUD/JPY (a single-contributor pair) to show averaging degrades gracefully.

**What to expect.**

```
BankA fix : 0.6568
BankB fix : 0.6533
Averaged  : 0.65505  provider label "Average" (synthetic - not for audit)
AUD/JPY   : 102.02  (single contributor)
```

0.65505 is the arithmetic mean of the two fixes, and its provider label is the synthetic
`"Average"` — the deliberate marker that this number traces to a computation, not a publication.
A pair only one bank quotes still resolves: the average of one contribution is that value.

**APIs demonstrated.** `AverageStrategy`, `RateAggregationOptions.DefaultStrategy`, the
synthetic provider label in provenance.

### PerPairRouting (`Scenarios/PerPairRouting.cs`)

**Intent.** Different pairs have different authoritative sources. Per-pair routes give each pair
its own provider order — and optionally its own strategy — while unrouted pairs keep the
aggregator's defaults.

**What it does.** Adds two routes to `RateAggregationOptions.Routes`: AUD/USD prefers BankB
(overriding the default child order), and AUD/EUR routes to BankA with an explicitly spelled-out
`PriorityFallbackStrategy` (showing the per-route strategy seam). AUD/JPY gets no route and
follows the default order.

**What to expect.**

```
AUD/USD: 0.6533  served by BankB  (routed to BankB first)
AUD/EUR: 0.6028  served by BankA  (routed to BankA)
AUD/JPY: 102.02  served by BankB  (no route, default order)
```

Compare the first line with PriorityFallback's: same aggregator children, same date, but the
route flipped AUD/USD to BankB's 0.6533. Routing is per-pair configuration, not code.

**APIs demonstrated.** `CurrencyPairRoute(providerOrder[, strategy])`,
`RateAggregationOptions.Routes`, `PriorityFallbackStrategy.Instance`.

### DiComposition (`Scenarios/DiComposition.cs`)

**Intent.** The whole stack — each child wrapped in its own read-through cache, grouped behind
one `IDatedRateProvider` registration, with routes declared fluently — composed in a service
collection the way a real host would, with each child also reachable by name.

**What it does.** Calls `AddFinancialService().AddAggregatedRateProvider(agg => …)` with two
factory-based children (each given an `InMemoryRateCache` via the cache-factory seam), a default
strategy, a BankB-first route for AUD/USD, and an averaging route for AUD/EUR. Then resolves the
aggregate as `IDatedRateProvider` and one child directly as a keyed service.

**What to expect.**

```
AUD/USD via aggregate : 0.6533  served by BankB
AUD/USD via "BankA"   : 0.6568  served by BankA (keyed child)
```

The aggregate obeys the `MapPair` route (BankB first); the keyed lookup bypasses routing
entirely and hits BankA's cached child directly — the escape hatch for "I need *this* source's
number". With live providers, swap the factory children for
`AddCachedChild<EcbRateProvider>("ECB")` after the provider package's `Add…ExchangeRates()`
registration (see the commented block in `Program.cs`).

**APIs demonstrated.** `AddFinancialService()`, `AddAggregatedRateProvider(builder => …)`,
`IAggregatedRateBuilder.AddCachedChild(name, factory, cacheFactory)` / `UseDefaultStrategy` /
`MapPair(pair, params order)` / `MapPair(pair, strategy, params order)`,
`GetRequiredKeyedService<IDatedRateProvider>(name)`.

## Data

`Data/central-bank-a.csv` (AUD/USD, AUD/EUR) and `Data/central-bank-b.csv` (AUD/USD, AUD/JPY)
hold illustrative Q1 2024 business-day rates (synthetic; see the file headers). BankB's USD fix
is deliberately offset from BankA's so strategy choices are visible.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.ExchangeRates.Caching
dotnet add package Bodu.Financial.DependencyInjection
```
