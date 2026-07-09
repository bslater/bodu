// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.Extensions;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the behaviour of the <see cref="DatedRateProviderExtensions" /> materializers when the source is a
/// <see cref="WebRateProvider" /> — the cross-package half of the suite that lives in
/// <c>Bodu.Financial.Test</c>.
/// </summary>
[TestClass]
public class DatedRateProviderExtensionsTests
{
    private static readonly CurrencyPair AudUsd = new(CurrencyCode.AUD, CurrencyCode.USD);
    private static readonly DateOnly RangeStart = new(2023, 1, 1);
    private static readonly DateOnly RangeEnd = new(2023, 1, 31);
    private static readonly DateOnly Known = new(2023, 1, 3);

    private static ExchangeRate Rate(string from, string to, DateOnly date, decimal rate, string provider = "Test", DateTimeOffset? fetchedAtUtc = null) =>
        new(CurrencyInfo.ParseCurrencyCode(from), CurrencyInfo.ParseCurrencyCode(to), date, rate, provider, isInverted: false, fetchedAtUtc);

    /// <summary>
    /// Verifies that materializing from a web provider triggers its fetches and the result is isolated from data the
    /// source accumulates afterwards.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenSourceIsWebProvider_ShouldFetchAndStayIsolated()
    {
        using TestBulkWebRateProvider source = new(Rate("AUD", "USD", Known, 0.6828m, "TESTBULK"));

        FixedDatedRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.AreEqual(1, source.LoadCount, "materialization triggered the catalogue fetch");
        Assert.IsTrue(snapshot.TryGetRate("AUD", "USD", Known, RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreNotSame(source.GetLoadedSnapshot(), snapshot, "the materialized snapshot is a decoupled instance");
    }

}
