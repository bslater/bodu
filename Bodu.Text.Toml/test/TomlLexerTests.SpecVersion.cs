// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLexerTests.SpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlLexerTests
{
    /// <summary>
    /// Verifies that the <c>\e</c> escape decodes to U+001B under TOML v1.1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenEscapeE_ForV11_ShouldDecodeEscapeCharacter()
    {
        TomlLexer lexer = CreateLexer("s = \"\\e\"\n", TomlSpecVersion.V1_1);

        Advance(ref lexer, 2);
        Assert.AreEqual("\u001b", lexer.GetString());
    }

    /// <summary>
    /// Verifies that the <c>\xHH</c> escape decodes to the corresponding scalar under TOML v1.1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenEscapeX_ForV11_ShouldDecodeHexScalar()
    {
        TomlLexer lexer = CreateLexer("s = \"\\x41\"\n", TomlSpecVersion.V1_1);

        Advance(ref lexer, 2);
        Assert.AreEqual("A", lexer.GetString());
    }

    /// <summary>
    /// Verifies that the v1.1.0-only escapes are rejected under TOML v1.0.0.
    /// </summary>
    [TestMethod]
    [DataRow("s = \"\\e\"\n", DisplayName = "escape e")]
    [DataRow("s = \"\\x41\"\n", DisplayName = "escape x")]
    public void Read_WhenV11Escape_ForV10_ShouldThrowTomlFormatException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            TomlLexer lexer = CreateLexer(toml);
            DrainLexer(ref lexer);
        });
    }

    /// <summary>
    /// Verifies that a time without seconds is accepted under TOML v1.1.0 and decodes with zero seconds.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeWithoutSeconds_ForV11_ShouldDecodeWithZeroSeconds()
    {
        TomlLexer lexer = CreateLexer("t = 07:32\n", TomlSpecVersion.V1_1);

        Advance(ref lexer, 2);
        Assert.AreEqual(new TimeOnly(7, 32, 0), lexer.GetTimeOnly());
    }

    /// <summary>
    /// Verifies that a multi-line inline table with a trailing comma lexes under TOML v1.1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineInlineTableWithTrailingComma_ForV11_ShouldLexCleanly()
    {
        TomlLexer lexer = CreateLexer("p = {\n  x = 1, # one\n  y = 2,\n}\n", TomlSpecVersion.V1_1);

        CollectionAssert.AreEqual(
            new[] { "Key(p)!", "StartInlineTable", "Key(x)!", "Integer(1)", "Comment( one)", "Key(y)!", "Integer(2)", "EndInlineTable" },
            DrainLexer(ref lexer));
    }
}
