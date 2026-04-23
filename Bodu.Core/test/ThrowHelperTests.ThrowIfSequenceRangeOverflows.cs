// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSequenceRangeOverflows.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, when SumExceedsIntMax, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_WhenSumExceedsIntMax_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 1, 3));
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, when SumDoesNotExceedIntMax, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_WhenSumDoesNotExceedIntMax_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 2, 2);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, Long, when SumExceedsLongMax, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_Long_WhenSumExceedsLongMax_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 1, 3));
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, Long, when SumDoesNotExceedLongMax, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_Long_WhenSumDoesNotExceedLongMax_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 2, 2);
    }
}
