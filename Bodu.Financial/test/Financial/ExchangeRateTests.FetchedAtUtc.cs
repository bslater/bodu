// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.FetchedAtUtc.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that a supplied fetch instant is exposed through <see cref="ExchangeRate.FetchedAtUtc" />.
    /// </summary>
    [TestMethod]
    public void FetchedAtUtc_WhenSupplied_ShouldBeExposed()
    {
        DateTimeOffset fetchedAt = new(2024, 1, 3, 9, 30, 0, TimeSpan.Zero);
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA", isInverted: false, fetchedAt);

        Assert.AreEqual(fetchedAt, rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.FetchedAtUtc" /> is <see langword="null" /> when no fetch instant is
    /// supplied at construction.
    /// </summary>
    [TestMethod]
    public void FetchedAtUtc_WhenOmitted_ShouldBeNull()
    {
        ExchangeRate rate = new("USD", "AUD", s_sampleDate, 1.5m, "RBA");

        Assert.IsNull(rate.FetchedAtUtc);
    }
}
