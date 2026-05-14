// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange{T}(System.Span{T}, int, int, string)" />
    /// contract for both <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" />: length below min or above
    /// max throws <see cref="ArgumentOutOfRangeException" /> with ParamName "span"; in-range passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("below min → throw on span", 0, 1, 10, true)]
    [DataRow("above max → throw on span", 11, 1, 10, true)]
    [DataRow("at min → pass", 1, 1, 10, false)]
    [DataRow("at max → pass", 10, 1, 10, false)]
    [DataRow("inside → pass", 5, 1, 10, false)]
    [DataRow("degenerate single value → pass", 7, 7, 7, false)]
    public void ThrowIfSpanLengthOutOfRange_WhenInvokedWithVariousLengths_ShouldFollowContract(
        string testName, int spanLength, int minLength, int maxLength, bool expectsException)
    {
        var buffer = new int[spanLength];
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "span" : null;

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength, "span"),
            expected,
            expectedParam);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength, "span"),
            expected,
            expectedParam);
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
    public void ThrowIfSpanLengthOutOfRange_Span_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int spanLength, int minLength, int maxLength)
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

    // ReadOnlySpan<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, ReadOnlySpan, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]
    [DataRow(11, 1, 10)]
    [DataRow(5, 10, 20)]
    [DataRow(25, 10, 20)]
    public void ThrowIfSpanLengthOutOfRange_ReadOnlySpan_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int spanLength, int minLength, int maxLength)
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
}
