// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextDouble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextDouble" /> returns values in the expected range [0.0, 1.0).
    /// </summary>
    [TestMethod]
    public void NextDouble_ShouldBeWithinRange()
    {
        var rng = new XorShiftRandom();
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.NextDouble();
            Assert.IsTrue(value >= 0.0 && value < 1.0, $"Value {value} was not in range [0.0, 1.0).");
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextDouble" /> returns values inside the half-open
    /// unit interval [0.0, 1.0), matching the <see cref="System.Random.NextDouble" /> contract.
    /// </summary>
    [TestMethod]
    public void NextDouble_WhenCalled_ShouldReturnValueInHalfOpenUnitInterval()
    {
        var rng = new XorShiftRandom();
        for (var i = 0; i < 100; i++)
        {
            var value = rng.NextDouble();
            Assert.IsTrue(value >= 0.0 && value < 1.0, $"Value {value} was not in range [0.0, 1.0).");
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.ScaleToUnitInterval(uint)" /> returns a value strictly
    /// less than 1.0 even when the underlying generator emits <see cref="uint.MaxValue" />, preserving
    /// the [0.0, 1.0) contract of <see cref="System.Random.NextDouble" />.
    /// </summary>
    [TestMethod]
    public void ScaleToUnitInterval_WhenValueIsUIntMaxValue_ShouldReturnLessThanOne()
    {
        var actual = XorShiftRandom.ScaleToUnitInterval(uint.MaxValue);
        Assert.IsLessThan(1.0, actual, $"Expected value strictly less than 1.0 but got {actual}.");
        Assert.IsGreaterThanOrEqualTo(0.0, actual, $"Expected non-negative value but got {actual}.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.ScaleToUnitInterval(uint)" /> maps zero to zero.
    /// </summary>
    [TestMethod]
    public void ScaleToUnitInterval_WhenValueIsZero_ShouldReturnZero() => Assert.AreEqual(0.0, XorShiftRandom.ScaleToUnitInterval(0));

}
