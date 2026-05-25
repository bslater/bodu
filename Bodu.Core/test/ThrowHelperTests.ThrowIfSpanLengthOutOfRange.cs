// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthOutOfRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    // ReadOnlySpan<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, ReadOnlySpan, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]
    [DataRow(11, 1, 10)]
    [DataRow(5, 10, 20)]
    [DataRow(25, 10, 20)]
    public void ThrowIfSpanLengthOutOfRange_ReadOnlySpan_WhenLengthIsOutOfRange_ShouldThrowExactly(int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, ReadOnlySpan, when LengthIsWithinRange, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 10)]
    [DataRow(10, 1, 10)]
    [DataRow(5, 1, 10)]
    [DataRow(0, 0, 0)]
    [DataRow(7, 7, 7)]
    public void ThrowIfSpanLengthOutOfRange_ReadOnlySpan_WhenLengthIsWithinRange_ShouldNotThrow(int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength);
    }

    // Span<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, Span, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]
    [DataRow(11, 1, 10)]
    [DataRow(5, 10, 20)]
    [DataRow(25, 10, 20)]
    public void ThrowIfSpanLengthOutOfRange_Span_WhenLengthIsOutOfRange_ShouldThrowExactly(int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, Span, when LengthIsWithinRange, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 10)]
    [DataRow(10, 1, 10)]
    [DataRow(5, 1, 10)]
    [DataRow(0, 0, 0)]
    [DataRow(7, 7, 7)]
    public void ThrowIfSpanLengthOutOfRange_Span_WhenLengthIsWithinRange_ShouldNotThrow(int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange{T}(System.Span{T}, int, int, string)" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span length is within
    /// <c>[minLength, maxLength]</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    [TestMethod]
    [DataRow("at min", 1, 1, 10)]
    [DataRow("at max", 10, 1, 10)]
    [DataRow("inside", 5, 1, 10)]
    [DataRow("degenerate single value", 7, 7, 7)]
    public void ThrowIfSpanLengthOutOfRange_WhenLengthFits_ShouldNotThrowAndReportNothing(
        string testName, int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength, "span"),
            null,
            null);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength, "span"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange{T}(System.Span{T}, int, int, string)" />
    /// throws <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "span"</c> for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span length falls outside
    /// <c>[minLength, maxLength]</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    [TestMethod]
    [DataRow("below min", 0, 1, 10)]
    [DataRow("above max", 11, 1, 10)]
    public void ThrowIfSpanLengthOutOfRange_WhenLengthIsOutOfRange_ShouldThrowOnSpan(
        string testName, int spanLength, int minLength, int maxLength)
    {
        var buffer = new int[spanLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength, "span"),
            typeof(ArgumentOutOfRangeException),
            "span");

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength, "span"),
            typeof(ArgumentOutOfRangeException),
            "span");
    }

}
