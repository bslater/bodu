// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHistoryAvailabilityTests.Unbounded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateHistoryAvailability.Unbounded" /> records the unbounded kind.
    /// </summary>
    [TestMethod]
    public void Unbounded_ShouldRecordUnboundedKind()
    {
        ExchangeRateHistoryAvailability availability = ExchangeRateHistoryAvailability.Unbounded;

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Unbounded, availability.Kind);
    }

    /// <summary>
    /// Verifies that the default-constructed value is unbounded, so an uninitialized availability imposes no floor.
    /// </summary>
    [TestMethod]
    public void Unbounded_WhenDefaultConstructed_ShouldBeUnbounded()
    {
        ExchangeRateHistoryAvailability availability = default;

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Unbounded, availability.Kind);
        Assert.AreEqual(availability, ExchangeRateHistoryAvailability.Unbounded);
    }
}
