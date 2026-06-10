// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.SpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that the TOML v1.1.0 hex-byte escape <c>\xHH</c> decodes to its scalar value under v1.1 but is rejected
    /// under v1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenHexByteEscape_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = \"\\x41\"\n");
        });

        Utf8TomlReader reader = CreateV11("v = \"\\x41\"\n");
        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("A", reader.GetString());
    }

    /// <summary>
    /// Verifies that the TOML v1.1.0 escape <c>\e</c> decodes to U+001B under v1.1 but is rejected under v1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenEscapeSequenceEscape_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = \"\\e\"\n");
        });

        Utf8TomlReader reader = CreateV11("v = \"\\e\"\n");
        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("", reader.GetString());
    }

    /// <summary>
    /// Verifies that a trailing comma in an inline table is rejected under v1.0 but accepted under v1.1.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasTrailingComma_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = {x = 1,}\n");
        });

        Utf8TomlReader reader = CreateV11("v = {x = 1,}\n");
        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that a multi-line inline table is rejected under v1.0 but accepted under v1.1.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableSpansLines_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = {x = 1,\ny = 2}\n");
        });

        Utf8TomlReader reader = CreateV11("v = {x = 1,\ny = 2}\n");
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
    /// Verifies that a time value omitting seconds is rejected under v1.0 but accepted under v1.1, defaulting the
    /// seconds to zero.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeOmitsSeconds_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 07:32\n");
        });

        Utf8TomlReader reader = CreateV11("v = 07:32\n");
        ExpectSingleValue(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(new TimeOnly(7, 32, 0), reader.GetTimeOnly());
    }
}
