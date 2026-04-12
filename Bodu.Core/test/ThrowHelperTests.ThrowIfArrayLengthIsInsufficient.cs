// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsInsufficient.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(5, 2, 5)]  // 2 + 5 = 7 > 5 => insufficient
        [DataRow(4, 0, 5)]  // 0 + 5 = 5 > 4 => insufficient
        [DataRow(3, 1, 3)]  // 1 + 3 = 4 > 3 => insufficient
        public void ThrowIfArrayLengthIsInsufficient_WhenArrayTooShort_ShouldThrowExactly(int arrayLength, int offset, int count)
        {
            var array = new int[arrayLength];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, count);
            });
        }

        [TestMethod]
        [DataRow(10, 2, 5)]  // 2 + 5 = 7 <= 10 => sufficient
        [DataRow(5, 0, 5)]   // 0 + 5 = 5 <= 5 => sufficient
        [DataRow(8, 3, 5)]   // 3 + 5 = 8 <= 8 => sufficient
        public void ThrowIfArrayLengthIsInsufficient_WhenArrayIsSufficient_ShouldNotThrow(int arrayLength, int offset, int count)
        {
            var array = new int[arrayLength];
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, count);
        }
    }
}