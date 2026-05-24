// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSequenceRangeOverflows.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the boundary matrix for <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows(int, int, string)" />:
    /// non-overflowing ranges, the exact <c>int.MaxValue</c> endpoint, overflowing ranges, the
    /// <c>count == 0</c> no-op, and negative-count rows that the implementation must treat as a no-op.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of elements in the sequence.</param>
    /// <param name="expectExceptionType">The exception type the guard must throw, or <see cref="string.Empty" /> for no throw.</param>
    [TestMethod]
    [DataRow("start=0 count=0 → pass", 0, 0, "")]
    [DataRow("start=0 count=1 → pass", 0, 1, "")]
    [DataRow("start=MaxValue-1 count=1 lands on MaxValue → pass", int.MaxValue - 1, 1, "")]
    [DataRow("start=MaxValue count=0 → pass (count == 0 short-circuit)", int.MaxValue, 0, "")]
    [DataRow("start=MaxValue count=1 lands on MaxValue → pass", int.MaxValue, 1, "")]
    [DataRow("start=MaxValue-10 count=12 overflows → throw", int.MaxValue - 10, 12, "ArgumentOutOfRangeException")]
    [DataRow("start=MaxValue-2 count=4 overflows → throw", int.MaxValue - 2, 4, "ArgumentOutOfRangeException")]
    [DataRow("start=MaxValue count=2 overflows → throw", int.MaxValue, 2, "ArgumentOutOfRangeException")]
    [DataRow("count=-5 with positive start → pass (count <= 0 short-circuit)", 1000, -5, "")]
    public void ThrowIfSequenceRangeOverflows_Int32_WhenInvokedWithBoundaryValues_ShouldFollowContract(
        string testName, int start, int count, string expectExceptionType)
    {
        Type? expected = expectExceptionType.Length == 0
            ? null
            : Type.GetType($"System.{expectExceptionType}, System.Private.CoreLib")
              ?? throw new InvalidOperationException($"Unknown exception type '{expectExceptionType}'.");

        var expectedParam = expected is null ? null : "count";

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfSequenceRangeOverflows(start, count, nameof(count)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, Long, when SumDoesNotExceedLongMax, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_Long_WhenSumDoesNotExceedLongMax_ShouldNotThrow() => ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 2, 2);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, Long, when SumExceedsLongMax, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_Long_WhenSumExceedsLongMax_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(long.MaxValue - 1, 3));

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, when SumDoesNotExceedIntMax, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_WhenSumDoesNotExceedIntMax_ShouldNotThrow() => ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 2, 2);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows" />, when SumExceedsIntMax, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfSequenceRangeOverflows_WhenSumExceedsIntMax_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfSequenceRangeOverflows(int.MaxValue - 1, 3));

}
