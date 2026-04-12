// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotZero.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(1)]
        [DataRow(-1)]
        [DataRow(int.MaxValue)]
        [DataRow(int.MinValue)]
        public void ThrowIfNotZero_WhenValueIsNotZero_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfNotZero(value);
            });
        }

        [TestMethod]
        [DataRow(0)]
        public void ThrowIfNotZero_WhenValueIsZero_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfNotZero(value);
        }
    }
}