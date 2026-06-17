// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedExchangeRateTests.IsFresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the freshness contract of <see cref="CachedExchangeRate.IsFresh(DateTimeOffset, TimeSpan)" />.
/// </summary>
[TestClass]
public sealed class CachedExchangeRateTests
{
    /// <summary>
    /// Verifies that a rate younger than the duration is reported fresh, while a rate at or beyond the duration is not.
    /// </summary>
    /// <param name="ageHours">The age of the cached rate, in hours, relative to the evaluation instant.</param>
    /// <param name="expectedFresh">The expected freshness outcome.</param>
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(23, true)]
    [DataRow(24, false)]
    [DataRow(25, false)]
    public void IsFresh_WhenAgeComparedToDuration_ShouldUseStrictLessThan(int ageHours, bool expectedFresh)
    {
        var asOf = new DateTimeOffset(2023, 1, 4, 0, 0, 0, TimeSpan.Zero);
        CachedExchangeRate rate = new(new DateOnly(2023, 1, 3), 0.5m, asOf - TimeSpan.FromHours(ageHours));

        bool fresh = rate.IsFresh(asOf, TimeSpan.FromHours(24));

        Assert.AreEqual(expectedFresh, fresh);
    }
}
