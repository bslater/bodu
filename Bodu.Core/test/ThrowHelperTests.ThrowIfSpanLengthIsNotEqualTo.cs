// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthIsNotEqualTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    // ReadOnlySpan<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, ReadOnlySpan, when LengthDiffers, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(3, 4)]
    [DataRow(5, 4)]
    public void ThrowIfSpanLengthIsNotEqualTo_ReadOnlySpan_WhenLengthDiffers_ShouldThrowExactly(int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, ReadOnlySpan, when LengthMatches, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(16, 16)]
    public void ThrowIfSpanLengthIsNotEqualTo_ReadOnlySpan_WhenLengthMatches_ShouldNotThrow(int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength);
    }

    // Span<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, Span, when LengthDiffers, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(3, 4)]
    [DataRow(5, 4)]
    public void ThrowIfSpanLengthIsNotEqualTo_Span_WhenLengthDiffers_ShouldThrowExactly(int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, Span, when LengthMatches, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(16, 16)]
    public void ThrowIfSpanLengthIsNotEqualTo_Span_WhenLengthMatches_ShouldNotThrow(int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo{T}(System.Span{T}, int, string)" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span length matches the expected
    /// length exactly.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="expectedLength">The required exact length.</param>
    [TestMethod]
    [DataRow("exact match", 4, 4)]
    [DataRow("both empty", 0, 0)]
    public void ThrowIfSpanLengthIsNotEqualTo_WhenLengthMatches_ShouldNotThrowAndReportNothing(
        string testName, int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength, "span"),
            null,
            null);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength, "span"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo{T}(System.Span{T}, int, string)" />
    /// throws <see cref="ArgumentException" /> with <c>ParamName == "span"</c> for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span length differs from the
    /// expected length.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="expectedLength">The required exact length.</param>
    [TestMethod]
    [DataRow("shorter", 3, 4)]
    [DataRow("longer", 5, 4)]
    [DataRow("empty vs nonzero", 0, 4)]
    public void ThrowIfSpanLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowOnSpan(
        string testName, int spanLength, int expectedLength)
    {
        int[] buffer = new int[spanLength];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength, "span"),
            typeof(ArgumentException),
            "span");

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength, "span"),
            typeof(ArgumentException),
            "span");
    }

}
