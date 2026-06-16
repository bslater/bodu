// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthNotPositiveMultipleOf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf" />, ReadOnlySpan, when LengthInvalid, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2)]  // Not a multiple
    [DataRow(0, 1)]  // Zero length
    [DataRow(7, 3)]  // Not a multiple
    public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthInvalid_ShouldThrowExactly(int length, int factor)
    {
        int[] span = new int[length];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(new ReadOnlySpan<int>(span), factor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf" />, ReadOnlySpan, when LengthValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(6, 3)]
    [DataRow(4, 2)]
    [DataRow(8, 4)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthValid_ShouldNotThrow(int length, int factor)
    {
        ReadOnlySpan<int> span = new int[length];
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span, factor);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf" />, Span, when LengthInvalid, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2)]
    [DataRow(0, 1)]
    [DataRow(7, 3)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_Span_WhenLengthInvalid_ShouldThrowExactly(int length, int factor)
    {
        int[] span = new int[length];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span.AsSpan(), factor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf" />, Span, when LengthValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(6, 3)]
    [DataRow(4, 2)]
    [DataRow(8, 4)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_Span_WhenLengthValid_ShouldNotThrow(int length, int factor)
    {
        Span<int> span = new int[length];
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span, factor);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf{T}(System.Span{T}, int, string)" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span length is a positive multiple
    /// of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="length">The span length.</param>
    /// <param name="divisor">The required divisor.</param>
    [TestMethod]
    [DataRow("exact divisor", 4, 4)]
    [DataRow("multiple", 12, 4)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenLengthIsValidMultiple_ShouldNotThrowAndReportNothing(
        string testName, int length, int divisor)
    {
        int[] buffer = new int[length];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(buffer.AsSpan(), divisor, paramName: "span"),
            null,
            null);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf((ReadOnlySpan<int>)buffer, divisor, paramName: "span"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf{T}(System.Span{T}, int, string)" />
    /// throws <see cref="ArgumentException" /> with <c>ParamName == "span"</c> for both
    /// <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> when the span is zero-length or its length
    /// is not a positive multiple of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="length">The span length.</param>
    /// <param name="divisor">The required divisor.</param>
    [TestMethod]
    [DataRow("zero length", 0, 4)]
    [DataRow("not a multiple", 5, 2)]
    [DataRow("not a multiple of 3", 7, 3)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenLengthIsInvalid_ShouldThrowOnSpan(
        string testName, int length, int divisor)
    {
        int[] buffer = new int[length];

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(buffer.AsSpan(), divisor, paramName: "span"),
            typeof(ArgumentException),
            "span");

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf((ReadOnlySpan<int>)buffer, divisor, paramName: "span"),
            typeof(ArgumentException),
            "span");
    }

}
