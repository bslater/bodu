// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthIsNotEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo{T}(System.Span{T}, int, string)" />
    /// contract for both <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" />: length mismatch throws
    /// <see cref="ArgumentException" /> with ParamName "span"; exact match passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">The span length.</param>
    /// <param name="expectedLength">The required exact length.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("shorter → throw on span", 3, 4, true)]
    [DataRow("longer → throw on span", 5, 4, true)]
    [DataRow("empty vs nonzero → throw on span", 0, 4, true)]
    [DataRow("exact match → pass", 4, 4, false)]
    [DataRow("both empty → pass", 0, 0, false)]
    public void ThrowIfSpanLengthIsNotEqualTo_WhenInvokedWithVariousLengths_ShouldFollowContract(
        string testName, int spanLength, int expectedLength, bool expectsException)
    {
        var buffer = new int[spanLength];
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "span" : null;

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength, "span"),
            expected,
            expectedParam);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength, "span"),
            expected,
            expectedParam);
    }

    // Span<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, Span, when LengthDiffers, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(3, 4)]
    [DataRow(5, 4)]
    public void ThrowIfSpanLengthIsNotEqualTo_Span_WhenLengthDiffers_ShouldThrowArgumentException(int spanLength, int expectedLength)
    {
        var buffer = new int[spanLength];
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
        var buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength);
    }

    // ReadOnlySpan<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsNotEqualTo" />, ReadOnlySpan, when LengthDiffers, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(3, 4)]
    [DataRow(5, 4)]
    public void ThrowIfSpanLengthIsNotEqualTo_ReadOnlySpan_WhenLengthDiffers_ShouldThrowArgumentException(int spanLength, int expectedLength)
    {
        var buffer = new int[spanLength];
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
        var buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength);
    }
}
