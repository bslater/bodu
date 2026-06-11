// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLexerTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlLexerTests
{
    /// <summary>
    /// Verifies that every lexically malformed input is rejected by <see cref="TomlLexer.Read" /> with a
    /// <see cref="TomlFormatException" /> carrying a position.
    /// </summary>
    [TestMethod]
    [DataRow("a = \"x", DisplayName = "unterminated basic string")]
    [DataRow("a = 'x", DisplayName = "unterminated literal string")]
    [DataRow("a = \"\"\"x\"\"", DisplayName = "unterminated multiline string")]
    [DataRow("a = \"\\q\"", DisplayName = "invalid escape")]
    [DataRow("a = \"\\uD800\"", DisplayName = "escaped unpaired surrogate")]
    [DataRow("a = \"\\uFFF\"", DisplayName = "short unicode escape")]
    [DataRow("a = 01", DisplayName = "leading zero integer")]
    [DataRow("a = 1__0", DisplayName = "doubled underscore")]
    [DataRow("a = _1", DisplayName = "leading underscore")]
    [DataRow("a = 1_", DisplayName = "trailing underscore")]
    [DataRow("a = 0x", DisplayName = "empty radix body")]
    [DataRow("a = 0o8", DisplayName = "octal digit out of range")]
    [DataRow("a = 9223372036854775808", DisplayName = "integer overflow")]
    [DataRow("a = .5", DisplayName = "float without integer part")]
    [DataRow("a = 1.", DisplayName = "float without fraction digits")]
    [DataRow("a = 1e", DisplayName = "float without exponent digits")]
    [DataRow("a = 1979-13-01", DisplayName = "month out of range")]
    [DataRow("a = 1979-05-27T25:00:00", DisplayName = "hour out of range")]
    [DataRow("a = 23:59:60", DisplayName = "leap second")]
    [DataRow("a = 1979-05-27T07:32", DisplayName = "seconds required in v1.0")]
    [DataRow("a = tru", DisplayName = "misspelled boolean")]
    [DataRow("a = 1 b = 2", DisplayName = "missing newline between expressions")]
    [DataRow("a  1", DisplayName = "missing equals")]
    [DataRow("= 1", DisplayName = "missing key")]
    [DataRow("a. = 1", DisplayName = "empty key segment")]
    [DataRow("\"\"\"a\"\"\" = 1", DisplayName = "multiline string as key")]
    [DataRow("[a\nb = 1", DisplayName = "unterminated table header")]
    [DataRow("[[a]\nb = 1", DisplayName = "unterminated array-table header")]
    [DataRow("a = [1, 2", DisplayName = "unterminated array")]
    [DataRow("a = [1 2]", DisplayName = "missing array separator")]
    [DataRow("a = {x = 1", DisplayName = "unterminated inline table")]
    [DataRow("a = {x = 1,}", DisplayName = "inline table trailing comma in v1.0")]
    [DataRow("a = {x = 1\n}", DisplayName = "multiline inline table in v1.0")]
    [DataRow("a = \"\u0001\"", DisplayName = "control character in string")]
    [DataRow("# comment \u0001", DisplayName = "control character in comment")]
    [DataRow("a = \"x\rb\"", DisplayName = "bare carriage return in string")]
    public void Read_WhenLexicallyMalformed_ShouldThrowTomlFormatException(string toml)
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            TomlLexer lexer = CreateLexer(toml);
            DrainLexer(ref lexer);
        });

        Assert.IsNotNull(ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that invalid UTF-8 inside a string is rejected with the dedicated invalid-UTF-8 message and a
    /// position.
    /// </summary>
    [TestMethod]
    public void Read_WhenInvalidUtf8InString_ShouldThrowTomlFormatException()
    {
        byte[] document = [.. "s = \""u8.ToArray(), 0xFF, .. "\"\n"u8.ToArray()];

        var ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            var lexer = new TomlLexer(document, TomlSpecVersion.V1_0);
            DrainLexer(ref lexer);
        });

        Assert.AreEqual(5, ex.Offset);
    }

    /// <summary>
    /// Verifies that an overlong UTF-8 encoding inside a string is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenOverlongUtf8InString_ShouldThrowTomlFormatException()
    {
        byte[] document = [.. "s = \""u8.ToArray(), 0xC0, 0xAF, .. "\"\n"u8.ToArray()];

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            var lexer = new TomlLexer(document, TomlSpecVersion.V1_0);
            DrainLexer(ref lexer);
        });
    }
}
