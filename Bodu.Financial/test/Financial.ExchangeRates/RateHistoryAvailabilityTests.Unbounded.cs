// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailabilityTests.Unbounded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="RateHistoryAvailability.Unbounded" /> records the unbounded kind.
    /// </summary>
    [TestMethod]
    public void Unbounded_ShouldRecordUnboundedKind()
    {
        RateHistoryAvailability availability = RateHistoryAvailability.Unbounded;

        Assert.AreEqual(RateHistoryAvailabilityKind.Unbounded, availability.Kind);
    }

    /// <summary>
    /// Verifies that the default-constructed value is unbounded, so an uninitialized availability imposes no floor.
    /// </summary>
    [TestMethod]
    public void Unbounded_WhenDefaultConstructed_ShouldBeUnbounded()
    {
        RateHistoryAvailability availability = default;

        Assert.AreEqual(RateHistoryAvailabilityKind.Unbounded, availability.Kind);
        Assert.AreEqual(availability, RateHistoryAvailability.Unbounded);
    }
}
