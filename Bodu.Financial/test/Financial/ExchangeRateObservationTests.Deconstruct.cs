// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateObservationTests.Deconstruct.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateObservationTests
{

    /// <summary>
    /// Verifies that deconstruction surfaces the recorded date and rate.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenCalled_ShouldReturnDateAndRate()
    {
        ExchangeRateObservation observation = new(new DateOnly(2026, 6, 1), 1.5m);

        (DateOnly date, decimal rate) = observation;

        Assert.AreEqual(new DateOnly(2026, 6, 1), date);
        Assert.AreEqual(1.5m, rate);
    }
}
