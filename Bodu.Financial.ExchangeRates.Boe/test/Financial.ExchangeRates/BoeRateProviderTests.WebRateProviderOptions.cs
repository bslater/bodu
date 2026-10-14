// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.ExchangeRates.Testing;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>The event id of the <c>SynchronousNetworkFetch</c> diagnostic logged after a blocking on-demand load.</summary>
    private const int SynchronousNetworkFetchEventId = 4204;

    /// <summary>
    /// Verifies that the provider requests the IADB query under the configured
    /// <see cref="WebRateProviderOptions.BaseAddress" />.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenBaseAddressConfigured_ShouldRequestTheIadbQueryUnderIt()
    {
        BoeRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).BaseAddress = new Uri("https://mirror.example/iadb/");
        StubHttpMessageHandler handler = new(BoeFixtures.ReadBytes(BoeFixtures.Sample));
        using HttpClient client = new(handler);
        using BoeRateProvider provider = new(client, options);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual(
            "https://mirror.example/iadb/_iadb-fromshowcolumns.asp",
            handler.LastRequestUri!.GetLeftPart(UriPartial.Path));
    }

    /// <summary>
    /// Verifies that the client the provider builds for itself carries the configured
    /// <see cref="WebRateProviderOptions.HttpTimeout" />, <see cref="WebRateProviderOptions.UserAgent" /> and
    /// <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderOwnsItsClient_ShouldApplyTheTimeoutUserAgentAndResponseCap()
    {
        BoeRateProviderOptions options = new() { EnableDiskCache = false };
        WebRateProviderOptions shared = SharedOptions.Of(options);
        shared.HttpTimeout = TimeSpan.FromSeconds(17);
        shared.UserAgent = "spec-agent";
        shared.MaxResponseContentBufferSize = 4096;

        using BoeRateProvider provider = new(options);
        HttpClient client = GetOwnedClient(provider);

        Assert.AreEqual(TimeSpan.FromSeconds(17), client.Timeout);
        Assert.AreEqual("spec-agent", client.DefaultRequestHeaders.GetValues("User-Agent").Single());
        Assert.AreEqual(4096L, client.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Verifies that a single-date lookup that must load on demand loads the
    /// <see cref="WebRateProviderOptions.DefaultLookback" /> window ending on the requested date.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackConfigured_ShouldLoadTheWindowEndingOnTheDate()
    {
        BoeRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).DefaultLookback = TimeSpan.FromDays(3);
        FixtureBoeRateTableSource source = new(options);
        using BoeRateProvider provider = new(source, options);

        RateLookupResult result = await provider.GetRateAsync("GBP", "USD", new DateOnly(2023, 1, 4));

        Assert.AreEqual(1.2050m, result.Rate.Rate);
        CollectionAssert.AreEqual(new[] { (new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 4)) }, source.Requests);
    }

    /// <summary>
    /// Verifies that <see cref="WebRateProviderOptions.CurrencyAliases" /> maps a series' quote label to the ISO code
    /// the provider serves it under.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenCurrencyAliasMapsASeriesQuoteLabel_ShouldServeTheAliasedCurrency()
    {
        BoeRateProviderOptions options = new()
        {
            EnableDiskCache = false,
            Series = [new BoeSeries("DOLLAR", "XUDLUSS", "US dollar into Sterling")],
        };
        SharedOptions.Of(options).CurrencyAliases["DOLLAR"] = "USD";
        FixtureBoeRateTableSource source = new(options);
        using BoeRateProvider provider = new(source, options);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(1.2065m, provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that a synchronous lookup that blocks to load on demand is logged at the configured
    /// <see cref="WebRateProviderOptions.SynchronousNetworkFetchLogLevel" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSynchronousFetchOccurs_ShouldLogItAtTheConfiguredLevel()
    {
        BoeRateProviderOptions options = new() { EnableDiskCache = false };
        WebRateProviderOptions shared = SharedOptions.Of(options);
        shared.AllowSynchronousNetworkAccess = true;
        shared.SynchronousNetworkFetchLogLevel = LogLevel.Information;
        CapturingLogger logger = new();
        using BoeRateProvider provider = new(new FixtureBoeRateTableSource(options), options, logger);

        Assert.IsTrue(provider.TryGetRate("GBP", "USD", new DateOnly(2023, 1, 3), null, out _));

        Assert.AreEqual(
            1,
            logger.Entries.Count(entry => entry.EventId.Id == SynchronousNetworkFetchEventId && entry.Level == LogLevel.Information));
    }

    /// <summary>
    /// Reads the HTTP client a provider built for itself and owns.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The provider-owned client.</returns>
    private static HttpClient GetOwnedClient(WebRateProvider provider)
    {
        FieldInfo? field = typeof(WebRateProvider).GetField("_ownedHttpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);

        object? client = field.GetValue(provider);
        Assert.IsInstanceOfType<HttpClient>(client);

        return (HttpClient)client;
    }
}
