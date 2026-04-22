// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrNegative.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsZeroOrNegative, throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void ThrowIfZeroOrNegative_WhenValueIsZeroOrNegative_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfZeroOrNegative(value);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsPositive, NotThrow.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(42)]
        [DataRow(int.MaxValue)]
        public void ThrowIfZeroOrNegative_WhenValueIsPositive_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);
        }
    }
}