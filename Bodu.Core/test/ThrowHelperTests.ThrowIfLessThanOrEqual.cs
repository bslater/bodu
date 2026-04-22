// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Less Than Or Equal, when Value Is Less Than Or Equal To Min, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(5, 6)]
        [DataRow(int.MinValue, int.MaxValue)]
        [DataRow(3, 3)]
        public void ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldThrowArgumentOutOfRangeException(int value, int min)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfLessThanOrEqual(value, min);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than Or Equal, when Value Is Greater Than Min, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(1, 0)]
        [DataRow(6, 5)]
        [DataRow(int.MaxValue, int.MinValue)]
        public void ThrowIfLessThanOrEqual_WhenValueIsGreaterThanMin_ShouldNotThrow(int value, int min)
        {
            ThrowHelper.ThrowIfLessThanOrEqual(value, min);
        }
    }
}