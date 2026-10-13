// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
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
            _ = Interval<int>.Parse((string)null!, CultureInfo.InvariantCulture);
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

    /// <summary>
    /// Verifies that <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" /> reports failure
    /// for a null string rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenStringIsNull_ShouldReturnFalse() =>
        Assert.IsFalse(Interval<int>.TryParse((string?)null, CultureInfo.InvariantCulture, out _));

    /// <summary>
    /// Verifies that parsing fails for a non-empty token shorter than the minimum five-character interval form.
    /// </summary>
    [TestMethod]
    [DataRow("[1]")]
    [DataRow("()")]
    public void TryParse_WhenTokenIsTooShort_ShouldReturnFalse(string text) =>
        Assert.IsFalse(Interval<int>.TryParse(text, CultureInfo.InvariantCulture, out _));

    /// <summary>
    /// Verifies that the span-based <see cref="Interval{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" /> overload
    /// throws <see cref="FormatException" /> for malformed input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSpanIsMalformed_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Interval<int>.Parse("not-an-interval".AsSpan(), CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" /> rejects NaN
    /// endpoint text, matching the constructor's NaN-endpoint guard: parsing must not be a back door that constructs
    /// an interval the public constructor would refuse.
    /// </summary>
    [TestMethod]
    [DataRow("[NaN, 5]")]
    [DataRow("[0, NaN]")]
    [DataRow("(NaN, NaN)")]
    public void TryParse_WhenEndpointIsNaN_ShouldReturnFalse(string text) =>
        Assert.IsFalse(Interval<double>.TryParse(text, CultureInfo.InvariantCulture, out _));

    /// <summary>
    /// Verifies that the single-argument <see cref="Interval{T}.Parse(string)" /> and
    /// <see cref="Interval{T}.TryParse(string?, out Interval{T})" /> overloads delegate to the provider-based parsers.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenStringOnly_ShouldDelegateToProviderOverload()
    {
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 5), Interval<int>.Parse("[1, 5)"));

        Assert.IsTrue(Interval<int>.TryParse("[2, 4]", out Interval<int> parsed));
        Assert.AreEqual(Interval<int>.Closed(2, 4), parsed);

        Assert.IsFalse(Interval<int>.TryParse("nope", out _));
    }

    /// <summary>
    /// Verifies that an <see cref="Interval{T}" /> parses from a UTF-8 byte span and round-trips through the UTF-8
    /// format and parse pair, closing the loop the type could previously only format.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenUtf8Bytes_ShouldReturnExpectedValue()
    {
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 5), Interval<int>.Parse("[1, 5)"u8, CultureInfo.InvariantCulture));

        Assert.IsTrue(Interval<double>.TryParse("(0.5, 2.5]"u8, CultureInfo.InvariantCulture, out Interval<double> parsed));
        Assert.AreEqual(Interval<double>.OpenClosed(0.5, 2.5), parsed);

        Assert.IsFalse(Interval<int>.TryParse("bad"u8, CultureInfo.InvariantCulture, out _));

        Interval<int> value = Interval<int>.Closed(-3, 9);
        Span<byte> buffer = stackalloc byte[32];
        Assert.IsTrue(value.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture));
        Assert.AreEqual(value, Interval<int>.Parse(buffer[..written], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that an interval whose closed side carries an infinite endpoint formats to text that parses back to the
    /// same bounded interval: an infinity token beside a closed bracket is the endpoint value, since an unbounded side
    /// is always open.
    /// </summary>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <param name="lowerInclusive">Whether the lower endpoint is included.</param>
    /// <param name="upperInclusive">Whether the upper endpoint is included.</param>
    /// <param name="cultureName">The culture to format and parse under, or empty for the invariant culture.</param>
    [TestMethod]
    [DataRow(0.0, double.PositiveInfinity, true, true, "", DisplayName = "[0, +inf], invariant")]
    [DataRow(double.NegativeInfinity, 0.0, true, true, "", DisplayName = "[-inf, 0], invariant")]
    [DataRow(double.NegativeInfinity, 5.5, true, false, "", DisplayName = "[-inf, 5.5), invariant")]
    [DataRow(double.NegativeInfinity, double.PositiveInfinity, true, true, "", DisplayName = "[-inf, +inf], invariant")]
    [DataRow(0.0, double.PositiveInfinity, true, true, "en-US", DisplayName = "[0, +inf], en-US")]
    [DataRow(double.NegativeInfinity, 2.5, true, true, "de-DE", DisplayName = "[-inf, 2.5], de-DE")]
    public void Parse_WhenClosedEndpointIsInfinite_ShouldRoundTripTheBoundedInterval(
        double lower,
        double upper,
        bool lowerInclusive,
        bool upperInclusive,
        string cultureName)
    {
        CultureInfo culture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
        var original = new Interval<double>(lower, upper, lowerInclusive, upperInclusive);

        string text = original.ToString(null, culture);
        Interval<double> parsed = Interval<double>.Parse(text, culture);

        Assert.AreEqual(original, parsed, $"Text '{text}' did not parse back to the same interval.");
        Assert.IsTrue(parsed.IsBounded);
    }

    /// <summary>
    /// Verifies that an infinite endpoint on an open side formats to text that parses back as an unbounded side, which
    /// holds the same finite values as the bounded interval but is not equal to it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOpenEndpointIsInfinite_ShouldReadAsUnboundedSide()
    {
        var original = Interval<double>.Open(0.0, double.PositiveInfinity);

        string text = original.ToString(null, CultureInfo.InvariantCulture);
        Interval<double> parsed = Interval<double>.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual("(0, Infinity)", text);
        Assert.AreEqual(Interval<double>.GreaterThan(0.0), parsed);
        Assert.AreNotEqual(original, parsed);
        Assert.IsTrue(original.Contains(double.MaxValue));
        Assert.IsTrue(parsed.Contains(double.MaxValue));
    }

    /// <summary>
    /// Verifies that bracket text for a half-bounded interval whose bounded endpoint is the infinity on its unbounded
    /// side parses to <see cref="Interval{T}.Empty" />, as the half-bounded factories return it.
    /// </summary>
    /// <param name="text">The interval text, read under the invariant culture.</param>
    [TestMethod]
    [DataRow("[Infinity, +\u221E)")]
    [DataRow("(Infinity, +\u221E)")]
    [DataRow("(-\u221E, -Infinity]")]
    [DataRow("(-\u221E, -Infinity)")]
    public void Parse_WhenHalfLineIsAnchoredAtFarInfinity_ShouldReturnEmpty(string text)
    {
        Interval<double> parsed = Interval<double>.Parse(text, CultureInfo.InvariantCulture);

        Assert.IsTrue(parsed.IsEmpty, $"'{text}' parsed to {parsed}");
        Assert.AreEqual(Interval<double>.Empty, parsed);
    }
}
