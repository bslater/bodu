// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.GetRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a cross-currency range request (neither leg is USD) is rejected with
    /// <see cref="RateSeriesNotFoundException" /> because the IMF report is quoted only against the US dollar.
    /// </summary>
    [TestMethod]
    public async Task GetRates_WhenCrossCurrencyPair_ShouldThrowRateSeriesNotFoundException()
    {
        using ImfRateProvider provider = await CreateLoadedProviderAsync();

        _ = Assert.ThrowsExactly<RateSeriesNotFoundException>(() =>
        {
            _ = provider.GetRates("GBP", "EUR", s_seeded, s_seeded);
        });
    }
}
