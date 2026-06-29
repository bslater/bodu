// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHistoryAvailabilityTests.Since.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateHistoryAvailabilityTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateHistoryAvailability.Since" /> records a since kind and the supplied earliest
    /// date.
    /// </summary>
    [TestMethod]
    public void Since_WhenGivenDate_ShouldRecordKindAndEarliestDate()
    {
        var earliest = new DateOnly(1999, 1, 1);

        ExchangeRateHistoryAvailability availability = ExchangeRateHistoryAvailability.Since(earliest);

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Since, availability.Kind);
        Assert.AreEqual(earliest, availability.EarliestDate);
    }
}
