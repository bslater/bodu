// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that the provider requests each feed file under the configured
    /// <see cref="WebRateProviderOptions.BaseAddress" />.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenBaseAddressConfigured_ShouldRequestTheFeedUnderIt()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false, Feeds = [EcbRateFeed.Full] };
        SharedOptions.Of(options).BaseAddress = new Uri("https://mirror.example/fx/");
        StubHttpMessageHandler handler = new(EcbFixtures.ReadBytes(EcbFixtures.Sample));
        using HttpClient client = new(handler);
        using EcbRateProvider provider = new(client, options);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(new Uri("https://mirror.example/fx/eurofxref-hist.xml"), handler.LastRequestUri);
    }

    /// <summary>
    /// Verifies that the client the provider builds for itself carries the configured
    /// <see cref="WebRateProviderOptions.HttpTimeout" />, <see cref="WebRateProviderOptions.UserAgent" /> and
    /// <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderOwnsItsClient_ShouldApplyTheTimeoutUserAgentAndResponseCap()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        WebRateProviderOptions shared = SharedOptions.Of(options);
        shared.HttpTimeout = TimeSpan.FromSeconds(17);
        shared.UserAgent = "spec-agent";
        shared.MaxResponseContentBufferSize = 4096;

        using EcbRateProvider provider = new(options);
        HttpClient client = GetOwnedClient(provider);

        Assert.AreEqual(TimeSpan.FromSeconds(17), client.Timeout);
        Assert.AreEqual("spec-agent", client.DefaultRequestHeaders.GetValues("User-Agent").Single());
        Assert.AreEqual(4096L, client.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Verifies that a single-date lookup that must load on demand loads the feed covering the whole
    /// <see cref="WebRateProviderOptions.DefaultLookback" /> window ending on the requested date: when the window
    /// reaches back past the 90-day feed, the full history is loaded.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackReachesPastTheNarrowFeed_ShouldLoadTheFeedCoveringTheWindow()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).DefaultLookback = TimeSpan.FromDays(7);
        FixtureEcbRateTableSource source = new(options);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 4, 1, 12, 0, 0, TimeSpan.Zero));
        using EcbRateProvider provider = new(source, options, logger: null, time);

        RateLookupResult result = await provider.GetRateAsync("EUR", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(1.0545m, result.Rate.Rate);
        CollectionAssert.AreEqual(new[] { EcbRateFeed.Full.Name }, source.RequestedFeeds.ToArray());
    }

    /// <summary>
    /// Verifies that a <see cref="WebRateProviderOptions.HistoryAvailability" /> set on the options is the depth the
    /// provider advertises, in place of the depth of its feed catalog.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenSetOnTheOptions_ShouldAdvertiseIt()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        SharedOptions.Of(options).HistoryAvailability = RateHistoryAvailability.RollingDays(30);
        using EcbRateProvider provider = new(new FixtureEcbRateTableSource(options), options);

        Assert.AreEqual(RateHistoryAvailability.RollingDays(30), provider.HistoryAvailability);
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
