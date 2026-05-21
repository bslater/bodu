// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthInvalid_ShouldThrow(int length, int factor)
    {
        var span = new int[length];
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
    public void ThrowIfSpanLengthNotPositiveMultipleOf_Span_WhenLengthInvalid_ShouldThrow(int length, int factor)
    {
        var span = new int[length];
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
    /// Verifies the <see cref="ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf{T}(System.Span{T}, int, string)" />
    /// contract for both <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" />: a zero-length span or a
    /// length that is not a positive multiple of the divisor throws <see cref="ArgumentException" /> with
    /// ParamName "span"; valid positive multiples pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="length">The span length.</param>
    /// <param name="divisor">The required divisor.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("zero length → throw on span", 0, 4, true)]
    [DataRow("not a multiple → throw on span", 5, 2, true)]
    [DataRow("not a multiple of 3 → throw on span", 7, 3, true)]
    [DataRow("exact divisor → pass", 4, 4, false)]
    [DataRow("multiple → pass", 12, 4, false)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenInvokedWithVariousLengths_ShouldFollowContract(
        string testName, int length, int divisor, bool expectsException)
    {
        var buffer = new int[length];
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "span" : null;

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(buffer.AsSpan(), divisor, paramName: "span"),
            expected,
            expectedParam);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf((ReadOnlySpan<int>)buffer, divisor, paramName: "span"),
            expected,
            expectedParam);
    }

}
