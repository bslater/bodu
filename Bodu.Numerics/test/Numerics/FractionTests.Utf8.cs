// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that a <see cref="Fraction{T}" /> can be parsed from a UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenUtf8Bytes_ShouldReturnExpectedValue()
    {
        var value = Fraction<int>.Parse("3/4"u8, null);

        Assert.AreEqual(new Fraction<int>(3, 4), value);
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> succeeds for a valid UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenValidUtf8Bytes_ShouldReturnTrueAndValue()
    {
        bool parsed = Fraction<int>.TryParse("5/8"u8, null, out Fraction<int> value);

        Assert.IsTrue(parsed);
        Assert.AreEqual(new Fraction<int>(5, 8), value);
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> fails gracefully for an invalid UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenInvalidUtf8Bytes_ShouldReturnFalse()
    {
        bool parsed = Fraction<int>.TryParse("bad"u8, null, out _);

        Assert.IsFalse(parsed);
    }

    /// <summary>
    /// Verifies that a <see cref="Fraction{T}" /> formats into a UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenWritingUtf8Bytes_ShouldWriteFormattedValue()
    {
        Span<byte> buffer = stackalloc byte[16];

        bool formatted = new Fraction<int>(3, 4).TryFormat(buffer, out int written, default, null);

        Assert.IsTrue(formatted);
        Assert.AreEqual("3/4", Encoding.UTF8.GetString(buffer[..written]));
    }
}
