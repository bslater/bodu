// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailabilityTests.RollingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="RateHistoryAvailability.RollingDays" /> records a rolling kind and the supplied
    /// window length.
    /// </summary>
    [TestMethod]
    public void RollingDays_WhenPositive_ShouldRecordKindAndWindow()
    {
        var availability = RateHistoryAvailability.RollingDays(180);

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, availability.Kind);
        Assert.AreEqual(180, availability.WindowDays);
    }

    /// <summary>
    /// Verifies that a non-positive window length throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="days">The invalid window length under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void RollingDays_WhenNotPositive_ShouldThrowArgumentOutOfRangeException(int days)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = RateHistoryAvailability.RollingDays(days);
        });
    }
}
