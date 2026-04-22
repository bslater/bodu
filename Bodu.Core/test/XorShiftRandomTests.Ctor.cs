// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu
{
    public partial class XorShiftRandomTests
    {
        /// <summary>
        /// Verifies that Constructor, when Valid Range, Create Instance.
        /// </summary>
        [TestMethod]
        [DataRow(int.MinValue)]
        [DataRow(0)]
        [DataRow(int.MaxValue)]
        public void Constructor_WhenValidRange_ShouldCreateInstance(int seed)
        {
            var rng = new XorShiftRandom(seed);
            Assert.IsNotNull(rng);
        }

        /// <summary>
        /// Verifies that Constructor, when Called Without Seed, Create Instance.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithoutSeed_ShouldCreateInstance()
        {
            var rng = new XorShiftRandom();
            Assert.IsNotNull(rng);
        }
    }
}