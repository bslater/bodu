// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.GetHashCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that two exchange rates with identical fields produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenFieldsMatch_ShouldBeEqual()
    {
        var a = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "ecb");
        var b = new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "ecb");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two exchange rates that differ only in their fetch instant produce the same hash code, because the
    /// fetch instant is excluded from the hash.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenOnlyFetchedAtUtcDiffers_ShouldBeEqual()
    {
        ExchangeRate stamped = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA", isInverted: false, new DateTimeOffset(2024, 1, 3, 9, 30, 0, TimeSpan.Zero));
        ExchangeRate unstamped = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

        Assert.AreEqual(unstamped.GetHashCode(), stamped.GetHashCode());
    }
}
