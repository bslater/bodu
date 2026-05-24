// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextDouble.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// Verifies that <see cref="XorShiftRandom.NextDouble" />, when Called, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void NextDouble_WhenCalled_ShouldReturnValueBetweenZeroAndOne()
    {
        var rng = new XorShiftRandom();
        for (var i = 0; i < 100; i++)
        {
            var value = rng.NextDouble();
            Assert.IsTrue(value >= 0.0 && value <= 1.0);
        }
    }

}
