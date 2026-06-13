// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.SpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that the TOML v1.1.0 hex-byte escape <c>\xHH</c> is rejected under the v1.0.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenHexByteEscape_ForV10_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = \"\\x41\"\n");
        });
    }

    /// <summary>
    /// Verifies that the TOML v1.1.0 hex-byte escape <c>\xHH</c> decodes to its scalar value under the v1.1.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenHexByteEscape_ForV11_ShouldDecodeScalar()
    {
        TomlDocumentReader reader = CreateV11("v = \"\\x41\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("A", reader.GetString());
    }

    /// <summary>
    /// Verifies that the TOML v1.1.0 escape <c>\e</c> is rejected under the v1.0.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenEscapeSequenceEscape_ForV10_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = \"\\e\"\n");
        });
    }

    /// <summary>
    /// Verifies that the TOML v1.1.0 escape <c>\e</c> decodes to U+001B under the v1.1.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenEscapeSequenceEscape_ForV11_ShouldDecodeEscapeCharacter()
    {
        TomlDocumentReader reader = CreateV11("v = \"\\e\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("\u001b", reader.GetString());
    }


    /// <summary>
    /// Verifies that a trailing comma in an inline table is accepted under the v1.1.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasTrailingComma_ForV11_ShouldDecodeTable()
    {
        TomlDocumentReader reader = CreateV11("v = {x = 1,}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }


    /// <summary>
    /// Verifies that a multi-line inline table is accepted under the v1.1.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableSpansLines_ForV11_ShouldDecodeTable()
    {
        TomlDocumentReader reader = CreateV11("v = {x = 1,\ny = 2}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectProperty(ref reader, "y");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that a time value omitting seconds is rejected under the v1.0.0 grammar.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeOmitsSeconds_ForV10_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 07:32\n");
        });
    }

    /// <summary>
    /// Verifies that a time value omitting seconds is accepted under the v1.1.0 grammar, defaulting the seconds to
    /// zero.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeOmitsSeconds_ForV11_ShouldDefaultSecondsToZero()
    {
        TomlDocumentReader reader = CreateV11("v = 07:32\n");

        ExpectSingleValue(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(new TimeOnly(7, 32, 0), reader.GetTimeOnly());
    }
}
