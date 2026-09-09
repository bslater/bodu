---
title: Writing your own web provider
---

# Writing your own web provider

The eleven built-in providers are thin: each one supplies a feed-specific fetch and parse, and
inherits everything else — the immutable snapshot, the full synchronous and asynchronous lookup
matrix, inverse fallback, request coalescing, `HttpClient` ownership, history advertisement, and
the dependency-injection registration — from two base classes in `Bodu.Financial.ExchangeRates`.
A provider for a feed Bodu does not ship is the same amount of code. This page builds one,
`AcmeRateProvider`, over a fictional CSV endpoint, registers it with the same `Financial:Acme`
shape the built-in providers use, and proves it with the shipped contract tests. Every sample
runs offline against a <xref:Bodu.Financial.ExchangeRates.Testing.StubHttpMessageHandler>.

Two bases, one choice:

| Base | Derive when the feed… | You implement | The base owns |
|---|---|---|---|
| <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> | returns **one currency pair per request** (Yahoo, OFX, Fixer, FRED, …) | an `IPairRateSource<TSeries>` (fetch + parse) and `ProviderId` | per-pair coverage tracking, single-flight coalescing per pair-and-window, series discovery, logging |
| <xref:Bodu.Financial.ExchangeRates.WebRateProvider> | returns **many pairs per download** — a whole file, an era, a month (ECB, RBA, BoE, IMF) | `EnsureLoadedAsync`, `IsLoaded`, `ProviderId`, `AllowSynchronousNetworkAccess`, `DefaultLookback` | the accumulator, the snapshot, the lookup matrix, coalescing through `LoadCoalescedAsync` |

`PairWebRateProvider<TSeries>` itself derives from `WebRateProvider`, so both shapes end up with
the same public surface: `IDatedRateProvider`, `IRateProvider`,
<xref:Bodu.Financial.ExchangeRates.IPairRateLoader>, and
<xref:Bodu.Financial.ExchangeRates.IHistoricalRateProvider>.

## Pattern 1 — the options type

Derive <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions>. The base carries the shared
surface (`BaseAddress`, `HttpTimeout`, `UserAgent`, `AllowSynchronousNetworkAccess`,
`DefaultLookback`, `HistoryAvailability`, `CurrencyAliases`, the `*LogLevel` knobs) and its
validation; your constructor sets the host and the history depth, and `TryValidateCore`
guards whatever you add. `MapCurrency` applies the alias map when building a request.

```csharp
using System.Globalization;
using Bodu.Financial.ExchangeRates;

public sealed class AcmeRateProviderOptions : WebRateProviderOptions
{
    public AcmeRateProviderOptions()
    {
        BaseAddress = new Uri("https://rates.acme.example/");
        HistoryAvailability = RateHistoryAvailability.RollingDays(365);
    }

    /// <summary>The relative request path; {from}, {to}, {start}, and {end} are substituted per request.</summary>
    public string HistoryPath { get; set; } = "v1/history/{from}{to}.csv?start={start}&end={end}";

    /// <summary>The <c>Authorization: Bearer</c> token the feed expects; blank sends no header.</summary>
    public string ApiToken { get; set; } = string.Empty;

    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(HistoryPath)
            || !HistoryPath.Contains("{from}", StringComparison.Ordinal)
            || !HistoryPath.Contains("{to}", StringComparison.Ordinal))
        {
            error = "HistoryPath must contain the {from} and {to} placeholders.";
            return false;
        }

        error = null;
        return true;
    }

    internal Uri BuildRequestUri(CurrencyPairRequest request)
    {
        string path = HistoryPath
            .Replace("{from}", MapCurrency(request.Pair.From.ToString()), StringComparison.Ordinal)
            .Replace("{to}", MapCurrency(request.Pair.To.ToString()), StringComparison.Ordinal)
            .Replace("{start}", request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{end}", request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        return new Uri(BaseAddress, path);
    }
}
```

Declare the history depth deliberately, even when it is `Unbounded`: the caching and
aggregation layers consult it to skip or clamp doomed fetches, and the pair contract test
fails when a provider forgets to set it. `RateHistoryAvailability.Since(date)` describes a
fixed floor, `RollingDays(n)` a window that moves with the clock.

## Pattern 2 — the series-info type

`GetAvailablePairs()` on a pair provider returns one `TSeries` per fetched pair. Keep it a small
immutable class exposing at least the `CurrencyPair`, plus whatever the feed reports (a ticker,
a series identifier, the quote symbol) that a caller might want to log or display:

```csharp
using Bodu.Financial.ExchangeRates;

/// <summary>Series metadata surfaced through <c>GetAvailablePairs()</c> once a pair has been fetched.</summary>
public sealed class AcmeSeriesInfo
{
    internal AcmeSeriesInfo(CurrencyPair pair, string symbol)
    {
        Pair = pair;
        Symbol = symbol;
    }

    public CurrencyPair Pair { get; }

    public string Symbol { get; }
}
```

## Pattern 3 — the source: fetch and parse

<xref:Bodu.Financial.ExchangeRates.IPairRateSource`1> is the seam between the provider and the
network. It receives a <xref:Bodu.Financial.ExchangeRates.CurrencyPairRequest> (pair plus
inclusive window) and returns a <xref:Bodu.Financial.ExchangeRates.PairRateData`1>: the
resolved pair, the observations **already restricted to the window**, and the series metadata.
The provider stamps its own `ProviderId` on every observation, so the source never sees a
provider name.

Two contracts matter inside it. A payload the parser cannot read is reported as
<xref:Bodu.Financial.ExchangeRates.ExchangeRateFormatException> — the shared
`FormatException` subtype every provider raises — so callers, the resilience pipeline (which
never retries it), and the pair base's failure logging all recognize it. A transport failure is
left to `HttpClient`: `EnsureSuccessStatusCode` (or `GetByteArrayAsync`) raises
`HttpRequestException`, which the base logs at `DownloadFailedLogLevel` and rethrows.

```csharp
using System.Globalization;
using System.Net.Http.Headers;
using Bodu.Financial.ExchangeRates;

/// <summary>Fetches one pair's CSV (<c>date,rate</c> rows) and parses it into observations.</summary>
internal sealed class AcmeRateSource : IPairRateSource<AcmeSeriesInfo>
{
    private readonly HttpClient _httpClient;
    private readonly AcmeRateProviderOptions _options;

    internal AcmeRateSource(HttpClient httpClient, AcmeRateProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    public async ValueTask<PairRateData<AcmeSeriesInfo>> GetPairAsync(
        CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, _options.BuildRequestUri(request));
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        // A non-success status surfaces as HttpRequestException; on the DI path the resilience
        // pipeline has already retried before it reaches here.
        using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string csv = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new PairRateData<AcmeSeriesInfo>(
            request.Pair,
            ParseCsv(csv, request),
            new AcmeSeriesInfo(request.Pair, $"{request.Pair.From}{request.Pair.To}"));
    }

    /// <summary>Parses <c>date,rate</c> rows, keeping only those inside the requested window.</summary>
    private static IReadOnlyList<RateObservation> ParseCsv(string csv, CurrencyPairRequest request)
    {
        var observations = new List<RateObservation>();
        string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0 || !lines[0].Equals("date,rate", StringComparison.OrdinalIgnoreCase))
            throw new ExchangeRateFormatException("The Acme feed did not start with the expected 'date,rate' header.");

        foreach (string line in lines.Skip(1))
        {
            string[] cells = line.Split(',');
            if (cells.Length != 2
                || !DateOnly.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                || !decimal.TryParse(cells[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate)
                || rate <= 0m)
            {
                throw new ExchangeRateFormatException($"The Acme feed row '{line}' is not a valid 'date,rate' observation.");
            }

            if (date >= request.StartDate && date <= request.EndDate)
                observations.Add(new RateObservation(date, rate));
        }

        observations.Sort((a, b) => a.Date.CompareTo(b.Date));
        return observations;
    }
}
```

> [!TIP]
> Parse with `CultureInfo.InvariantCulture` throughout — a feed's dates and decimals are wire
> formats, not user-facing text — and reject non-positive rates: the snapshot reciprocates a
> rate for inverse lookups, and a zero would surface far from the row that caused it.

## Pattern 4 — the provider

The provider contributes the identity and the constructors. Follow the shipped convention of
three: options-only (builds and **owns** an `HttpClient` through
<xref:Bodu.Financial.ExchangeRates.RateProviderHttpClientFactory>), `HttpClient` + options (the
caller's client, never disposed — the DI shape and the test shape), and source + options (the
seam a file-backed source plugs into). The `PairWebRateProvider<TSeries>` constructor takes the
source, the options, an optional logger, the owned client or `null`, and an optional
`TimeProvider`; it validates the options for you.

```csharp
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.Logging;

/// <summary>A per-pair web provider over the Acme CSV feed.</summary>
public sealed class AcmeRateProvider : PairWebRateProvider<AcmeSeriesInfo>
{
    public const string ProviderName = "Acme";

    // Options only: the provider builds and owns its HttpClient (no retry — see the DI registration).
    public AcmeRateProvider(AcmeRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    // Caller-supplied client: the shape the DI registration (and a StubHttpMessageHandler test) uses.
    public AcmeRateProvider(HttpClient httpClient, AcmeRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : base(new AcmeRateSource(httpClient, options), options, logger, ownedHttpClient: null, timeProvider)
    {
    }

    // Caller-supplied source: the seam a file-backed IPairRateSource<AcmeSeriesInfo> plugs into.
    public AcmeRateProvider(IPairRateSource<AcmeSeriesInfo> source, AcmeRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : base(source, options, logger, ownedHttpClient: null, timeProvider)
    {
    }

    private AcmeRateProvider(AcmeRateProviderOptions options, HttpClient ownedClient, ILogger? logger, TimeProvider? timeProvider)
        : base(new AcmeRateSource(ownedClient, options), options, logger, ownedClient, timeProvider)
    {
    }

    protected override string ProviderId => ProviderName;

    private static HttpClient CreateOwnedClient(AcmeRateProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout, options.MaxResponseContentBufferSize);
    }
}
```

That is the whole provider. `AllowSynchronousNetworkAccess`, `DefaultLookback`, and
`HistoryAvailability` are forwarded from the options by the pair base (which seals the first
two); `FormatPairForLog` is the one further virtual worth overriding when the feed has a
better label than `FROM/TO` — a ticker, say — for its download log lines. Exercised against a
stub, the inherited machinery is visible end to end:

```csharp
using System.Text;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

const string AcmeCsv = "date,rate\n2024-01-15,0.6660\n2024-01-16,0.6660\n2024-01-17,0.6663\n2024-01-18,0.6670\n2024-01-19,0.6681\n";

var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes(AcmeCsv));
var options = new AcmeRateProviderOptions { ApiToken = "secret-token" };

using var acme = new AcmeRateProvider(new HttpClient(handler), options);
await acme.LoadPairAsync("AUD", "USD", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 19));

RateLookupResult usd = acme.GetRate("AUD", "USD", new DateOnly(2024, 1, 17));
Console.WriteLine($"{usd.Rate.Rate} from {usd.Rate.Provider}");       // 0.6663 from Acme
Console.WriteLine(handler.LastRequestUri);                            // https://rates.acme.example/v1/history/AUDUSD.csv?start=2024-01-15&end=2024-01-19
Console.WriteLine(handler.LastAuthorization);                         // Bearer secret-token

foreach (AcmeSeriesInfo series in acme.GetAvailablePairs())
    Console.WriteLine($"{series.Pair.From}/{series.Pair.To} ({series.Symbol})");   // AUD/USD (AUDUSD)
```

A malformed body surfaces as the format exception from Pattern 3, at the call that triggered
the fetch:

```csharp
var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes("<html>maintenance</html>"));
using var acme = new AcmeRateProvider(new HttpClient(handler), new AcmeRateProviderOptions());

try
{
    await acme.LoadPairAsync("AUD", "USD", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 19));
}
catch (ExchangeRateFormatException ex)
{
    Console.WriteLine(ex.Message);   // The Acme feed did not start with the expected 'date,rate' header.
}
```

## Pattern 5 — the bulk shape: deriving `WebRateProvider` directly

When one download covers many pairs, derive <xref:Bodu.Financial.ExchangeRates.WebRateProvider>
directly. The contract is five abstract members plus a handful of protected helpers:

| Member | Kind | What it does |
|---|---|---|
| `ProviderId` | abstract property | The name stamped on every `ExchangeRate` the provider produces. |
| `AllowSynchronousNetworkAccess` | abstract property | Whether a synchronous miss may block to fetch (see [the configuration page](provider-configuration.md#two-settings-worth-a-second-look)). |
| `DefaultLookback` | abstract property | The window a single-date on-demand fetch spans, ending on the requested date. |
| `EnsureLoadedAsync(pair, start, end, ct)` | abstract method | Fetch and accumulate whatever unit covers the window, idempotently. A feed that ignores the pair may ignore it. |
| `IsLoaded(pair, start, end)` | abstract method | Whether the window is already covered, so the synchronous path can skip a blocking fetch. |
| `HistoryAvailability` | virtual property | `Unbounded` unless overridden. |
| `ValidateRangeRequest(...)` | virtual method | Reject pairs the feed cannot carry — throw <xref:Bodu.Financial.ExchangeRates.RateSeriesNotFoundException> — before any download. |
| `CreateRangeInvertedException` / `FormatRateNotFound` | virtual | Feed-specific exception types and messages. |
| `OnObservationIngested` / `OnSynchronousNetworkFetch` | virtual | Diagnostics hooks; the pair base uses them for its logging. |
| `SyncRoot` | protected property | Hold it while accumulating so a fetch publishes atomically. |
| `AddObservations(rates, fetchedAtUtc)` then `RebuildSnapshot()` | protected methods | Upsert a batch under `ProviderId` and swap in the new immutable snapshot — both under `SyncRoot`. |
| `LoadCoalescedAsync(key, load, ct)` | protected method | Run a load once per key; concurrent callers with the same key share the in-flight fetch. |
| `TimeProvider` | protected property | The clock, for the `fetchedAtUtc` stamp. |
| `Dispose(bool)` | virtual | Extend to release resources; the base disposes an owned client. |

A minimal feed provider over a CSV of `date,from,to,rate` rows:

```csharp
using System.Globalization;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

/// <summary>
/// A bulk-shaped provider: one download (a CSV of <c>date,from,to,rate</c> rows) covers every pair, so the
/// feed is fetched once and every window is answered from the accumulated snapshot.
/// </summary>
public sealed class AcmeFeedRateProvider : WebRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly Uri _feedUri;
    private readonly bool _allowSynchronousNetworkAccess;
    private bool _loaded;   // guarded by SyncRoot

    public AcmeFeedRateProvider(HttpClient httpClient, Uri feedUri, bool allowSynchronousNetworkAccess = false, TimeProvider? timeProvider = null)
        : base(ownedHttpClient: null, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(feedUri);

        _httpClient = httpClient;
        _feedUri = feedUri;
        _allowSynchronousNetworkAccess = allowSynchronousNetworkAccess;
    }

    protected override string ProviderId => "AcmeFeed";

    protected override bool AllowSynchronousNetworkAccess => _allowSynchronousNetworkAccess;

    protected override TimeSpan DefaultLookback => TimeSpan.FromDays(7);

    public override RateHistoryAvailability HistoryAvailability => RateHistoryAvailability.Since(new DateOnly(2024, 1, 1));

    // Warm-up entry point shaped to the feed: the whole file, regardless of window.
    public Task LoadFeedAsync(CancellationToken cancellationToken = default) =>
        EnsureLoadedAsync(default, DateOnly.MinValue, DateOnly.MaxValue, cancellationToken).AsTask();

    protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return _loaded;
        }
    }

    protected override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        if (IsLoaded(pair, startDate, endDate))
            return ValueTask.CompletedTask;

        // One key for the whole feed: concurrent callers share a single download.
        return new ValueTask(LoadCoalescedAsync("feed", LoadFeedCoreAsync, cancellationToken));
    }

    private async Task LoadFeedCoreAsync(CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (_loaded)
                return;
        }

        string csv = await _httpClient.GetStringAsync(_feedUri, cancellationToken).ConfigureAwait(false);
        List<ExchangeRate> rates = ParseFeed(csv);
        DateTimeOffset fetchedAt = TimeProvider.GetUtcNow();

        lock (SyncRoot)
        {
            AddObservations(rates, fetchedAt);
            RebuildSnapshot();
            _loaded = true;
        }
    }

    private List<ExchangeRate> ParseFeed(string csv)
    {
        var rates = new List<ExchangeRate>();
        foreach (string line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1))
        {
            string[] cells = line.Split(',');
            if (cells.Length != 4
                || !DateOnly.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                || !CurrencyInfo.TryGetCurrencyCode(cells[1], out CurrencyCode from)
                || !CurrencyInfo.TryGetCurrencyCode(cells[2], out CurrencyCode to)
                || !decimal.TryParse(cells[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate))
            {
                throw new ExchangeRateFormatException($"The Acme feed row '{line}' is not a valid 'date,from,to,rate' observation.");
            }

            rates.Add(new ExchangeRate(from, to, date, rate, ProviderId));
        }

        return rates;
    }
}
```

The sequencing inside `LoadFeedCoreAsync` is the pattern every bulk provider follows: check
coverage under `SyncRoot`, download **outside** the lock, then accumulate and publish under
the lock. Readers never wait for a download — they read the previous snapshot until
`RebuildSnapshot` swaps in the new one.

```csharp
const string AcmeFeedCsv = "date,from,to,rate\n2024-01-15,AUD,USD,0.6660\n2024-01-15,AUD,EUR,0.6082\n2024-01-16,AUD,USD,0.6660\n2024-01-16,AUD,EUR,0.6120\n";

var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes(AcmeFeedCsv));
using var feed = new AcmeFeedRateProvider(new HttpClient(handler), new Uri("https://rates.acme.example/v1/feed.csv"));

await feed.LoadFeedAsync();

Console.WriteLine(feed.GetRate("AUD", "EUR", new DateOnly(2024, 1, 16)).Rate.Rate);   // 0.6120
Console.WriteLine(feed.GetRate("EUR", "AUD", new DateOnly(2024, 1, 16)).Rate.IsInverted); // True — inverse fallback
Console.WriteLine(feed.GetLoadedPairs().Count);                                        // 2
Console.WriteLine(handler.RequestCount);                                               // 1
```

A real bulk provider would additionally track *which* windows or units are loaded (a
<xref:Bodu.Financial.ExchangeRates.DateRangeCoverage> per pair, an era set, a month set),
override `ValidateRangeRequest` for a single-base feed, and keep a payload cache — the shipped
ECB, RBA, BoE, and IMF sources are the worked references.

## Pattern 6 — registering with `AddWebRateProvider`

<xref:Bodu.Financial.ExchangeRates.WebRateProviderExtensions.AddWebRateProvider*> in the
`Bodu.Financial.ExchangeRates.DependencyInjection` package is the machinery every
`Add<Source>ExchangeRates` delegates to. Its short overload — for an options type derived from
`WebRateProviderOptions` — binds the section, wires `TryValidate` into `ValidateOnStart`,
registers a named `HttpClient` with the standard resilience handler (user agent and per-attempt
timeout from the options), and registers the provider singleton as `IDatedRateProvider` and
`IRateProvider`. Your package's extension method supplies only what differs:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

public static class AcmeServiceCollectionExtensions
{
    public const string HttpClientName = "Acme.Rates";

    public static IFinancialServiceBuilder AddAcmeRates(
        this IFinancialServiceBuilder builder,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Acme",
        Action<AcmeRateProviderOptions>? configure = null,
        Action<HttpStandardResilienceOptions>? configureResilience = null) =>
        builder.AddWebRateProvider<AcmeRateProvider, AcmeRateProviderOptions>(
            HttpClientName,
            configuration,
            sectionName,
            "Acme exchange-rate options are invalid.",
            configure,
            configureResilience,
            static (client, options, loggerFactory, timeProvider) =>
                new AcmeRateProvider(client, options, loggerFactory?.CreateLogger<AcmeRateProvider>(), timeProvider));
}
```

The full overload takes the same parameters plus explicit `validateOptions`, `getUserAgent`,
and `getHttpTimeout` selectors, for an options type that does not derive from
`WebRateProviderOptions` (the way BoE's nested-endpoint options are registered). Either way the
consumer experience matches the built-in providers exactly — section, validation, resilience
hook, and all:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddAcmeRates(
        builder.Configuration,                                            // binds Financial:Acme
        configure: options => options.ApiToken = builder.Configuration["Acme:Token"] ?? string.Empty,
        configureResilience: resilience => resilience.Retry.MaxRetryAttempts = 2);
```

```json
{
  "Financial": {
    "Acme": {
      "BaseAddress": "https://rates.acme.example/",
      "HttpTimeout": "00:00:10",
      "HistoryPath": "v1/history/{from}{to}.csv?start={start}&end={end}",
      "ApiToken": "",
      "CurrencyAliases": { "CNH": "CNY" }
    }
  }
}
```

```csharp
IDatedRateProvider rates = host.Services.GetRequiredService<IDatedRateProvider>();   // the AcmeRateProvider singleton
RateLookupResult usd = await rates.GetRateAsync("AUD", "USD", new DateOnly(2024, 1, 17));
```

From here the provider is indistinguishable from a shipped one: wrap it with
`AddCachedRateProvider<AcmeRateProvider>("Acme", …)`, make it an aggregation child, or warm it at
startup — see [Configuring rate caching from appsettings](caching-configuration.md).

## Pattern 7 — proving it with the contract tests

Derive `PairWebRateProviderContractTests<AcmeRateProvider, AcmeSeriesInfo>` from the
in-repository `Bodu.Financial.ExchangeRates.Testing` project (a bulk-shaped provider derives
`DatedRateProviderContractTests<TProvider>` instead), point `CreateProvider` at a stub, and the
base sweeps the full lookup surface plus the pair warm-up lifecycle:

```csharp
using System.Text;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

[TestClass]
public sealed class AcmeRateProviderContractTests
    : PairWebRateProviderContractTests<AcmeRateProvider, AcmeSeriesInfo>
{
    // Every request, whatever its URL, is answered with these five business days.
    private const string Csv = "date,rate\n2024-01-15,0.6660\n2024-01-16,0.6660\n2024-01-17,0.6663\n2024-01-18,0.6670\n2024-01-19,0.6681\n";

    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    protected override DateOnly KnownDate => new(2024, 1, 17);

    protected override DateOnly UnknownDate => new(2024, 1, 20);   // a Saturday: no row in the CSV

    protected override DateOnly RangeStart => new(2024, 1, 15);

    protected override DateOnly RangeEnd => new(2024, 1, 19);

    protected override RateHistoryAvailability ExpectedHistoryAvailability => RateHistoryAvailability.RollingDays(365);

    protected override bool SupportsDisposalGuard => true;

    protected override AcmeRateProvider CreateProvider()
    {
        var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes(Csv));
        var options = new AcmeRateProviderOptions { AllowSynchronousNetworkAccess = true };
        return new AcmeRateProvider(new HttpClient(handler), options);
    }
}
```

[Testing your own provider](testing-providers.md) lists every seam on the two bases and shows
the file-backed `IPairRateSource<TSeries>` that the third `AcmeRateProvider` constructor exists
for.

## API summary

| Member | Description |
|---|---|
| <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions> | Base for your options: shared keys, `TryValidate` / `Validate`, `TryValidateCore` hook, `MapCurrency`. |
| <xref:Bodu.Financial.ExchangeRates.IPairRateSource`1> | `GetPairAsync(CurrencyPairRequest, CancellationToken)` → `PairRateData<TSeries>`; the fetch-and-parse seam. |
| <xref:Bodu.Financial.ExchangeRates.PairRateData`1> | `(Pair, Observations, Series)` — window-restricted `RateObservation`s plus the series metadata. |
| <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> | Per-pair coverage, coalescing, discovery, and logging over a source; you supply `ProviderId`. |
| <xref:Bodu.Financial.ExchangeRates.WebRateProvider> | The accumulator, snapshot, and lookup matrix; you supply the fetch, coverage check, and identity. |
| <xref:Bodu.Financial.ExchangeRates.ExchangeRateFormatException> | Throw for any payload the parser rejects. |
| <xref:Bodu.Financial.ExchangeRates.RateSeriesNotFoundException> | Throw from `ValidateRangeRequest` for a pair the feed structurally cannot carry. |
| <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability> | `Unbounded`, `Since(date)`, `RollingDays(n)` — declare it in the options constructor. |
| <xref:Bodu.Financial.ExchangeRates.RateProviderHttpClientFactory> | Builds the owned client for the options-only constructor. |
| <xref:Bodu.Financial.ExchangeRates.WebRateProviderExtensions.AddWebRateProvider*> | The DI registration your `AddAcmeRates` delegates to. |

## Where to go next

- [Testing your own provider](testing-providers.md) — the stub, the file-backed source, and both contract bases in detail.
- [Configuring providers from appsettings](provider-configuration.md) — the section shape and resilience pipeline your registration inherits.
- [Built-in exchange-rate providers](exchange-rate-providers.md) — the eleven worked references, and their failure modes.
- [Working with exchange rates](exchange-rates.md) — the contracts and provenance model your provider serves.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic.
