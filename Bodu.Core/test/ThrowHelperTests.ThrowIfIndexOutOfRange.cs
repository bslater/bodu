// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfIndexOutOfRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    private static readonly int[] s_testArray = [1, 2, 3, 4, 5];

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> throws
    /// <see cref="ArgumentNullException" /> when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfIndexOutOfRange_Array_WhenArrayIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(0L, (Array)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_Array_WhenIndexIsInvalid_ShouldThrowExactly(int index)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, s_testArray);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> does not throw
    /// when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_Array_WhenIndexIsWithinBounds_ShouldNotThrow(int index) => ThrowHelper.ThrowIfIndexOutOfRange(index, s_testArray);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> does not throw —
    /// and on the ParamName-asserting overload reports nothing — when the index is within array bounds.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="index">The index passed to the guard.</param>
    [TestMethod]
    [DataRow("index in range", 2)]
    public void ThrowIfIndexOutOfRange_Array_WhenIndexFits_ShouldNotThrowAndReportNothing(string testName, int index) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfIndexOutOfRange(index, s_testArray, nameof(index)),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> throws
    /// <see cref="ArgumentNullException" /> (with <c>ParamName == "array"</c>) when the array is
    /// <see langword="null" />, or <see cref="ArgumentOutOfRangeException" /> (with <c>ParamName == "index"</c>)
    /// when the index is out of bounds — never just the first parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="index">The index passed to the guard.</param>
    /// <param name="arrayIsNull">If <see langword="true" />, null is passed for <c>array</c>.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    /// <param name="expectedParamName">The expected ParamName.</param>
    [TestMethod]
    [DataRow("null array → ANE on array", 0, true, "ArgumentNullException", "array")]
    [DataRow("negative index → AOORE on index", -1, false, "ArgumentOutOfRangeException", "index")]
    [DataRow("index == Length → AOORE on index", 5, false, "ArgumentOutOfRangeException", "index")]
    [DataRow("index > Length → AOORE on index", 99, false, "ArgumentOutOfRangeException", "index")]
    public void ThrowIfIndexOutOfRange_Array_WhenInputIsRejected_ShouldThrowOnExpectedParam(
        string testName, int index, bool arrayIsNull, string expectedExceptionTypeName, string expectedParamName)
    {
        Array? array = arrayIsNull ? null : s_testArray;
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfIndexOutOfRange(index, array!, nameof(index)),
            expected,
            expectedParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, ReadOnlySpan{T}, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_ReadOnlySpan_WhenIndexIsInvalid_ShouldThrowExactly(int index)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, new ReadOnlySpan<int>(s_testArray));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, ReadOnlySpan{T}, string?)" /> does
    /// not throw when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_ReadOnlySpan_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
    {
        ReadOnlySpan<int> span = s_testArray;

        ThrowHelper.ThrowIfIndexOutOfRange(index, span);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, Span{T}, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_Span_WhenIndexIsInvalid_ShouldThrowExactly(int index)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, new Span<int>([1, 2, 3, 4, 5]));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, Span{T}, string?)" /> does not
    /// throw when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_Span_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
    {
        Span<int> span = [1, 2, 3, 4, 5];

        ThrowHelper.ThrowIfIndexOutOfRange(index, span);
    }

}
