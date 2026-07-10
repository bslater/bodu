// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfMultiplyOverflows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfMultiplyOverflows(int, int, string)" /> does not throw — and
    /// reports nothing on the ParamName-asserting overload — when the product stays within the <see cref="int" />
    /// range, including the largest fitting square and multiplications by zero.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="left">The left factor.</param>
    /// <param name="right">The right factor.</param>
    [TestMethod]
    [DataRow("0 * MaxValue", 0, int.MaxValue)]
    [DataRow("MaxValue * 1", int.MaxValue, 1)]
    [DataRow("MinValue * 1", int.MinValue, 1)]
    [DataRow("46340 * 46340 fits", 46340, 46340)]
    [DataRow("-46340 * 46340 fits", -46340, 46340)]
    public void ThrowIfMultiplyOverflows_Int32_WhenProductFits_ShouldNotThrowAndReportNothing(string testName, int left, int right) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfMultiplyOverflows(left, right, nameof(left)),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfMultiplyOverflows(int, int, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "left"</c> when the product overflows the
    /// <see cref="int" /> range, including the <c>MinValue * -1</c> magnitude overflow.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="left">The left factor.</param>
    /// <param name="right">The right factor.</param>
    [TestMethod]
    [DataRow("MaxValue * 2 overflows", int.MaxValue, 2)]
    [DataRow("65536 * 65536 overflows", 65536, 65536)]
    [DataRow("46341 * 46341 overflows", 46341, 46341)]
    [DataRow("MinValue * -1 overflows", int.MinValue, -1)]
    public void ThrowIfMultiplyOverflows_Int32_WhenProductOverflows_ShouldThrowOnLeft(string testName, int left, int right) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfMultiplyOverflows(left, right, nameof(left)),
            typeof(ArgumentOutOfRangeException),
            "left");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfMultiplyOverflows(long, long, string)" /> does not throw when
    /// the product stays within the <see cref="long" /> range.
    /// </summary>
    [TestMethod]
    public void ThrowIfMultiplyOverflows_Int64_WhenProductFits_ShouldNotThrow() =>
        ThrowHelper.ThrowIfMultiplyOverflows(3_037_000_499L, 3_037_000_499L);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfMultiplyOverflows(long, long, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the product overflows the <see cref="long" /> range.
    /// </summary>
    [TestMethod]
    public void ThrowIfMultiplyOverflows_Int64_WhenProductOverflows_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfMultiplyOverflows(long.MaxValue, 2L));

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfMultiplyOverflows(long, long, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for the <c>MinValue * -1</c> magnitude overflow.
    /// </summary>
    [TestMethod]
    public void ThrowIfMultiplyOverflows_Int64_WhenMinValueTimesNegativeOne_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfMultiplyOverflows(long.MinValue, -1L));
}
