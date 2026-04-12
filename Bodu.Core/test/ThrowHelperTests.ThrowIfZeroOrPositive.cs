// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrPositive.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
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