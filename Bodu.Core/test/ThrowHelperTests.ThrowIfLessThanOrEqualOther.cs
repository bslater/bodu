// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqualOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Less Than Or Equal Other, when Value Is Less Than Or Equal To Other, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(5, 6)]
        [DataRow(3, 3)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldThrowArgumentException(int value, int other)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfLessThanOrEqualOther(value, other);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than Or Equal Other, when Value Is Greater Than Other, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(1, 0)]
        [DataRow(6, 5)]
        [DataRow(int.MaxValue, int.MinValue)]
        public void ThrowIfLessThanOrEqualOther_WhenValueIsGreaterThanOther_ShouldNotThrow(int value, int other)
        {
            ThrowHelper.ThrowIfLessThanOrEqualOther(value, other);
        }
    }
}