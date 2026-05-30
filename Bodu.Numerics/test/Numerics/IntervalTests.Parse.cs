// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" /> accepts each
    /// bracket combination.
    /// </summary>
    [TestMethod]
    [DataRow("[1, 5]", 1, 5, true, true)]
    [DataRow("(1, 5)", 1, 5, false, false)]
    [DataRow("[1, 5)", 1, 5, true, false)]
    [DataRow("(1, 5]", 1, 5, false, true)]
    [DataRow("[-3, 7]", -3, 7, true, true)]
    public void TryParse_WhenWellFormed_ShouldRecoverBoundsAndInclusivity(string text, int expectedLower, int expectedUpper, bool expectedLowerInclusive, bool expectedUpperInclusive)
    {
        bool ok = Interval<int>.TryParse(text, CultureInfo.InvariantCulture, out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedLower, result.Lower);
        Assert.AreEqual(expectedUpper, result.Upper);
        Assert.AreEqual(expectedLowerInclusive, result.LowerInclusive);
        Assert.AreEqual(expectedUpperInclusive, result.UpperInclusive);
    }

    /// <summary>
    /// Verifies that the empty-set glyph parses to <see cref="Interval{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsEmptySetGlyph_ShouldReturnEmpty()
    {
        bool ok = Interval<int>.TryParse("∅", CultureInfo.InvariantCulture, out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that whitespace around the brackets and endpoints is ignored.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenSurroundedByWhitespace_ShouldStillParse()
    {
        bool ok = Interval<int>.TryParse("  [ 1 ,  5 ]  ", CultureInfo.InvariantCulture, out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that malformed inputs return <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("[1, 5")]
    [DataRow("1, 5]")]
    [DataRow("{1, 5}")]
    [DataRow("[1 - 5]")]
    [DataRow("[abc, 5]")]
    [DataRow("[1, ]")]
    public void TryParse_WhenMalformed_ShouldReturnFalse(string? text)
    {
        bool ok = Interval<int>.TryParse(text, CultureInfo.InvariantCulture, out Interval<int> result);

        Assert.IsFalse(ok);
        Assert.AreEqual(default, result);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Parse(string, IFormatProvider?)" /> throws <see cref="FormatException" />
    /// on bad input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformed_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Interval<int>.Parse("not an interval", CultureInfo.InvariantCulture);
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Parse(string, IFormatProvider?)" /> throws
    /// <see cref="ArgumentNullException" /> when the input string is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Interval<int>.Parse(null!, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that the ToString → TryParse round-trip recovers the original interval for a sample matrix.
    /// </summary>
    [TestMethod]
    public void Parse_ToStringRoundTrip_ShouldRecoverOriginal()
    {
        Interval<int>[] originals =
        [
            Interval<int>.Closed(1, 5),
            Interval<int>.Open(-3, 3),
            Interval<int>.ClosedOpen(0, 100),
            Interval<int>.OpenClosed(-10, 0),
            Interval<int>.Singleton(42),
            Interval<int>.Empty,
        ];

        foreach (Interval<int> original in originals)
        {
            string text = original.ToString();
            bool ok = Interval<int>.TryParse(text, CultureInfo.InvariantCulture, out Interval<int> parsed);

            Assert.IsTrue(ok, $"Failed to parse '{text}'.");
            Assert.AreEqual(original, parsed, $"Round-trip failed for '{text}'.");
        }
    }
}
