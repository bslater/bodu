// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSequenceRangeOverflows.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Sequence Range Overflows, when Sum Exceeds Int Max, throws.
        /// </summary>
        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_WhenSumExceedsIntMax_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 1, 3));
        }

        /// <summary>
        /// Verifies that Throw If Sequence Range Overflows, when Sum Does Not Exceed Int Max, does not Throw.
        /// </summary>
        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_WhenSumDoesNotExceedIntMax_ShouldNotThrow()
        {
            ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 2, 2);
        }

        /// <summary>
        /// Verifies that Throw If Sequence Range Overflows, Long, when Sum Exceeds Long Max, throws.
        /// </summary>
        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_Long_WhenSumExceedsLongMax_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 1, 3));
        }

        /// <summary>
        /// Verifies that Throw If Sequence Range Overflows, Long, when Sum Does Not Exceed Long Max, does not Throw.
        /// </summary>
        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_Long_WhenSumDoesNotExceedLongMax_ShouldNotThrow()
        {
            ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 2, 2);
        }
    }
}