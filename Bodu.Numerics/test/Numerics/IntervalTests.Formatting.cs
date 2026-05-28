// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that <see cref="Interval{T}.ToString()" /> produces the ISO 31-11 bracket notation appropriate to
    /// each combination of endpoint inclusivity.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNonEmpty_ShouldEmitIso31Dash11Brackets()
    {
        Assert.AreEqual("[1, 5]", Interval<int>.Closed(1, 5).ToString());
        Assert.AreEqual("(1, 5)", Interval<int>.Open(1, 5).ToString());
        Assert.AreEqual("[1, 5)", Interval<int>.ClosedOpen(1, 5).ToString());
        Assert.AreEqual("(1, 5]", Interval<int>.OpenClosed(1, 5).ToString());
    }

    /// <summary>
    /// Verifies that empty intervals format to the U+2205 EMPTY SET glyph.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEmpty_ShouldEmitEmptySetGlyph()
    {
        Assert.AreEqual("∅", Interval<int>.Empty.ToString());
        Assert.AreEqual("∅", new Interval<int>(5, 1, true, true).ToString());
    }

    /// <summary>
    /// Verifies that the format specifier and culture are honored when rendering each endpoint.
    /// </summary>
    [TestMethod]
    public void ToString_WithFormatAndProvider_ShouldFormatEndpoints()
    {
        Interval<double> interval = Interval<double>.Closed(1.5, 2.75);
        string result = interval.ToString("F2", CultureInfo.InvariantCulture);

        Assert.AreEqual("[1.50, 2.75]", result);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
    /// writes the canonical text and returns the character count.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenSpanIsLargeEnough_ShouldWriteAndReturnTrue()
    {
        Interval<int> interval = Interval<int>.Closed(1, 5);
        Span<char> buffer = stackalloc char[16];

        bool ok = interval.TryFormat(buffer, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        Assert.AreEqual("[1, 5]", buffer.Slice(0, written).ToString());
    }

    /// <summary>
    /// Verifies that <c>TryFormat</c> returns <c>false</c> and writes zero characters when the buffer is too small.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenSpanIsTooSmall_ShouldReturnFalse()
    {
        Interval<int> interval = Interval<int>.Closed(1, 5);
        Span<char> buffer = stackalloc char[2];

        bool ok = interval.TryFormat(buffer, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies the UTF-8 <c>TryFormat</c> path round-trips through UTF-8 bytes back to the canonical text.
    /// </summary>
    [TestMethod]
    public void TryFormat_Utf8_ShouldWriteCanonicalBytes()
    {
        Interval<int> interval = Interval<int>.ClosedOpen(1, 5);
        Span<byte> buffer = stackalloc byte[16];

        bool ok = interval.TryFormat(buffer, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        string roundTripped = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, written));
        Assert.AreEqual("[1, 5)", roundTripped);
    }
}
