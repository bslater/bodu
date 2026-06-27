// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.TypedAccessors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that the narrowing integer accessors return the value when it fits the target type.
    /// </summary>
    [TestMethod]
    public void GetNarrowingAccessors_WhenValueFits_ShouldReturnNarrowedValue()
    {
        Utf8TomlReader reader = Create("i = 42\n");

        Advance(ref reader, 2);
        Assert.AreEqual((byte)42, reader.GetByte());
        Assert.AreEqual((sbyte)42, reader.GetSByte());
        Assert.AreEqual((short)42, reader.GetInt16());
        Assert.AreEqual(42, reader.GetInt32());
        Assert.AreEqual((ushort)42, reader.GetUInt16());
        Assert.AreEqual(42u, reader.GetUInt32());
        Assert.AreEqual(42ul, reader.GetUInt64());
    }

    /// <summary>
    /// Verifies that the non-throwing narrowing accessors report failure without throwing when the value does not fit
    /// the target type.
    /// </summary>
    [TestMethod]
    public void TryGetNarrowingAccessors_WhenValueDoesNotFit_ShouldReturnFalse()
    {
        Utf8TomlReader reader = Create("i = 4294967296\n");

        Advance(ref reader, 2);
        Assert.IsFalse(reader.TryGetByte(out _));
        Assert.IsFalse(reader.TryGetSByte(out _));
        Assert.IsFalse(reader.TryGetInt16(out _));
        Assert.IsFalse(reader.TryGetInt32(out _));
        Assert.IsFalse(reader.TryGetUInt16(out _));
        Assert.IsFalse(reader.TryGetUInt32(out _));
        Assert.IsTrue(reader.TryGetUInt64(out ulong u64));
        Assert.AreEqual(4294967296ul, u64);
    }

    /// <summary>
    /// Verifies that a negative integer fails the unsigned conversions and the throwing accessor raises
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void GetUInt64_WhenValueIsNegative_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            Utf8TomlReader reader = Create("i = -1\n");
            Advance(ref reader, 2);
            _ = reader.GetUInt64();
        });
    }

    /// <summary>
    /// Verifies that a narrowing accessor on a non-integer token throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenCurrentTokenIsFloat_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader reader = Create("f = 1.5\n");
            Advance(ref reader, 2);
            _ = reader.GetInt32();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetSingle" /> parses the raw literal and maps the special sentinels.
    /// </summary>
    [TestMethod]
    public void GetSingle_WhenFloatToken_ShouldDecodeSinglePrecisionValue()
    {
        Utf8TomlReader reader = Create("f = 1.5\ng = -inf\n");

        Advance(ref reader, 2);
        Assert.AreEqual(1.5f, reader.GetSingle());

        Advance(ref reader, 2);
        Assert.AreEqual(float.NegativeInfinity, reader.GetSingle());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetDecimal" /> parses the raw literal exactly, preserving digits that
    /// binary64 cannot represent.
    /// </summary>
    [TestMethod]
    public void GetDecimal_WhenFloatToken_ShouldParseRawLiteralExactly()
    {
        Utf8TomlReader reader = Create("d = 0.1000000000000000055511151231257827\n");

        Advance(ref reader, 2);
        Assert.AreEqual(0.1000000000000000055511151231m, reader.GetDecimal());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TryGetDecimal" /> returns <see langword="false" /> for the non-finite
    /// sentinels, which <see cref="decimal" /> cannot represent.
    /// </summary>
    [TestMethod]
    public void TryGetDecimal_WhenNonFinite_ShouldReturnFalse()
    {
        Utf8TomlReader reader = Create("d = nan\n");

        Advance(ref reader, 2);
        Assert.IsFalse(reader.TryGetDecimal(out _));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetDecimal" /> honors underscores in the raw literal.
    /// </summary>
    [TestMethod]
    public void GetDecimal_WhenLiteralHasUnderscores_ShouldStripThem()
    {
        Utf8TomlReader reader = Create("d = 1_000.5\n");

        Advance(ref reader, 2);
        Assert.AreEqual(1000.5m, reader.GetDecimal());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetGuid" /> parses a hyphenated GUID from a string token and rejects
    /// other shapes.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenStringTokenHoldsHyphenatedGuid_ShouldParse()
    {
        Utf8TomlReader reader = Create("g = \"8f6b9bbe-7a17-46d6-9c3e-cdcf38b1d6a2\"\nh = \"not a guid\"\n");

        Advance(ref reader, 2);
        Assert.AreEqual(Guid.Parse("8f6b9bbe-7a17-46d6-9c3e-cdcf38b1d6a2"), reader.GetGuid());

        Advance(ref reader, 2);
        Assert.IsFalse(reader.TryGetGuid(out _));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetComment" /> returns the comment text and rejects other tokens.
    /// </summary>
    [TestMethod]
    public void GetComment_WhenCommentToken_ShouldReturnTextAfterHash()
    {
        Utf8TomlReader reader = Create("# note\na = 1\n");

        Advance(ref reader, 1);
        Assert.AreEqual(" note", reader.GetComment());

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader inner = Create("a = 1\n");
            Advance(ref inner, 1);
            _ = inner.GetComment();
        });
    }

    /// <summary>
    /// Verifies that a multi-line basic string trims its leading newline, resolves escapes and line-ending
    /// backslashes, and preserves interior newlines as written.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineBasicString_ShouldTrimLeadingNewlineAndResolveEscapes()
    {
        Utf8TomlReader lexer = Create("s = \"\"\"\nline1\nline2 \\\n   joined\"\"\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual("line1\nline2 joined", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line literal string trims its leading newline and preserves all other content verbatim.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineLiteralString_ShouldTrimLeadingNewlineOnly()
    {
        Utf8TomlReader lexer = Create("s = '''\nraw \\n text'''\n");

        Advance(ref lexer, 2);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.AreEqual("raw \\n text", lexer.GetString());
    }

    /// <summary>
    /// Verifies that quotes belonging to a multi-line basic string's content adjacent to the closing delimiter are
    /// preserved.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineStringEndsWithQuotes_ShouldKeepContentQuotes()
    {
        Utf8TomlReader lexer = Create("s = \"\"\"two quotes:\"\"\"\"\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual("two quotes:\"\"", lexer.GetString());
    }

    /// <summary>
    /// Verifies that radix integers (hexadecimal, octal, binary) decode to the expected values.
    /// </summary>
    [TestMethod]
    [DataRow("0xDEADBEEF", 3735928559L, DisplayName = "hex")]
    [DataRow("0o755", 493L, DisplayName = "octal")]
    [DataRow("0b1101", 13L, DisplayName = "binary")]
    [DataRow("0xF_F", 255L, DisplayName = "hex with underscore")]
    public void GetInt64_WhenRadixInteger_ShouldDecodeValue(string literal, long expected)
    {
        Utf8TomlReader lexer = Create($"i = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetInt64());
    }

    /// <summary>
    /// Verifies that the integer range boundaries decode exactly.
    /// </summary>
    [TestMethod]
    [DataRow("9223372036854775807", long.MaxValue, DisplayName = "long.MaxValue")]
    [DataRow("-9223372036854775808", long.MinValue, DisplayName = "long.MinValue")]
    public void GetInt64_WhenAtRangeBoundary_ShouldDecodeValue(string literal, long expected)
    {
        Utf8TomlReader lexer = Create($"i = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetInt64());
    }

    /// <summary>
    /// Verifies that special float values decode to the expected IEEE 754 values.
    /// </summary>
    [TestMethod]
    [DataRow("inf", double.PositiveInfinity, DisplayName = "inf")]
    [DataRow("+inf", double.PositiveInfinity, DisplayName = "+inf")]
    [DataRow("-inf", double.NegativeInfinity, DisplayName = "-inf")]
    [DataRow("6.626e-34", 6.626e-34, DisplayName = "exponent")]
    public void GetDouble_WhenSpecialOrExponentFloat_ShouldDecodeValue(string literal, double expected)
    {
        Utf8TomlReader lexer = Create($"f = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetDouble());
    }

    /// <summary>
    /// Verifies that <c>nan</c> decodes to <see cref="double.NaN" />.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenNan_ShouldDecodeToNaN()
    {
        Utf8TomlReader lexer = Create("f = nan\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(double.IsNaN(lexer.GetDouble()));
    }

    /// <summary>
    /// Verifies that a typed accessor mismatching the current token type throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenCurrentTokenIsString_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader lexer = Create("s = \"x\"\n");
            Advance(ref lexer, 2);
            _ = lexer.GetInt64();
        });
    }

}
