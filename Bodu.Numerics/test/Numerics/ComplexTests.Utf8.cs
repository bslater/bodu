// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that a <see cref="Complex{T}" /> can be parsed from a UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenUtf8Bytes_ShouldReturnExpectedValue()
    {
        var value = Complex<double>.Parse("<3; -4>"u8, CultureInfo.InvariantCulture);

        Assert.AreEqual(new Complex<double>(3.0, -4.0), value);
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> succeeds for a valid UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenValidUtf8Bytes_ShouldReturnTrueAndValue()
    {
        bool parsed = Complex<double>.TryParse("<1.5; 2.25>"u8, CultureInfo.InvariantCulture, out Complex<double> value);

        Assert.IsTrue(parsed);
        Assert.AreEqual(new Complex<double>(1.5, 2.25), value);
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> fails gracefully for an invalid UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenInvalidUtf8Bytes_ShouldReturnFalse()
    {
        Assert.IsFalse(Complex<double>.TryParse("not a complex"u8, CultureInfo.InvariantCulture, out _));
    }

    /// <summary>
    /// Verifies that a value round-trips exactly through the UTF-8 format and parse pair, closing the loop the type
    /// could previously only format.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenRoundTrippedThroughUtf8Parse_ShouldPreserveValue()
    {
        var value = new Complex<double>(Math.PI, -Math.E);
        Span<byte> buffer = stackalloc byte[64];

        Assert.IsTrue(value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture));
        var roundTripped = Complex<double>.Parse(buffer[..written], CultureInfo.InvariantCulture);

        Assert.AreEqual(value, roundTripped);
        Assert.AreEqual(value.ToString("R", CultureInfo.InvariantCulture), Encoding.UTF8.GetString(buffer[..written]));
    }
}
