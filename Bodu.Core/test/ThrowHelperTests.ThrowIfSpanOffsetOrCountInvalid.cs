// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanOffsetOrCountInvalid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    // ReadOnlySpan<T> overload (canonical implementation)

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]
    [DataRow(6, 2)]
    [DataRow(2, -1)]
    [DataRow(2, 10)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenOffsetOrCountOutOfRange_ShouldThrowExactly(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(5, 0)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        var buffer = new int[5];
        ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(4, 2)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenSumExceedsLength_ShouldThrowExactly(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid{T}(System.Span{T}, int, int, string, string, string)" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the <c>(offset, count)</c> window
    /// fits inside the buffer.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="bufferLength">The backing buffer length.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    [TestMethod]
    [DataRow("valid window at start", 5, 0, 5)]
    [DataRow("valid window in middle", 5, 2, 3)]
    [DataRow("valid empty slice at end", 5, 5, 0)]
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenInputIsAccepted_ShouldNotThrowAndReportNothing(
        string testName, int bufferLength, int offset, int count)
    {
        var buffer = new int[bufferLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count, "span", "offset", "count"),
            null,
            null);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count, "span", "offset", "count"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid{T}(System.Span{T}, int, int, string, string, string)" />
    /// throws the expected exception type with the expected <c>ParamName</c> — disambiguating across
    /// <c>span</c>, <c>offset</c>, and <c>count</c> — for both <see cref="Span{T}" /> and
    /// <see cref="ReadOnlySpan{T}" /> when offset or count is out of range. Spans are value types so there
    /// is no null branch.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="bufferLength">The backing buffer length.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("negative offset → AOORE on offset", 5, -1, 0, "ArgumentOutOfRangeException", "offset")]
    [DataRow("offset > length → AOORE on offset", 5, 6, 0, "ArgumentOutOfRangeException", "offset")]
    [DataRow("negative count → AOORE on count", 5, 0, -1, "ArgumentOutOfRangeException", "count")]
    [DataRow("count > length → AOORE on count", 5, 0, 6, "ArgumentOutOfRangeException", "count")]
    [DataRow("offset+count > length → ArgumentException", 5, 2, 5, "ArgumentException", "")]
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenInputIsRejected_ShouldThrowOnExpectedParam(
        string testName, int bufferLength, int offset, int count, string expectedExceptionTypeName, string expectedParamName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;
        var buffer = new int[bufferLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count, "span", "offset", "count"),
            expected,
            param);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count, "span", "offset", "count"),
            expected,
            param);
    }

    // Span<T> overload (delegates to ReadOnlySpan<T>)

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]   // negative offset
    [DataRow(6, 2)]    // offset > length
    [DataRow(2, -1)]   // negative count
    [DataRow(2, 10)]   // count > length
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenOffsetOrCountOutOfRange_ShouldThrowExactly(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(5, 0)]   // offset at length with zero count is valid
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        var buffer = new int[5];
        ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]    // offset + count > length
    [DataRow(4, 2)]    // offset + count > length
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenSumExceedsLength_ShouldThrowExactly(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
        });
    }

}
