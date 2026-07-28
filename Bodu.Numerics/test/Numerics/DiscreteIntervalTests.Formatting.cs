// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that a bounded interval renders in canonical closed-bracket notation regardless of the constructing
    /// shape.
    /// </summary>
    [TestMethod]
    public void ToString_WhenBounded_ShouldRenderClosedBrackets()
    {
        Assert.AreEqual("[1, 10]", DiscreteInterval<int>.Closed(1, 10).ToString());
        Assert.AreEqual("[2, 4]", DiscreteInterval<int>.Open(1, 5).ToString());
        Assert.AreEqual("[7, 7]", DiscreteInterval<int>.Singleton(7).ToString());
    }

    /// <summary>
    /// Verifies that unbounded and half-bounded intervals render with the infinity glyphs and an open bracket on the
    /// unbounded side.
    /// </summary>
    [TestMethod]
    public void ToString_WhenUnbounded_ShouldRenderInfinityGlyphs()
    {
        Assert.AreEqual("[0, +∞)", DiscreteInterval<int>.AtLeast(0).ToString());
        Assert.AreEqual("(-∞, 5]", DiscreteInterval<int>.AtMost(5).ToString());
        Assert.AreEqual("(-∞, +∞)", DiscreteInterval<int>.All.ToString());
    }

    /// <summary>
    /// Verifies that the empty interval renders as the empty-set glyph.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEmpty_ShouldRenderEmptyGlyph()
    {
        Assert.AreEqual("∅", DiscreteInterval<int>.Empty.ToString());
        Assert.AreEqual("∅", DiscreteInterval<int>.Open(3, 4).ToString());
    }

    /// <summary>
    /// Verifies that the format-and-culture overloads apply the endpoint format specifier and culture, matching the
    /// continuous <see cref="Interval{T}" /> the formatting delegates to.
    /// </summary>
    [TestMethod]
    public void ToString_WhenGivenFormatAndCulture_ShouldFormatEndpoints()
    {
        DiscreteInterval<int> value = DiscreteInterval<int>.Closed(1, 10);

        Assert.AreEqual("[0001, 0010]", value.ToString("D4"));
        Assert.AreEqual("[0001, 0010]", value.ToString("D4", System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(value.ToInterval().ToString(), value.ToString());
    }

    /// <summary>
    /// Verifies that the character and UTF-8 <c>TryFormat</c> overloads write the same text as
    /// <see cref="DiscreteInterval{T}.ToString()" /> and fail cleanly on an undersized destination.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenWritingCharsAndUtf8_ShouldMatchToString()
    {
        DiscreteInterval<int> value = DiscreteInterval<int>.Closed(2, 4);
        var invariant = System.Globalization.CultureInfo.InvariantCulture;

        Span<char> chars = stackalloc char[32];
        Assert.IsTrue(value.TryFormat(chars, out int charsWritten, default, invariant));
        Assert.AreEqual("[2, 4]", chars[..charsWritten].ToString());

        Span<byte> bytes = stackalloc byte[32];
        Assert.IsTrue(value.TryFormat(bytes, out int bytesWritten, default, invariant));
        Assert.AreEqual("[2, 4]", System.Text.Encoding.UTF8.GetString(bytes[..bytesWritten]));

        Span<char> tiny = stackalloc char[2];
        Assert.IsFalse(value.TryFormat(tiny, out int tinyWritten, default, invariant));
        Assert.AreEqual(0, tinyWritten);
    }
}
