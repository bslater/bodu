// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.Equals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that two exchange rates that differ only in their fetch instant compare equal, because the fetch
    /// instant is provenance metadata excluded from equality.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOnlyFetchedAtUtcDiffers_ShouldBeEqual()
    {
        ExchangeRate stamped = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));
        ExchangeRate other = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        ExchangeRate unstamped = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual(stamped, other);
        Assert.AreEqual(stamped, unstamped);
    }
}
