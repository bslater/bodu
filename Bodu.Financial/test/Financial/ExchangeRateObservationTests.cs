// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateObservationTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

[TestClass]
public class ExchangeRateObservationTests
{
    /// <summary>
    /// Verifies that a default-constructed instance carries <see cref="DateOnly.MinValue" /> and a zero rate.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldHaveZeroRateAndMinDate()
    {
        ExchangeRateObservation observation = default;

        Assert.AreEqual(DateOnly.MinValue, observation.Date);
        Assert.AreEqual(0m, observation.Rate);
    }

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

    /// <summary>
    /// Verifies that deconstruction surfaces the recorded date and rate.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenCalled_ShouldReturnDateAndRate()
    {
        ExchangeRateObservation observation = new(new DateOnly(2026, 6, 1), 1.5m);

        var (date, rate) = observation;

        Assert.AreEqual(new DateOnly(2026, 6, 1), date);
        Assert.AreEqual(1.5m, rate);
    }
}
