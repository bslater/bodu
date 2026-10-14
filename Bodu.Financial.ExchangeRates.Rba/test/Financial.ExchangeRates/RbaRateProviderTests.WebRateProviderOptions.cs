// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.ExchangeRates.Testing;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>The event id of the <c>SynchronousNetworkFetch</c> diagnostic logged after a blocking on-demand load.</summary>
    private const int SynchronousNetworkFetchEventId = 4304;

    /// <summary>
    /// Verifies that the provider requests each era workbook under the configured
    /// <see cref="WebRateProviderOptions.BaseAddress" />.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenBaseAddressConfigured_ShouldRequestTheEraWorkbookUnderIt()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).BaseAddress = new Uri("https://mirror.example/rba/");
        StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(RbaFixtures.Sample));
        using HttpClient client = new(handler);
        using RbaRateProvider provider = new(client, options);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 5));

        Assert.AreEqual(new Uri("https://mirror.example/rba/2023-current.xls"), handler.LastRequestUri);
    }

    /// <summary>
    /// Verifies that the client the provider builds for itself carries the configured
    /// <see cref="WebRateProviderOptions.HttpTimeout" />, <see cref="WebRateProviderOptions.UserAgent" /> and
    /// <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderOwnsItsClient_ShouldApplyTheTimeoutUserAgentAndResponseCap()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        WebRateProviderOptions shared = SharedOptions.Of(options);
        shared.HttpTimeout = TimeSpan.FromSeconds(17);
        shared.UserAgent = "spec-agent";
        shared.MaxResponseContentBufferSize = 4096;

        using RbaRateProvider provider = new(options);
        HttpClient client = GetOwnedClient(provider);

        Assert.AreEqual(TimeSpan.FromSeconds(17), client.Timeout);
        Assert.AreEqual("spec-agent", client.DefaultRequestHeaders.GetValues("User-Agent").Single());
        Assert.AreEqual(4096L, client.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Verifies that the open-ended current era, loaded before the RBA published today's rates, is downloaded again,
    /// for a request that reaches today, once the shared <see cref="WebRateProviderOptions.RefreshInterval" /> has
    /// passed.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenRefreshIntervalHasPassed_ShouldLoadTheCurrentEraAgain()
    {
        DateOnly today = new(2023, 1, 4);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 1, 4, 9, 0, 0, TimeSpan.Zero));
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).RefreshInterval = TimeSpan.FromHours(1);
        FixtureRbaRateTableSource source = new(options) { PublishedThrough = new DateOnly(2023, 1, 3) };
        using RbaRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadRangeAsync(today, today);
        source.PublishedThrough = today;
        time.Advance(TimeSpan.FromHours(2));
        await provider.LoadRangeAsync(today, today);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.IsTrue(provider.TryGetRate("AUD", "USD", today, RateLookupOptions.Exact, out _));
    }

    /// <summary>
    /// Verifies that a single-date lookup that must load on demand loads every era the
    /// <see cref="WebRateProviderOptions.DefaultLookback" /> window ending on the requested date reaches, so a date
    /// early in an era also loads the era before it.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackReachesThePreviousEra_ShouldLoadBothEras()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).DefaultLookback = TimeSpan.FromDays(7);
        FixtureRbaRateTableSource source = new(options);
        using RbaRateProvider provider = new(source, options);

        RateLookupResult result = await provider.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        CollectionAssert.AreEquivalent(new[] { "2018-2022", "2023-current" }, source.RequestedEras.ToArray());
    }

    /// <summary>
    /// Verifies that a single-date lookup whose <see cref="WebRateProviderOptions.DefaultLookback" /> window lies
    /// within one era loads only that era.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackStaysWithinTheEra_ShouldLoadOnlyThatEra()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).DefaultLookback = TimeSpan.FromDays(1);
        FixtureRbaRateTableSource source = new(options);
        using RbaRateProvider provider = new(source, options);

        RateLookupResult result = await provider.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        CollectionAssert.AreEqual(new[] { "2023-current" }, source.RequestedEras.ToArray());
    }

    /// <summary>
    /// Verifies that a <see cref="WebRateProviderOptions.HistoryAvailability" /> set on the options is the depth the
    /// provider advertises, in place of the depth of its era catalogue.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenSetOnTheOptions_ShouldAdvertiseIt()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).HistoryAvailability = RateHistoryAvailability.Since(new DateOnly(2010, 1, 1));
        using RbaRateProvider provider = new(new FixtureRbaRateTableSource(options), options);

        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(2010, 1, 1)), provider.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a synchronous lookup that blocks to load on demand is logged at the configured
    /// <see cref="WebRateProviderOptions.SynchronousNetworkFetchLogLevel" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSynchronousFetchOccurs_ShouldLogItAtTheConfiguredLevel()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        WebRateProviderOptions shared = SharedOptions.Of(options);
        shared.AllowSynchronousNetworkAccess = true;
        shared.SynchronousNetworkFetchLogLevel = LogLevel.Information;
        CapturingLogger logger = new();
        using RbaRateProvider provider = new(new FixtureRbaRateTableSource(options), options, logger);

        Assert.IsTrue(provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), null, out _));

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
