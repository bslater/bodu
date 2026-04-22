// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Greater Than, when Value Is Greater, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(6, 5)]
        [DataRow(1, 0)]
        [DataRow(int.MaxValue, int.MaxValue - 1)]
        public void ThrowIfGreaterThan_WhenValueIsGreater_ShouldThrowArgumentOutOfRangeException(int value, int max)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfGreaterThan(value, max);
            });
        }

        /// <summary>
        /// Verifies that Throw If Greater Than, when Value Is Less Than Or Equal To Max, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(3, 3)]
        [DataRow(2, 3)]
        [DataRow(0, 0)]
        [DataRow(-1, 0)]
        [DataRow(int.MinValue, int.MinValue)]
        public void ThrowIfGreaterThan_WhenValueIsLessThanOrEqualToMax_ShouldNotThrow(int value, int max)
        {
            ThrowHelper.ThrowIfGreaterThan(value, max);
        }
    }
}