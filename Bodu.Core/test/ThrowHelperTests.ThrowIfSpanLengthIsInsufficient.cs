// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthIsInsufficient.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, ReadOnlySpan, when Insufficient, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2, 5)]   // span.Length = 5; offset = 2; count = 5 => insufficient
    [DataRow(4, 0, 5)]   // span.Length = 4; offset = 0; count = 5 => insufficient
    public void ThrowIfSpanLengthIsInsufficient_ReadOnlySpan_WhenInsufficient_ShouldThrow(int spanLength, int offset, int count)
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
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthIsInsufficient" />, Span, when Insufficient, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2, 5)]
    [DataRow(4, 1, 4)]
    public void ThrowIfSpanLengthIsInsufficient_Span_WhenInsufficient_ShouldThrow(int spanLength, int offset, int count)
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
}
