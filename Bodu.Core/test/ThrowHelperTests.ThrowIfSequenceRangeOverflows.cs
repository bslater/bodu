// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSequenceRangeOverflows_Int.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_WhenSumExceedsIntMax_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 1, 3));
        }

        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_WhenSumDoesNotExceedIntMax_ShouldNotThrow()
        {
            ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 2, 2);
        }

        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_Long_WhenSumExceedsLongMax_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 1, 3));
        }

        [TestMethod]
        public void ThrowIfSequenceRangeOverflows_Long_WhenSumDoesNotExceedLongMax_ShouldNotThrow()
        {
            ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 2, 2);
        }
    }
}