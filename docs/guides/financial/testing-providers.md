---
title: Testing your own provider
---

# Testing your own provider

Testing exchange-rate code splits into two problems. Code that *consumes* rates needs a
deterministic <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> it can inject; code that
*produces* rates — a provider you wrote, or a shipped provider whose wiring you want to prove —
needs to run hermetically against a canned payload and pass the same contract the built-in
providers pass. Everything on this page is offline: no sample touches the network.

Two pieces of infrastructure cover both sides. `Bodu.Financial` ships the fixed providers
(<xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>,
<xref:Bodu.Financial.ExchangeRates.FixedRateTable>). The repository project
`Bodu.Financial.ExchangeRates.Testing` holds the HTTP stub and the MSTest contract-test bases
the built-in providers themselves derive from.

> [!IMPORTANT]
> `Bodu.Financial.ExchangeRates.Testing` is an **in-repository test-infrastructure project**, not
> a NuGet package: its project file is marked `<IsPackable>false</IsPackable>` and `<IsTestProject>true</IsTestProject>`.
> It is available by project reference inside the repository (the shipped provider test projects
> and the [CustomProvider sample](../../samples/financial.md#bodufinancialsamplescustomprovider--test)
> reference it that way); a consumer outside the repository copies the three source files —
> `StubHttpMessageHandler`, `DatedRateProviderContractTests<TProvider>`, and
> `PairWebRateProviderContractTests<TProvider, TSeries>` — into their own test project. The
> `IsTestProject` flag pulls in MSTest through the repository's build targets, so a referencing
> test project needs nothing beyond its own MSTest reference.

```xml
<!-- A test project inside the repository. -->
<ItemGroup>
  <ProjectReference Include="..\..\Bodu.Financial\src\Bodu.Financial.csproj" />
  <ProjectReference Include="..\..\Bodu.Financial.ExchangeRates.Testing\src\Bodu.Financial.ExchangeRates.Testing.csproj" />
</ItemGroup>
```

## Pattern 1 — `FixedDatedRateProvider` is the recommended test double

Code that consumes <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> should be tested
against <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>, not a mock: it is the real
lookup engine over an in-memory <xref:Bodu.Financial.ExchangeRates.RateBook>, so date
resolution, inverse fallback, identity rates, and provenance behave exactly as they will in
production.

```csharp
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

var rates = new FixedDatedRateProvider(new[]
{
    new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 3, 15), 0.6580m, "Test"),
});

// Inject `rates` wherever the system under test wants an IDatedRateProvider.
```

For larger fixtures, build the book with
<xref:Bodu.Financial.ExchangeRates.RateTableBuilder> /
<xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder>, or load a committed CSV the way the
[OfflineRates sample](../../samples/financial.md#bodufinancialsamplesofflinerates) does.

## Pattern 2 — fixed providers in DI for application tests

Under dependency injection, register the fixed providers on the financial builder so a service
that depends on the contracts sees deterministic data.
<xref:Bodu.Financial.ExchangeRates.FixedRateTable> is the timeless counterpart: a
`(From, To) → rate` table that short-circuits same-currency lookups and falls back to the
reciprocal when only the reverse pair is present.

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFinancialService(financial => financial
    .AddDatedExchangeRateProvider(new FixedDatedRateProvider(new[]
    {
        new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 15), 0.6660m, "Test"),
        new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 16), 0.6660m, "Test"),
    }))
    .AddExchangeRateProvider(new FixedRateTable(new Dictionary<(string From, string To), decimal>
    {
        [("AUD", "USD")] = 0.6660m,
    })));

using ServiceProvider provider = services.BuildServiceProvider();
```

The provider registrations use `TryAdd` semantics — the first registration for each contract
wins — so register the fakes before the production wiring runs. To override a registration that
is already present, replace its descriptor. `FixedDatedRateProvider` implements only the dated
contract; pin it to a date with <xref:Bodu.Financial.ExchangeRates.DatedRateProviderAdapter>
to stand in for the timeless one as well:

```csharp
using Microsoft.Extensions.DependencyInjection.Extensions;

// The provider registrations use TryAdd, so a fake registered first wins; to override one that is
// already there, replace the descriptor instead.
services.Replace(ServiceDescriptor.Singleton<IDatedRateProvider>(fake));
services.Replace(ServiceDescriptor.Singleton<IRateProvider>(
    new DatedRateProviderAdapter(fake, new DateOnly(2024, 1, 15))));   // the timeless surface, pinned to one date
```

Avoid calling `UseCurrencyResolution` in unit tests — it mutates process-wide ambient state.
When a test must exercise a custom ambient lookup, prefer the flow-scoped
`CurrencyResolution.PushScoped(...)`, which restores the previous lookup on dispose (see the
[DI guide](dependency-injection.md#swapping-in-a-test-double)).

## Pattern 3 — `StubHttpMessageHandler` drives a real provider offline

<xref:Bodu.Financial.ExchangeRates.Testing.StubHttpMessageHandler> is an `HttpMessageHandler`
that answers **every** request with one canned body and status code, and records what it saw.
Hand it to an `HttpClient`, hand that client to a provider's `(HttpClient, options)`
constructor, and the whole download-parse-accumulate path runs with no network. Its surface:

| Member | Description |
|---|---|
| `StubHttpMessageHandler(byte[] content, HttpStatusCode statusCode = OK)` | The body and status returned for every request. |
| `RequestCount` | How many requests reached the handler — the assertion that proves coalescing and coverage tracking. |
| `LastRequestUri` | The URI of the most recent request — proves the options built the URL you expect. |
| `LastAuthorization` | The `Authorization` header of the most recent request, or `null` — proves a bearer token went on the wire. |

A bulk provider — the ECB, fed a two-day `eurofxref` document. `EnableDiskCache` is turned off so
the test writes nothing under the temporary path:

```csharp
using System.Text;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

const string EcbFeed = """
    <?xml version="1.0" encoding="UTF-8"?>
    <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
      <gesmes:subject>Reference rates</gesmes:subject>
      <gesmes:Sender><gesmes:name>European Central Bank</gesmes:name></gesmes:Sender>
      <Cube>
        <Cube time="2023-01-04">
          <Cube currency="USD" rate="1.0600"/>
          <Cube currency="JPY" rate="139.92"/>
        </Cube>
        <Cube time="2023-01-03">
          <Cube currency="USD" rate="1.0545"/>
          <Cube currency="JPY" rate="140.06"/>
        </Cube>
      </Cube>
    </gesmes:Envelope>
    """;

var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes(EcbFeed));   // 200 OK with this body, every request
var options = new EcbRateProviderOptions { EnableDiskCache = false };        // keep the test hermetic: no payload cache on disk

using var ecb = new EcbRateProvider(new HttpClient(handler), options);
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));

RateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
Console.WriteLine(usd.Rate.Rate);                       // 1.0545
Console.WriteLine(handler.RequestCount);                // 1 — one feed download covered the whole window
Console.WriteLine(handler.LastRequestUri);              // https://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.xml
```

The same recipe drives a pair provider. Fixer builds a time-series request for the window and
sends the key as a query parameter, which the recorded URI confirms:

```csharp
const string FixerTimeSeries = """
    {
      "success": true, "timeseries": true, "base": "EUR",
      "start_date": "2023-01-02", "end_date": "2023-01-04",
      "rates": {
        "2023-01-02": { "USD": 1.0668 },
        "2023-01-03": { "USD": 1.0546 },
        "2023-01-04": { "USD": 1.0602 }
      }
    }
    """;

var handler = new StubHttpMessageHandler(Encoding.UTF8.GetBytes(FixerTimeSeries));
using var fixer = new FixerRateProvider(new HttpClient(handler), new FixerRateProviderOptions { ApiKey = "test-key" });

await fixer.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

Console.WriteLine(fixer.GetRate("EUR", "USD", new DateOnly(2023, 1, 3)).Rate.Rate);   // 1.0546
Console.WriteLine(handler.LastRequestUri!.Query.Contains("access_key=test-key"));     // True — the key went on the wire
Console.WriteLine(handler.LastAuthorization is null);                                  // True — Fixer keys the query, not the header
```

A non-success status code exercises the transport-failure path. A hand-built client has no
resilience pipeline, so the `HttpRequestException` is immediate:

```csharp
using System.Net;

var handler = new StubHttpMessageHandler(Array.Empty<byte>(), HttpStatusCode.ServiceUnavailable);
using var fixer = new FixerRateProvider(new HttpClient(handler), new FixerRateProviderOptions { ApiKey = "test-key" });

try
{
    await fixer.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));
}
catch (HttpRequestException ex)
{
    Console.WriteLine(ex.StatusCode);   // ServiceUnavailable — no resilience pipeline on a hand-built client
}
```

> [!NOTE]
> The stub returns the same body whatever the URL, so one handler serves a provider that issues
> several requests per warm-up (a multi-era RBA load, an ECB provider whose window spans the
> 90-day and full-history feeds). When a test needs different bodies per URL, write a small
> `HttpMessageHandler` of your own — the stub is deliberately minimal.

## Pattern 4 — a file-backed `IPairRateSource<TSeries>`

A pair provider built on <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> delegates its
fetch-and-parse to an <xref:Bodu.Financial.ExchangeRates.IPairRateSource`1>. A provider that
exposes a constructor taking that seam — as the `AcmeRateProvider` from
[Writing your own web provider](custom-web-provider.md) does — can be driven from a committed
data file with no HTTP at all, which keeps the coverage tracking, single-flight coalescing, and
lookup surface under test while the network is out of the picture. The shipped providers keep
their source constructors internal, so this recipe applies to providers you write.

```csharp
using System.Globalization;
using Bodu.Financial.ExchangeRates;

/// <summary>
/// A file-backed <see cref="IPairRateSource{TSeries}" />: reads <c>Date,From,To,Rate</c> rows from a committed CSV
/// and answers each request from that file, so a pair provider runs with no network at all.
/// </summary>
public sealed class CsvPairRateSource : IPairRateSource<AcmeSeriesInfo>
{
    private readonly string _path;

    public CsvPairRateSource(string path) => _path = path;

    public int RequestCount { get; private set; }

    public async ValueTask<PairRateData<AcmeSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        RequestCount++;
        var observations = new List<RateObservation>();

        foreach (string line in await File.ReadAllLinesAsync(_path, cancellationToken))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("Date,", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] cells = line.Split(',');
            var date = DateOnly.ParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (cells[1] == request.Pair.From.ToString() && cells[2] == request.Pair.To.ToString()
                && date >= request.StartDate && date <= request.EndDate)
            {
                observations.Add(new RateObservation(date, decimal.Parse(cells[3], CultureInfo.InvariantCulture)));
            }
        }

        return new PairRateData<AcmeSeriesInfo>(request.Pair, observations, new AcmeSeriesInfo(request.Pair, Path.GetFileName(_path)));
    }
}
```

The committed `samples/Financial/Bodu.Financial.Samples.CustomProvider/Data/custom-feed.csv`
(AUD/USD business days for January 2024) is a ready-made fixture:

```csharp
var source = new CsvPairRateSource(csvPath);           // samples/Financial/…/Data/custom-feed.csv
using var acme = new AcmeRateProvider(source, new AcmeRateProviderOptions());

await acme.LoadPairAsync("AUD", "USD", new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 31));

Console.WriteLine(acme.GetRate("AUD", "USD", new DateOnly(2024, 1, 15)).Rate.Rate);   // 0.6660
Console.WriteLine(source.RequestCount);                                                // 1
```

## Pattern 5 — `NullRateCache` keeps the composition, drops the caching

When a test composes the production wiring but must observe every miss reaching the source,
substitute <xref:Bodu.Financial.ExchangeRates.Caching.NullRateCache> — it stores nothing, so
every lookup is a miss — through the `cacheFactory` parameter rather than by removing the
caching registration:

```csharp
using Bodu.Financial.ExchangeRates.Caching;

services
    .AddFinancialService()
    .AddRbaExchangeRates()
    .AddCachedRateProvider<RbaRateProvider>("RBA", cacheFactory: (sp, name) => NullRateCache.Create(name));
```

By hand, it is the cache argument of the decorator:

```csharp
var uncached = new CachingRateProvider(inner, NullRateCache.Create("RBA"), new CachingRateOptions());
```

For a test that *does* want caching but no disk, `InMemoryRateCache` is the same swap. The
[caching guide's backend table](exchange-rate-caching.md#persistent-and-shared-backends)
lists all of them.

## Pattern 6 — deriving the dated-provider contract

Any custom `IDatedRateProvider` should pass the shared contract. Derive
`DatedRateProviderContractTests<TProvider>` from the Testing project, supply a seeded provider
and the dates that characterise it, and the base exercises the whole surface: sync/async
equivalence for single dates and ranges, misses (`TryGetRate` false, `GetRate` throwing
`KeyNotFoundException`), provenance consistency, null-options defaulting, inverse reciprocal
lookups, same-currency identity rates, and (opt-in) disposal guards.

| Member | Kind | Purpose |
|---|---|---|
| `CreateProvider()` | abstract | A cold, independently seeded provider per call. |
| `CanonicalPair` | abstract | The pair the provider is seeded for. |
| `KnownDate` | abstract | A date that resolves under `RateLookupOptions.Exact`. |
| `UnknownDate` | abstract | A date that resolves in neither direction. |
| `RangeStart` / `RangeEnd` | virtual (default `KnownDate`) | The inclusive window the range contract sweeps. |
| `SupportsInverseLookup` | virtual (`true`) | Switch off for a provider that rejects reverse-direction lookups. |
| `SupportsIdentityRate` | virtual (`true`) | Switch off for a provider that rejects same-currency lookups. |
| `SupportsDisposalGuard` | virtual (`false`) | Opt in to the post-dispose `ObjectDisposedException` sweep. |

```csharp
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

[TestClass]
public sealed class CsvFileRateProviderTests
    : DatedRateProviderContractTests<CsvFileRateProvider>
{
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    protected override DateOnly KnownDate => new(2024, 1, 15);

    protected override DateOnly UnknownDate => new(2024, 6, 17);

    protected override DateOnly RangeStart => new(2024, 1, 2);

    protected override DateOnly RangeEnd => new(2024, 1, 31);

    protected override CsvFileRateProvider CreateProvider() =>
        new(Path.Combine(AppContext.BaseDirectory, "Data", "custom-feed.csv"), "CustomFeed");
}
```

This exact class runs in the repository: the
[CustomProvider sample](../../samples/financial.md#bodufinancialsamplescustomprovider--test) is a
consumer-shaped `CsvFileRateProvider` whose companion test project derives the base and passes
it in CI.

## Pattern 7 — deriving the pair-web-provider contract

Providers built on <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> additionally derive
`PairWebRateProviderContractTests<TProvider, TSeries>`, which layers the pair warm-up lifecycle
on top of everything above: `LoadPairAsync` resolving a known date, idempotent re-warm of an
already-covered window, `GetAvailablePairs` reporting the loaded series, and the provider's
declared <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability> matching the value the
test states. It adds one abstract member:

| Member | Kind | Purpose |
|---|---|---|
| `ExpectedHistoryAvailability` | abstract | The depth the provider must advertise — an intentional `Unbounded` included — so a new provider cannot forget to declare it. |

Point the provider at a stub handler (or a file-backed source) so the contract runs
hermetically. The provider's `AllowSynchronousNetworkAccess` is switched on so the base's
synchronous probes fetch through the stub instead of reporting misses:

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

`AcmeRateProvider` and its series-info type are the ones built in
[Writing your own web provider](custom-web-provider.md); the shipped provider test projects
(Yahoo, OFX, Fixer, …) follow exactly this shape against their own fixtures.

## Where to go next

- [Writing your own web provider](custom-web-provider.md) — the provider these contract tests validate.
- [Financial dependency injection](dependency-injection.md) — registering fakes and the `TryAdd` rules.
- [Caching and aggregating exchange rates](exchange-rate-caching.md) — the cache backends, including the in-memory and null ones tests reach for.
- [Runnable samples](../../samples/financial.md) — `CustomProvider` and its `.Test` companion run this page's patterns in CI.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic.
