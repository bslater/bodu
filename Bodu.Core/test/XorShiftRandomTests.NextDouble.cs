// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextDouble.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu
{
    public partial class XorShiftRandomTests
    {
        /// <summary>
        /// Verifies that NextDouble returns values in the expected range [0.0, 1.0).
        /// </summary>
        [TestMethod]
        public void NextDouble_ShouldBeWithinRange()
        {
            var rng = new XorShiftRandom();
            for (int i = 0; i < 1000; i++)
            {
                double value = rng.NextDouble();
                Assert.IsTrue(value >= 0.0 && value < 1.0, $"Value {value} was not in range [0.0, 1.0).");
            }
        }

        /// <summary>
        /// Verifies that Next Double, when Called, returns Value Between Zero And One.
        /// </summary>
        [TestMethod]
        public void NextDouble_WhenCalled_ShouldReturnValueBetweenZeroAndOne()
        {
            var rng = new XorShiftRandom();
            for (int i = 0; i < 100; i++)
            {
                double value = rng.NextDouble();
                Assert.IsTrue(value >= 0.0 && value <= 1.0);
            }
        }
    }
}