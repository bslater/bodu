// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHistoryAvailabilityTests.RollingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateHistoryAvailability.RollingDays" /> records a rolling kind and the supplied
    /// window length.
    /// </summary>
    [TestMethod]
    public void RollingDays_WhenPositive_ShouldRecordKindAndWindow()
    {
        ExchangeRateHistoryAvailability availability = ExchangeRateHistoryAvailability.RollingDays(180);

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Rolling, availability.Kind);
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
            _ = ExchangeRateHistoryAvailability.RollingDays(days);
        });
    }
}
