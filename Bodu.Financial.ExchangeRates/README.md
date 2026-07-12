# Bodu.Financial.ExchangeRates

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

The web exchange-rate provider infrastructure for the [Bodu.Financial](../Bodu.Financial)
FX stack. It hosts the abstract `WebRateProvider` and `PairWebRateProvider<TSeries>` base
classes that every per-source provider package — BoE, ECB, RBA, Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED, IMF —
builds on, plus the shared fetch machinery they have in common. Factoring this layer out
keeps the core `Bodu.Financial` package free of HTTP machinery (and of any logging
dependency); reference this package only when you consume one of the web providers or
write your own.

All types live in the same flattened `Bodu.Financial.ExchangeRates` namespace as the core
FX types (`ExchangeRate`, `IRateProvider` / `IDatedRateProvider`, `RateSeries`, …) shipped
by `Bodu.Financial`, so a single `using Bodu.Financial.ExchangeRates;` covers both.

```csharp
using Bodu.Financial.ExchangeRates;

// A minimal pair-serving provider over the shared base: supply a source that
// fetches one currency pair's observations for a date range, and the base adds
// accumulation, snapshotting, lookup resolution, and request coalescing.
public sealed class AcmeRateProvider : PairWebRateProvider<AcmeSeriesInfo>
{
    public AcmeRateProvider(AcmeRateProviderOptions options)
        : base(new AcmeSource(options), options, logger: null, ownedHttpClient: null, timeProvider: null) { }
}
```

## What the base classes provide

- **`WebRateProvider`** — the abstract HTTP-backed dated-provider base. It accumulates
  fetched observations into an immutable book / snapshot, serves the full
  `IDatedRateProvider` / `IRateProvider` lookup matrix over that snapshot, coalesces
  concurrent loads of the same window, and either builds and owns its `HttpClient` from
  the options or borrows a caller-supplied one. Each provider advertises how far back it
  serves rates through `HistoryAvailability` (a `RateHistoryAvailability` — unbounded, a
  fixed earliest date, or a rolling window).
- **`PairWebRateProvider<TSeries>`** — the specialisation for sources that fetch a
  distinct series per currency pair (the shape of the Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED, and IMF feeds),
  driven by an `IPairRateSource<TSeries>`.
- **`WebRateProviderOptions`** — the abstract options base carrying `BaseAddress`,
  `HttpTimeout`, `UserAgent`, `DefaultLookback`, `CurrencyAliases`, and the per-stage
  log levels every derived options type inherits.

## Shared fetch machinery

- **`IPairRateLoader` / `IPairRateSource<TSeries>`**, **`CurrencyPairRequest`** (from the
  core package), and **`PairRateData<TSeries>`** — the pair-based fetch contract
  (`GetPairAsync`), the request struct (pair + inclusive date range), and the result
  record (pair, observations, source-specific series metadata).
- **`SingleFlightCoordinator<TKey>`** — keyed single-flight coordination that coalesces
  concurrent loads of the same key onto one in-flight operation (`RunAsync` /
  `RunAsync<TResult>`), used by the base to deduplicate endpoint fetches.
- **`FileSystemByteCache<TKey>`** — the abstract base for the file-feed providers'
  on-disk raw-response caches (best-effort `TryGetCore` / `StoreCore` keyed by a
  download unit); a derived cache supplies only the file name and, optionally, a
  freshness rule.
- **`RateProviderHttpClientFactory`** — builds the owned `HttpClient` (user agent,
  timeout) for the options-only constructor form.
- **`ExchangeRateFormatException`** — the `FormatException` a provider raises when a
  feed's payload cannot be parsed.

## HTTP client and lifetime

Providers built on the base are `IDisposable` and offer two construction styles:

- `new XProvider(options, ...)` — the provider builds, owns, and disposes its own
  `HttpClient`, created via `RateProviderHttpClientFactory.Create` from the configured
  user agent and timeout. Dispose the provider (for example with `using`) to release it.
- `new XProvider(httpClient, options, ...)` — you supply the client and own its lifetime;
  the provider never disposes a client it did not create. This is the form the
  [`Bodu.Financial.ExchangeRates.DependencyInjection`](../Bodu.Financial.ExchangeRates.DependencyInjection)
  package uses, backed by `IHttpClientFactory`; its generic
  `AddWebRateProvider<TProvider, TOptions>` registration handles options binding and a
  named, resilient `HttpClient` for every provider package.

## Logging

The base logs through `Microsoft.Extensions.Logging.Abstractions` — the package's only
non-Bodu dependency. Pass an `ILogger` to the constructor, or let the DI package wire one
for you. When no logger is supplied it defaults to `NullLogger.Instance`, so logging is
entirely opt-in and free when unused. Download-starting / download-completed /
observation-ingested / download-failed levels are individually configurable on
`WebRateProviderOptions`.

## Dependencies

Depends on `Bodu.Financial`, `Bodu.Core`, and `Microsoft.Extensions.Logging.Abstractions`.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
