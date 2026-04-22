// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCountExceedsAvailable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" />, when CountIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        [DataRow(6, 5)]   // Count exceeds available
        [DataRow(-1, 5)]  // Count is negative
        [DataRow(-10, 0)] // Count is negative regardless of available
        public void ThrowIfCountExceedsAvailable_WhenCountIsInvalid_ShouldThrowArgumentOutOfRangeException(int count, int available)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfCountExceedsAvailable(count, available);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" />, when CountIsValid, NotThrow.
        /// </summary>
        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(0, 5)]
        [DataRow(3, 5)]
        [DataRow(5, 5)]
        public void ThrowIfCountExceedsAvailable_WhenCountIsValid_ShouldNotThrow(int count, int available)
        {
            ThrowHelper.ThrowIfCountExceedsAvailable(count, available);
        }
    }
}