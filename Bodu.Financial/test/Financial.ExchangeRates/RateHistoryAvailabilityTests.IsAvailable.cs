// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailabilityTests.IsAvailable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that a date on or after the rolling floor is reported available while an earlier date is not.
    /// </summary>
    [TestMethod]
    public void IsAvailable_WhenRolling_ShouldGateOnTheFloor()
    {
        var availability = RateHistoryAvailability.RollingDays(180);
        var asOf = new DateOnly(2026, 6, 28);
        DateOnly floor = asOf.AddDays(-180);

        Assert.IsTrue(availability.IsAvailable(floor, asOf), "the floor date itself is available");
        Assert.IsTrue(availability.IsAvailable(asOf, asOf), "the reference date is available");
        Assert.IsFalse(availability.IsAvailable(floor.AddDays(-1), asOf), "a date before the floor is unavailable");
    }

    /// <summary>
    /// Verifies that a since availability gates on its fixed earliest date.
    /// </summary>
    [TestMethod]
    public void IsAvailable_WhenSince_ShouldGateOnEarliestDate()
    {
        var earliest = new DateOnly(1999, 1, 1);
        var availability = RateHistoryAvailability.Since(earliest);
        var asOf = new DateOnly(2026, 6, 28);

        Assert.IsTrue(availability.IsAvailable(earliest, asOf));
        Assert.IsFalse(availability.IsAvailable(earliest.AddDays(-1), asOf));
    }

    /// <summary>
    /// Verifies that an unbounded availability reports every date available.
    /// </summary>
    [TestMethod]
    public void IsAvailable_WhenUnbounded_ShouldAlwaysReturnTrue()
    {
        RateHistoryAvailability availability = RateHistoryAvailability.Unbounded;

        Assert.IsTrue(availability.IsAvailable(DateOnly.MinValue, new DateOnly(2026, 6, 28)));
    }
}
