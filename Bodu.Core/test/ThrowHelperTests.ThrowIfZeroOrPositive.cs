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
        /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive" />, when ValueIsZeroOrPositive, throws <see cref="ArgumentOutOfRangeException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive" />, when ValueIsNegative, NotThrow.
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