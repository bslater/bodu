// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that a <see cref="BigDecimal" /> can be parsed from a UTF-8 byte span, including scientific notation
    /// and trailing-zero canonicalization.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenUtf8Bytes_ShouldReturnExpectedValue()
    {
        Assert.AreEqual(BD(100, 0), BigDecimal.Parse("100"u8, CultureInfo.InvariantCulture));
        Assert.AreEqual(BD(-314, 2), BigDecimal.Parse("-3.14"u8, CultureInfo.InvariantCulture));
        Assert.AreEqual(BD(1200, 0), BigDecimal.Parse("1.2e3"u8, CultureInfo.InvariantCulture));
        Assert.AreEqual(BD(15, 1), BigDecimal.Parse("1.50"u8, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> succeeds for a valid UTF-8 byte span.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenValidUtf8Bytes_ShouldReturnTrueAndValue()
    {
        bool parsed = BigDecimal.TryParse("0.5"u8, CultureInfo.InvariantCulture, out BigDecimal value);

        Assert.IsTrue(parsed);
        Assert.AreEqual(BD(5, 1), value);
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> fails gracefully for an invalid UTF-8 byte span, leaving the result at zero.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenInvalidUtf8Bytes_ShouldReturnFalse()
    {
        bool parsed = BigDecimal.TryParse("not a number"u8, CultureInfo.InvariantCulture, out BigDecimal value);

        Assert.IsFalse(parsed);
        Assert.AreEqual(BigDecimal.Zero, value);
    }

    /// <summary>
    /// Verifies that a <see cref="BigDecimal" /> formats into a UTF-8 byte span and that the bytes match the
    /// character-based <c>ToString</c> output.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenWritingUtf8Bytes_ShouldWriteFormattedValue()
    {
        BigDecimal value = BD(-314, 2);
        Span<byte> buffer = stackalloc byte[16];

        bool formatted = value.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(formatted);
        Assert.AreEqual("-3.14", Encoding.UTF8.GetString(buffer[..written]));
        Assert.AreEqual(value.ToString(), Encoding.UTF8.GetString(buffer[..written]));
    }

    /// <summary>
    /// Verifies that the UTF-8 <c>TryFormat</c> reports failure without partial output when the destination buffer is
    /// too small.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenUtf8DestinationTooSmall_ShouldReturnFalse()
    {
        Span<byte> buffer = stackalloc byte[2];

        bool formatted = BD(-314, 2).TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(formatted);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that a value round-trips exactly through the UTF-8 format and parse pair.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenRoundTrippedThroughUtf8Parse_ShouldPreserveValue()
    {
        BigDecimal value = BD(123456789, 4);
        Span<byte> buffer = stackalloc byte[32];

        Assert.IsTrue(value.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture));
        BigDecimal roundTripped = BigDecimal.Parse(buffer[..written], CultureInfo.InvariantCulture);

        Assert.AreEqual(value, roundTripped);
    }
}
