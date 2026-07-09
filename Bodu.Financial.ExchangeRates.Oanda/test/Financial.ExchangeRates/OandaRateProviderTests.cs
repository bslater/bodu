// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the construction behavior of <see cref="OandaRateProvider" /> that the shared dated-provider
/// contract tests do not exercise, notably the public options-only constructor that builds and owns its
/// <see cref="HttpClient" />, and the advertised history availability.
/// </summary>
[TestClass]
public partial class OandaRateProviderTests
{
    /// <summary>
    /// Verifies that the provider advertises the anonymous endpoint's 180-day rolling history window.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_ShouldReportRolling180Days()
    {
        OandaRateProviderOptions options = new();
        FixtureOandaExchangeRateSource source = new(options);
        using OandaRateProvider provider = new(source, options);

        var today = new DateOnly(2026, 6, 28);

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, provider.HistoryAvailability.Kind);
        Assert.AreEqual(today.AddDays(-180), provider.HistoryAvailability.GetEarliestAvailable(today));
    }
}
