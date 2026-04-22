// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu
{
    public partial class XorShiftRandomTests
    {
        [TestMethod]
        [DataRow(int.MinValue)]
        [DataRow(0)]
        [DataRow(int.MaxValue)]
        public void Constructor_WhenValidRange_ShouldCreateInstance(int seed)
        {
            var rng = new XorShiftRandom(seed);
            Assert.IsNotNull(rng);
        }

        [TestMethod]
        public void Constructor_WhenCalledWithoutSeed_ShouldCreateInstance()
        {
            var rng = new XorShiftRandom();
            Assert.IsNotNull(rng);
        }
    }
}