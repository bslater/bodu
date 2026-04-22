// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfPositive.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Positive, when Value Is Positive, throws.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(42)]
        [DataRow(int.MaxValue)]
        public void ThrowIfPositive_WhenValueIsPositive_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfPositive(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Positive, when Value Is Zero Or Negative, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void ThrowIfPositive_WhenValueIsZeroOrNegative_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfPositive(value);
        }
    }
}