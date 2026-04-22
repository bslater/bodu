// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Array Length Out Of Range, when Array Is Null, throws Argument Null Exception.
        /// </summary>
        [TestMethod]
        public void ThrowIfArrayLengthOutOfRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            Array? array = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfArrayLengthOutOfRange(array!, 1, 10);
            });
        }

        /// <summary>
        /// Verifies that Throw If Array Length Out Of Range, when Length Is Out Of Range, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(0, 1, 10)]    // below min
        [DataRow(11, 1, 10)]   // above max
        [DataRow(5, 10, 20)]   // below min
        [DataRow(25, 10, 20)]  // above max
        public void ThrowIfArrayLengthOutOfRange_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int arrayLength, int minLength, int maxLength)
        {
            Array array = new int[arrayLength];
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfArrayLengthOutOfRange(array, minLength, maxLength);
            });
        }

        /// <summary>
        /// Verifies that Throw If Array Length Out Of Range, when Length Is Within Range, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(1, 1, 10)]    // at min
        [DataRow(10, 1, 10)]   // at max
        [DataRow(5, 1, 10)]    // inside range
        [DataRow(0, 0, 0)]     // degenerate equal min and max
        [DataRow(7, 7, 7)]     // degenerate, only one valid length
        public void ThrowIfArrayLengthOutOfRange_WhenLengthIsWithinRange_ShouldNotThrow(int arrayLength, int minLength, int maxLength)
        {
            Array array = new int[arrayLength];
            ThrowHelper.ThrowIfArrayLengthOutOfRange(array, minLength, maxLength);
        }
    }
}
