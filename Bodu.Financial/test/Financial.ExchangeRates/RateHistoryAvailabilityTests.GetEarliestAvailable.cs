// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailabilityTests.GetEarliestAvailable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that a rolling availability resolves the earliest date by subtracting the window from the reference
    /// date.
    /// </summary>
    [TestMethod]
    public void GetEarliestAvailable_WhenRolling_ShouldSubtractWindowFromAsOf()
    {
        var availability = RateHistoryAvailability.RollingDays(180);
        var asOf = new DateOnly(2026, 6, 28);

        Assert.AreEqual(asOf.AddDays(-180), availability.GetEarliestAvailable(asOf));
    }

    /// <summary>
    /// Verifies that a since availability resolves the earliest date as its fixed date, independent of the reference
    /// date.
    /// </summary>
    [TestMethod]
    public void GetEarliestAvailable_WhenSince_ShouldReturnFixedDate()
    {
        var earliest = new DateOnly(1999, 1, 1);
        var availability = RateHistoryAvailability.Since(earliest);

        Assert.AreEqual(earliest, availability.GetEarliestAvailable(new DateOnly(2026, 6, 28)));
    }

    /// <summary>
    /// Verifies that an unbounded availability resolves no earliest date.
    /// </summary>
    [TestMethod]
    public void GetEarliestAvailable_WhenUnbounded_ShouldReturnNull()
    {
        RateHistoryAvailability availability = RateHistoryAvailability.Unbounded;

        Assert.IsNull(availability.GetEarliestAvailable(new DateOnly(2026, 6, 28)));
    }
}
