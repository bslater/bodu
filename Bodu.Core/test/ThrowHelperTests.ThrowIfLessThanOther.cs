// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Less Than Other, when Value Is Less Than Other, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(0, 1)]
        [DataRow(5, 6)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldThrowArgumentException(int value, int other)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfLessThanOther(value, other);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than Other, when Value Is Equal Or Greater Than Other, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(6, 5)]
        [DataRow(int.MaxValue, int.MinValue)]
        public void ThrowIfLessThanOther_WhenValueIsEqualOrGreaterThanOther_ShouldNotThrow(int value, int other)
        {
            ThrowHelper.ThrowIfLessThanOther(value, other);
        }
    }
}