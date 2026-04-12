// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfIndexOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        private static readonly int[] TestArray = new[] { 1, 2, 3, 4, 5 };

        [TestMethod]
        [DataRow(5)] // index == Count
        [DataRow(6)] // index > Count
        [DataRow(int.MaxValue)]
        [DataRow(-1)] // negative index
        public void ThrowIfIndexOutOfRange_WhenIndexIsInvalid_ShouldThrowArgumentOutOfRangeException(int index)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfIndexOutOfRange(index, TestArray);
            });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(4)]
        public void ThrowIfIndexOutOfRange_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, TestArray);
        }
    }
}