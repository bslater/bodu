// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfAddOverflows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfAddOverflows(int, int, string)" /> does not throw — and reports
    /// nothing on the ParamName-asserting overload — when the sum stays within the <see cref="int" /> range,
    /// including the boundary sums and negative underflow boundary.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="left">The left addend.</param>
    /// <param name="right">The right addend.</param>
    [TestMethod]
    [DataRow("0 + 0", 0, 0)]
    [DataRow("MaxValue-1 + 1 lands on MaxValue", int.MaxValue - 1, 1)]
    [DataRow("MaxValue + 0", int.MaxValue, 0)]
    [DataRow("MinValue + 0", int.MinValue, 0)]
    [DataRow("MinValue+1 + -1 lands on MinValue", int.MinValue + 1, -1)]
    public void ThrowIfAddOverflows_Int32_WhenSumFits_ShouldNotThrowAndReportNothing(string testName, int left, int right) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfAddOverflows(left, right, nameof(left)),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfAddOverflows(int, int, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "left"</c> when the sum overflows or
    /// underflows the <see cref="int" /> range.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="left">The left addend.</param>
    /// <param name="right">The right addend.</param>
    [TestMethod]
    [DataRow("MaxValue + 1 overflows", int.MaxValue, 1)]
    [DataRow("MaxValue-2 + 4 overflows", int.MaxValue - 2, 4)]
    [DataRow("MinValue + -1 underflows", int.MinValue, -1)]
    public void ThrowIfAddOverflows_Int32_WhenSumOverflows_ShouldThrowOnLeft(string testName, int left, int right) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfAddOverflows(left, right, nameof(left)),
            typeof(ArgumentOutOfRangeException),
            "left");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfAddOverflows(long, long, string)" /> does not throw when the
    /// sum stays within the <see cref="long" /> range.
    /// </summary>
    [TestMethod]
    public void ThrowIfAddOverflows_Int64_WhenSumFits_ShouldNotThrow() =>
        ThrowHelper.ThrowIfAddOverflows(long.MaxValue - 2, 2);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfAddOverflows(long, long, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the sum overflows the <see cref="long" /> range.
    /// </summary>
    [TestMethod]
    public void ThrowIfAddOverflows_Int64_WhenSumOverflows_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfAddOverflows(long.MaxValue - 1, 3));

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfAddOverflows(long, long, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the sum underflows the <see cref="long" /> range.
    /// </summary>
    [TestMethod]
    public void ThrowIfAddOverflows_Int64_WhenSumUnderflows_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfAddOverflows(long.MinValue + 1, -3));
}
