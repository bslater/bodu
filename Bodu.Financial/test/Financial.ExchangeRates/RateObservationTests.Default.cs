// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateObservationTests.Default.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateObservationTests
{
    /// <summary>
    /// Verifies that a default-constructed instance carries <see cref="DateOnly.MinValue" /> and a zero rate.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldHaveZeroRateAndMinDate()
    {
        RateObservation observation = default;

        Assert.AreEqual(DateOnly.MinValue, observation.Date);
        Assert.AreEqual(0m, observation.Rate);
    }
}
