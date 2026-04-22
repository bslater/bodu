// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Greater Than Other, when Value Is Greater Than Other, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(6, 5)]
        [DataRow(1, 0)]
        [DataRow(int.MaxValue, int.MaxValue - 1)]
        public void ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldThrowArgumentException(int value, int other)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfGreaterThanOther(value, other);
            });
        }

        /// <summary>
        /// Verifies that Throw If Greater Than Other, when Value Is Less Than Or Equal To Other, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(3, 3)]
        [DataRow(2, 3)]
        [DataRow(0, 0)]
        [DataRow(-1, 0)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void ThrowIfGreaterThanOther_WhenValueIsLessThanOrEqualToOther_ShouldNotThrow(int value, int other)
        {
            ThrowHelper.ThrowIfGreaterThanOther(value, other);
        }
    }
}