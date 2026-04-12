// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOrEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(5, 5)]
        [DataRow(6, 5)]
        [DataRow(1, 0)]
        [DataRow(0, 0)]
        [DataRow(int.MaxValue, int.MaxValue)]
        public void ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldThrowArgumentOutOfRangeException(int value, int max)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfGreaterThanOrEqual(value, max);
            });
        }

        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(4, 5)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void ThrowIfGreaterThanOrEqual_WhenValueIsLessThanMax_ShouldNotThrow(int value, int max)
        {
            ThrowHelper.ThrowIfGreaterThanOrEqual(value, max);
        }
    }
}