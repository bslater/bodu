// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailabilityTests.Since.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="RateHistoryAvailability.Since" /> records a since kind and the supplied earliest
    /// date.
    /// </summary>
    [TestMethod]
    public void Since_WhenGivenDate_ShouldRecordKindAndEarliestDate()
    {
        var earliest = new DateOnly(1999, 1, 1);

        var availability = RateHistoryAvailability.Since(earliest);

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, availability.Kind);
        Assert.AreEqual(earliest, availability.EarliestDate);
    }
}
