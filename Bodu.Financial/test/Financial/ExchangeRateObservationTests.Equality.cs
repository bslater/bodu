// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateObservationTests.Equality.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateObservationTests
{

    /// <summary>
    /// Verifies that two observations with identical date and rate compare equal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameDateAndRate_ShouldBeEqual()
    {
        ExchangeRateObservation left = new(new DateOnly(2026, 6, 1), 1.5m);
        ExchangeRateObservation right = new(new DateOnly(2026, 6, 1), 1.5m);

        Assert.AreEqual(left, right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that two observations differing only by rate do not compare equal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenDifferentRate_ShouldNotBeEqual()
    {
        ExchangeRateObservation left = new(new DateOnly(2026, 6, 1), 1.5m);
        ExchangeRateObservation right = new(new DateOnly(2026, 6, 1), 1.6m);

        Assert.AreNotEqual(left, right);
    }
}
