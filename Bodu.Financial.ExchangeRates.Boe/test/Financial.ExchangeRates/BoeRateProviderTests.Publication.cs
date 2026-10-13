// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.Publication.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that a configured series whose quote code the ISO 4217 catalog does not list (here <c>TRL</c>, the
    /// Turkish lira retired in 2005) is skipped, and the other series in the response are published.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSeriesQuotesUnknownCurrency_ShouldSkipItAndPublishTheRest()
    {
        BoeRateProviderOptions options = new()
        {
            EnableDiskCache = false,
            Series =
            [
                new("USD", "XUDLUSS", "US dollar into Sterling"),
                new("TRL", "XUDLJYS", "A retired currency mapped onto a fixture column"),
            ],
        };
        FixtureBoeRateTableSource source = new(options);
        using BoeRateProvider provider = new(source, options);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(1.2065m, provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3)).Rate.Rate);
        Assert.HasCount(1, provider.GetAvailablePairs());
    }
}
