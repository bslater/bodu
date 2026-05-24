// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthIsInsufficient.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient{T}(ReadOnlySpan{T}, int, string)" /> throws
    /// <see cref="ArgumentException" /> when the span is shorter than the minimum length.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(2, 3)]
    [DataRow(4, 100)]
    public void ThrowIfSpanLengthIsInsufficient_ReadOnlySpan_TwoArg_WhenLengthIsInsufficient_ShouldThrowExactly(int spanLength, int minimum)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<int> span = new int[spanLength];
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, minimum);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient{T}(ReadOnlySpan{T}, int, string)" /> does not throw when the
    /// span contains at least the minimum number of elements.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(5, 3)]
    [DataRow(10, 0)]
    public void ThrowIfSpanLengthIsInsufficient_ReadOnlySpan_TwoArg_WhenLengthIsSufficient_ShouldNotThrow(int spanLength, int minimum)
    {
        ReadOnlySpan<int> span = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, minimum);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, ReadOnlySpan, when Insufficient, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2, 5)]   // span.Length = 5; offset = 2; count = 5 => insufficient
    [DataRow(4, 0, 5)]   // span.Length = 4; offset = 0; count = 5 => insufficient
    public void ThrowIfSpanLengthIsInsufficient_ReadOnlySpan_WhenInsufficient_ShouldThrowExactly(int spanLength, int offset, int count)
    {
        var span = new int[spanLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(new ReadOnlySpan<int>(span), offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, ReadOnlySpan, when Sufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(10, 2, 5)]   // span.Length = 10; offset + count = 7 <= 10
    [DataRow(6, 0, 6)]    // offset 0 + count 6 = 6 == span.Length
    [DataRow(5, 0, 0)]    // offset 0 + count 0 = 0 <= span.Length
    public void ThrowIfSpanLengthIsInsufficient_ReadOnlySpan_WhenSufficient_ShouldNotThrow(int spanLength, int offset, int count)
    {
        ReadOnlySpan<int> span = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, offset, count);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient{T}(Span{T}, int, string)" /> throws
    /// <see cref="ArgumentException" /> when the span is shorter than the minimum length.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(2, 3)]
    [DataRow(4, 100)]
    public void ThrowIfSpanLengthIsInsufficient_Span_TwoArg_WhenLengthIsInsufficient_ShouldThrowExactly(int spanLength, int minimum)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<int> span = new int[spanLength];
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, minimum);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient{T}(Span{T}, int, string)" /> does not throw when the span
    /// contains at least the minimum number of elements.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(5, 3)]
    [DataRow(10, 0)]
    public void ThrowIfSpanLengthIsInsufficient_Span_TwoArg_WhenLengthIsSufficient_ShouldNotThrow(int spanLength, int minimum)
    {
        Span<int> span = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, minimum);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, Span, when Insufficient, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2, 5)]
    [DataRow(4, 1, 4)]
    public void ThrowIfSpanLengthIsInsufficient_Span_WhenInsufficient_ShouldThrowExactly(int spanLength, int offset, int count)
    {
        var span = new int[spanLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(span.AsSpan(), offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, Span, when Sufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(10, 2, 5)]
    [DataRow(6, 0, 6)]
    [DataRow(7, 3, 4)]
    public void ThrowIfSpanLengthIsInsufficient_Span_WhenSufficient_ShouldNotThrow(int spanLength, int offset, int count)
    {
        Span<int> span = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(span, offset, count);
    }
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient{T}(System.Span{T}, int, string)" />
    /// two-argument overload contract for both <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" />: a span
    /// shorter than the minimum throws <see cref="ArgumentException" /> with ParamName "span"; equal or longer
    /// passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="spanLength">Span length.</param>
    /// <param name="minimum">Required minimum length.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("shorter than minimum → throw on span", 2, 3, true)]
    [DataRow("empty span, positive minimum → throw on span", 0, 1, true)]
    [DataRow("exactly minimum → pass", 3, 3, false)]
    [DataRow("longer than minimum → pass", 10, 3, false)]
    [DataRow("any length, minimum 0 → pass", 5, 0, false)]
    public void ThrowIfSpanLengthIsInsufficient_TwoArg_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int spanLength, int minimum, bool expectsException)
    {
        var buffer = new int[spanLength];
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "span" : null;

        AssertGuard(
            $"Span<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsInsufficient(buffer.AsSpan(), minimum, "span"),
            expected,
            expectedParam);

        AssertGuard(
            $"ReadOnlySpan<T>: {testName}",
            () => ThrowHelper.ThrowIfSpanLengthIsInsufficient((ReadOnlySpan<int>)buffer, minimum, "span"),
            expected,
            expectedParam);
    }

}
