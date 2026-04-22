// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrPositive.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Zero Or Positive, when Value Is Zero Or Positive, throws.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(int.MaxValue)]
        public void ThrowIfZeroOrPositive_WhenValueIsZeroOrPositive_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfZeroOrPositive(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Zero Or Positive, when Value Is Negative, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(-100)]
        [DataRow(int.MinValue)]
        public void ThrowIfZeroOrPositive_WhenValueIsNegative_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfZeroOrPositive(value);
        }
    }
}