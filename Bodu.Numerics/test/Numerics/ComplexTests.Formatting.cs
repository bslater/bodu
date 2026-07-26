// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the default string representation uses the bracketed <c>&lt;real; imaginary&gt;</c> form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenInvariantCulture_ShouldUseBracketedForm()
    {
        string text = new Complex<double>(3.0, -4.0).ToString(null, CultureInfo.InvariantCulture);

        Assert.AreEqual("<3; -4>", text);
    }

    /// <summary>
    /// Verifies that a numeric format specifier is applied to each component.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatSpecifierSupplied_ShouldApplyToEachComponent()
    {
        string text = new Complex<double>(1.5, 2.25).ToString("F2", CultureInfo.InvariantCulture);

        Assert.AreEqual("<1.50; 2.25>", text);
    }

    /// <summary>
    /// Verifies that a comma-decimal culture keeps the semicolon component separator so the two parts stay
    /// distinguishable.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCommaDecimalCulture_ShouldKeepSemicolonSeparator()
    {
        var german = CultureInfo.GetCultureInfo("de-DE");

        string text = new Complex<double>(1.5, 2.5).ToString(null, german);

        Assert.AreEqual("<1,5; 2,5>", text);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
    /// writes the same text as <see cref="Complex{T}.ToString(string?, IFormatProvider?)" />.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationLargeEnough_ShouldWriteBracketedForm()
    {
        Span<char> destination = stackalloc char[32];

        bool success = new Complex<double>(3.0, -4.0).TryFormat(destination, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(success);
        Assert.AreEqual("<3; -4>", destination[..written].ToString());
    }

    /// <summary>
    /// Verifies that UTF-8 formatting produces the same bytes as the character representation.
    /// </summary>
    [TestMethod]
    public void TryFormatUtf8_WhenDestinationLargeEnough_ShouldMatchCharForm()
    {
        Span<byte> destination = stackalloc byte[32];

        bool success = new Complex<double>(3.0, -4.0).TryFormat(destination, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(success);
        Assert.AreEqual("<3; -4>", System.Text.Encoding.UTF8.GetString(destination[..written]));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
    /// returns <see langword="false" /> and writes nothing when the destination span is too small.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        Span<char> destination = stackalloc char[3];

        bool success = new Complex<double>(3.0, -4.0).TryFormat(destination, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);
    }
}
