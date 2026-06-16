// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayOffsetOrCountInvalid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public sealed partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for valid <c>(array, offset, count)</c> triples
    /// whose window fits inside the backing array.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    [TestMethod]
    [DataRow("valid window at start", 5, 0, 5)]
    [DataRow("valid window in middle", 5, 2, 3)]
    [DataRow("valid empty slice at end", 5, 5, 0)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenInputIsAccepted_ShouldNotThrowAndReportNothing(
        string testName, int arrayLength, int offset, int count)
    {
        Array array = new int[arrayLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count, "array", "offset", "count"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" /> throws the expected
    /// exception type with the expected <c>ParamName</c> — disambiguating across <c>array</c>,
    /// <c>offset</c>, and <c>count</c> — for null arrays and out-of-range offset/count values. This is the
    /// critical disambiguation contract for multi-parameter guards.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array, or <c>-1</c> to pass <see langword="null" />.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />, or empty if not asserted (used when the implementation does not designate a single parameter).</param>
    [TestMethod]
    [DataRow("null array → ArgumentNullException on array", -1, 0, 0, "ArgumentNullException", "array")]
    [DataRow("negative offset → AOORE on offset", 5, -1, 0, "ArgumentOutOfRangeException", "offset")]
    [DataRow("offset > array.Length → AOORE on offset", 5, 6, 0, "ArgumentOutOfRangeException", "offset")]
    [DataRow("negative count → AOORE on count", 5, 0, -1, "ArgumentOutOfRangeException", "count")]
    [DataRow("count > array.Length → AOORE on count", 5, 0, 6, "ArgumentOutOfRangeException", "count")]
    [DataRow("offset+count > array.Length → ArgumentException", 5, 2, 5, "ArgumentException", "")]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenInputIsRejected_ShouldThrowOnExpectedParam(
        string testName, int arrayLength, int offset, int count, string expectedExceptionTypeName, string expectedParamName)
    {
        Array? array = arrayLength < 0 ? null : new int[arrayLength];
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        string? param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array!, offset, count, "array", "offset", "count"),
            expected,
            param);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]  // Negative offset
    [DataRow(6, 2)]   // Offset > array length
    [DataRow(2, -1)]  // Negative count
    [DataRow(2, 10)]  // Count > array length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenOffsetOrCountOutOfRange_ShouldThrowExactly(int offset, int count)
    {
        int[] array = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(3, 2)]
    [DataRow(4, 1)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        int[] array = new int[5];
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]  // Offset + count exceeds array length
    [DataRow(4, 2)]  // Offset + count exceeds array length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenSumExceedsLength_ShouldThrowExactly(int offset, int count)
    {
        int[] array = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

}
