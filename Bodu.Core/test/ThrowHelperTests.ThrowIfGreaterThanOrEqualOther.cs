// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOrEqualOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Greater Than Or Equal Other, when Value Is Greater Than Or Equal To Other, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(5, 5)]
        [DataRow(6, 5)]
        [DataRow(1, 0)]
        [DataRow(0, 0)]
        [DataRow(int.MaxValue, int.MaxValue)]
        public void ThrowIfGreaterThanOrEqualOther_WhenValueIsGreaterThanOrEqualToOther_ShouldThrowArgumentException(int value, int other)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfGreaterThanOrEqualOther(value, other);
            });
        }

        /// <summary>
        /// Verifies that Throw If Greater Than Or Equal Other, when Value Is Less Than Other, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(4, 5)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void ThrowIfGreaterThanOrEqualOther_WhenValueIsLessThanOther_ShouldNotThrow(int value, int other)
        {
            ThrowHelper.ThrowIfGreaterThanOrEqualOther(value, other);
        }
    }
}