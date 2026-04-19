// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsNotEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        public void ThrowIfArrayLengthIsNotEqualTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            Array? array = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, 4);
            });
        }

        [TestMethod]
        [DataRow(0, 4)]
        [DataRow(3, 4)]
        [DataRow(5, 4)]
        [DataRow(10, 1)]
        public void ThrowIfArrayLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowArgumentException(int arrayLength, int expectedLength)
        {
            Array array = new int[arrayLength];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength);
            });
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 4)]
        [DataRow(16, 16)]
        public void ThrowIfArrayLengthIsNotEqualTo_WhenLengthMatches_ShouldNotThrow(int arrayLength, int expectedLength)
        {
            Array array = new int[arrayLength];
            ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength);
        }
    }
}
