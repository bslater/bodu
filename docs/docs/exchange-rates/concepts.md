---
title: Bodu.Financial.ExchangeRates — Core concepts
---

# Bodu.Financial.ExchangeRates — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [providers guide](../../guides/financial/exchange-rate-providers.md), and refer back whenever a term feels imprecise.

Part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic. For the high-level shape of the package, start with the [introduction](index.md); the FX vocabulary shared with the core package — provider contracts, resolution policies, provenance — lives in the [Bodu.Financial concepts](../financial/concepts.md) page.

## Warm, then look up

A web provider is a **materializer**: it downloads a feed, upserts every observation into an accumulator, and rebuilds an immutable snapshot (a <xref:Bodu.Financial.ExchangeRates.RateBook> wrapped in a <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>). Every lookup is answered from that snapshot, so a lookup never touches the network unless the snapshot cannot answer it.

There are three ways to warm the snapshot:

| Surface | Available on | What it loads |
|---|---|---|
| Feed-specific warm-up (`LoadRangeAsync`, `PreloadAsync`, `LoadFeedAsync`, `LoadEraAsync`) | Each bulk provider, shaped to its feed | A date range, an era, or a whole feed for every quote currency. |
| `LoadPairAsync(from, to, start, end)` | Every provider, through <xref:Bodu.Financial.ExchangeRates.IPairRateLoader> | Whatever unit covers the requested pair and window — a single pair on a pair feed, the enclosing feed or era on a bulk feed. |
| The asynchronous getters (`GetRateAsync`, `GetRatesAsync`) | Every provider | On demand: a single-date lookup fetches the window `[date − DefaultLookback, date]`; a range read fetches the requested range. Already-covered windows are skipped. |

The synchronous getters (`GetRate`, `TryGetRate`, `GetRates`) resolve against the snapshot only. On a miss they return `false` or throw `KeyNotFoundException` — unless `AllowSynchronousNetworkAccess` is enabled (below).

## Bulk provider vs pair provider

<xref:Bodu.Financial.ExchangeRates.WebRateProvider> is the root base. A **bulk** provider (RBA, ECB, BoE, IMF) derives from it directly: the feed publishes one base currency against many quotes, so a load brings in every pair at once and the provider tracks coverage per feed, era, or window. Only direct (`base→X`) and inverse (`X→base`) pairs resolve; a cross pair such as `USD→JPY` on the ECB provider is rejected with <xref:Bodu.Financial.ExchangeRates.RateSeriesNotFoundException> before any download.

<xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> is the **pair** specialization (Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED). It implements coverage tracking with a gap-aware per-pair <xref:Bodu.Financial.ExchangeRates.DateRangeCoverage> set — a request that straddles an unfetched interior gap is treated as uncovered and fetched — and delegates the actual fetch-and-parse to an <xref:Bodu.Financial.ExchangeRates.IPairRateSource`1>. The `TSeries` type parameter is the feed-specific series metadata (`YahooSeriesInfo`, `FixerSeriesInfo`, …) that `GetAvailablePairs()` returns.

## `DefaultLookback`

When a single-date asynchronous lookup must fetch on demand, the provider fetches the window ending on the requested date and spanning `DefaultLookback` (7 days by default on <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions>), so a `PreviousWithin` resolution has observations to fall back to. Bulk providers override it: the ECB provider uses `TimeSpan.Zero` because a feed load already brings the surrounding dates.

## `RateRangeResult`

A range read returns a <xref:Bodu.Financial.ExchangeRates.RateRangeResult> rather than a bare list, so the caller can tell what was asked for from what was observed:

| Member | Meaning |
|---|---|
| `FromIsoCode`, `ToIsoCode` | The pair as requested. |
| `RequestedStartDate`, `RequestedEndDate` | The inclusive window as requested. |
| `Rates` | The observations found, in ascending date order — also exposed as `this[int]`, `Count`, and `IEnumerable<ExchangeRate>` (the result itself is an `IReadOnlyList<ExchangeRate>`). |
| `IsEmpty` | `true` when no observation fell inside the window. |
| `FirstObservedDate`, `LastObservedDate` | The observed span, or `null` when empty. |

A window that spans weekends or holidays yields fewer observations than days; an empty result is a normal outcome, not an exception.

## History availability

Every web provider implements <xref:Bodu.Financial.ExchangeRates.IHistoricalRateProvider> and advertises how far back it can serve rates through `HistoryAvailability`, a <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability> with three kinds:

| Factory | `Kind` | Meaning |
|---|---|---|
| `RateHistoryAvailability.Unbounded` | `Unbounded` | No declared floor (the base-class default). |
| `RateHistoryAvailability.RollingDays(n)` | `Rolling` | Only the last `n` days relative to the current date — OANDA's anonymous endpoint (~180 days), or the ECB provider when only rolling feeds are configured. |
| `RateHistoryAvailability.Since(date)` | `Since` | Data exists from a fixed epoch — Fixer's 1999-01-01, the ECB full-history feed's 1999-01-04. |

`GetEarliestAvailable(asOf)` and `IsAvailable(date, asOf)` let a caller — or the [caching layer](../../guides/financial/exchange-rate-caching.md#respecting-advertised-history) — avoid asking for dates the feed has declared unavailable. Pair providers read the value from `WebRateProviderOptions.HistoryAvailability`; bulk providers derive it from their feed configuration.

## Payload cache vs rate cache

Two caches with different jobs:

| | Payload cache | Rate cache |
|---|---|---|
| Lives in | This package (<xref:Bodu.Financial.ExchangeRates.IByteCache`1>, <xref:Bodu.Financial.ExchangeRates.FileSystemByteCache`1>) and the bulk provider packages | `Bodu.Financial.ExchangeRates.Caching` (<xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider>) |
| Stores | The **raw bytes** of a downloaded file, keyed by download unit (an ECB feed, an RBA era, an IMF report month) | **Parsed, resolved rates** plus the fetched-window coverage, per provider and pair |
| Purpose | Avoid re-downloading immutable history across process restarts | Serve repeated lookups without consulting the provider at all; share across processes and machines |
| Freshness | A file older than the options' `RefreshInterval` is re-downloaded | Per-provider expiry, jitter, and refresh-ahead |
| Configured by | `EnableDiskCache`, `CacheDirectory`, `RefreshInterval` on the bulk providers' options | `AddCachedRateProvider` and the `IRateCache` backends |

The payload cache is **best-effort**: an I/O failure while reading is a miss, a failure while writing is swallowed (and logged when a logger is supplied), so a cache problem never breaks rate retrieval. Pair providers keep no payload cache — each pair-and-window request is a distinct query — and rely on in-memory coverage and, optionally, the rate cache.

## Single-flight

<xref:Bodu.Financial.ExchangeRates.SingleFlightCoordinator`1> implements request coalescing: when several callers request the same key while a fetch is running, they join the running task instead of starting duplicate work. `WebRateProvider.LoadCoalescedAsync(key, load, cancellationToken)` wraps it for derived types, and `PairWebRateProvider<TSeries>` applies it per pair-and-window automatically.

Two properties matter to consumers. The in-flight entry is released as soon as the operation completes, **including on failure**, so a fault never poisons a key. And the shared operation runs under `CancellationToken.None`: a caller's token abandons only that caller's wait (it observes `OperationCanceledException`), never the fetch the other joiners are waiting on.

## `AllowSynchronousNetworkAccess`

Off by default. When enabled, the synchronous getters block to fetch a missing window on demand instead of reporting a miss. Blocking on asynchronous I/O from a thread with a captured `SynchronizationContext` (a UI thread, classic ASP.NET) can deadlock, so the provider converts that situation into an immediate `InvalidOperationException` rather than a hang. Enable it only for code that calls the synchronous getters from thread-pool threads, or prefer the asynchronous surface and an explicit warm-up.

## Resilience

A provider constructed from options alone builds and owns its `HttpClient` through <xref:Bodu.Financial.ExchangeRates.RateProviderHttpClientFactory>: the options' `UserAgent`, `HttpTimeout` (applied as `HttpClient.Timeout`), and `MaxResponseContentBufferSize` (a 64 MiB ceiling so a hostile endpoint cannot drive unbounded memory use). There is no retry.

A provider registered through `AddWebRateProvider` (every `Add<Source>ExchangeRates`) borrows a named `HttpClient` from `IHttpClientFactory` fitted with the **standard Polly resilience handler** — retry with exponential backoff and jitter, a per-attempt timeout driven from `HttpTimeout`, a total-request timeout of three times that value, and a circuit breaker. `HttpClient.Timeout` is set to infinite so the two mechanisms do not compete. Tune or effectively disable the pipeline through the `configureResilience` callback (`Action<HttpStandardResilienceOptions>`) on the registration.

## Failure modes

| Situation | Surfaces as | Where |
|---|---|---|
| A date the snapshot cannot resolve under the lookup options | `TryGetRate` returns `false`; `GetRate` / `GetRateAsync` throw `KeyNotFoundException` | Every provider |
| A pair a single-base feed does not quote (cross pair) | <xref:Bodu.Financial.ExchangeRates.RateSeriesNotFoundException> (a `KeyNotFoundException`) | Bulk providers, before any download |
| An inverted date range (`endDate < startDate`), a malformed ISO code | `ArgumentException` (`ArgumentNullException` for a null code) | Range and warm-up surfaces |
| Invalid options (`BaseAddress` null, non-positive timeout, missing API key, …) | `ArgumentException` from the constructor; with DI, an options-validation failure at startup (`ValidateOnStart`) | Construction / registration |
| A downloaded payload that cannot be parsed | <xref:Bodu.Financial.ExchangeRates.ExchangeRateFormatException> (a `FormatException`) | The fetch, propagated to the caller that triggered it |
| A transport failure (DNS, connection, non-success status) | `HttpRequestException` — on the DI path, after the resilience pipeline has exhausted its retries | The fetch |
| A per-request timeout on an owned client | `TaskCanceledException` (the `HttpClient` convention) | The fetch |
| A synchronous fetch attempted on a thread with a `SynchronizationContext` | `InvalidOperationException` | Synchronous getters with `AllowSynchronousNetworkAccess` enabled |
| Any member after `Dispose()` | `ObjectDisposedException` | Every provider |

Fetch failures are logged at `DownloadFailedLogLevel` (`Warning` by default) and rethrown; the pair base logs anything other than a transport, I/O, or format failure under a distinct error event so a bug is not mislabelled as a feed problem. Cancellation is never logged.

## Lifetimes and `HttpClient` ownership

A provider is designed to be **long-lived**: its value is the accumulated snapshot. The DI registration makes it a singleton and exposes the same instance as `IDatedRateProvider` and `IRateProvider` (idempotent `TryAdd` registrations, so the first registered provider wins each contract).

The options-only constructor **owns** the `HttpClient` it builds and disposes it with the provider. The constructor that accepts an `HttpClient` leaves the client's lifetime — and its user agent and timeout — to the caller; that is the shape `AddWebRateProvider` uses with `IHttpClientFactory`. Dispose an owned provider when it goes out of scope; after disposal every public member throws `ObjectDisposedException`, but snapshots already handed out through `GetLoadedBook()` / `GetLoadedSnapshot()` stay valid because they are immutable.

## Thread safety

A provider is safe for concurrent use. Lookups read a `volatile` reference to the current immutable snapshot, so they never block behind a fetch and never observe a partially applied load; fetches accumulate under the provider's `SyncRoot` and publish the new snapshot atomically. Concurrent loads of the same unit coalesce (above). `GetLoadedBook()` and `GetLoadedSnapshot()` return immutable instances pinned at call time — later fetches replace, never mutate, them — so they are safe to share across threads and to keep after the provider is disposed. `GetAvailablePairs()` returns a snapshot array.

## Snapshots and export

`GetLoadedBook()` returns the immutable <xref:Bodu.Financial.ExchangeRates.RateBook> of everything fetched so far; `GetLoadedSnapshot()` returns the same data as a ready-to-query <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>. Both are the composable export primitives — rewrap the book with a provider-priority policy, edit a copy through `RateBook.ToBuilder()`, or hand the snapshot to code that must never touch the network. The [providers guide](../../guides/financial/exchange-rate-providers.md#snapshotting-and-exporting-rates) shows the patterns.

## Logging

Logging is opt-in: pass an `ILogger` (or let DI supply one) or the provider uses `NullLogger`. Each options type exposes five per-event levels — `DownloadStartingLogLevel` (`Debug`), `DownloadCompletedLogLevel` (`Information`), `DownloadFailedLogLevel` (`Warning`), `ObservationIngestedLogLevel` (`Information`), and `SynchronousNetworkFetchLogLevel` (`Warning`) — so verbosity is tuned per concern without category-wide filters; `LogLevel.None` suppresses an event entirely.

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples for every concept above.
- **[Introduction](index.md)** — the high-level shape of the package and the provider family table.
- **[Exchange-rate lookups on a known dataset](../../guides/financial/exchange-rate-lookups.md)** — the six date-resolution policies and tolerance, which apply unchanged to web providers.
- **[Caching and aggregating exchange rates](../../guides/financial/exchange-rate-caching.md)** — the rate cache this page distinguishes from the payload cache.
- **[Bodu.Financial concepts](../financial/concepts.md)** — the FX vocabulary shared with the core package.
- **[Bodu.Financial.ExchangeRates API reference](xref:Bodu.Financial.ExchangeRates)** — full type-by-type docs.
