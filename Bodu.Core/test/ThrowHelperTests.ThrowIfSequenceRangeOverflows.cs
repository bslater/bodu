// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSequenceRangeOverflows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows(int, int, string)" /> does not
    /// throw — and on the ParamName-asserting overload reports nothing — for non-overflowing ranges,
    /// boundary-equal endpoints, and the <c>count &lt;= 0</c> short-circuit.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of elements in the sequence.</param>
    [TestMethod]
    [DataRow("start=0 count=0", 0, 0)]
    [DataRow("start=0 count=1", 0, 1)]
    [DataRow("start=MaxValue-1 count=1 lands on MaxValue", int.MaxValue - 1, 1)]
    [DataRow("start=MaxValue count=0 (count == 0 short-circuit)", int.MaxValue, 0)]
    [DataRow("start=MaxValue count=1 lands on MaxValue", int.MaxValue, 1)]
    [DataRow("count=-5 with positive start (count <= 0 short-circuit)", 1000, -5)]
    public void ThrowIfSequenceRangeOverflows_Int32_WhenRangeFits_ShouldNotThrowAndReportNothing(string testName, int start, int count) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfSequenceRangeOverflows(start, count, nameof(count)),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSequenceRangeOverflows(int, int, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "count"</c> for ranges that overflow
    /// <see cref="int.MaxValue" />.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of elements in the sequence.</param>
    [TestMethod]
    [DataRow("start=MaxValue-10 count=12 overflows", int.MaxValue - 10, 12)]
    [DataRow("start=MaxValue-2 count=4 overflows", int.MaxValue - 2, 4)]
    [DataRow("start=MaxValue count=2 overflows", int.MaxValue, 2)]
    public void ThrowIfSequenceRangeOverflows_Int32_WhenRangeOverflows_ShouldThrowOnCount(string testName, int start, int count) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfSequenceRangeOverflows(start, count, nameof(count)),
            typeof(ArgumentOutOfRangeException),
            "count");

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
