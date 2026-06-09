// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.ParsingEdges.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public partial class DotEnvTests
{
    /// <summary>
    /// Verifies that trailing whitespace after the final entry is ignored.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTrailingWhitespaceAtEof_ShouldParseEntry()
    {
        DotEnvDocument doc = DotEnv.Parse("A=1\n   ");

        Assert.AreEqual("1", doc["A"]);
    }

    /// <summary>
    /// Verifies that a key beginning with an invalid character throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyStartsWithInvalidChar_ShouldThrow()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("1KEY=value");
        });
    }

    /// <summary>
    /// Verifies that a double-quoted value ending with a dangling backslash throws
    /// <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuoteEndsWithBackslash_ShouldThrow()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("A=\"x\\");
        });
    }

    /// <summary>
    /// Verifies that a backslash-CRLF inside a double-quoted value acts as a line continuation, joining the lines.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuoteCarriageReturnContinuation_ShouldJoinLines()
    {
        DotEnvDocument doc = DotEnv.Parse("A=\"a\\\r\nb\"");

        Assert.AreEqual("ab", doc["A"]);
    }

    /// <summary>
    /// Verifies that an unterminated single-quoted value throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleQuoteUnterminated_ShouldThrow()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("A='x");
        });
    }

    /// <summary>
    /// Verifies that formatting a value containing a carriage return escapes it as <c>\r</c>.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueContainsCarriageReturn_ShouldEscapeIt()
    {
        DotEnvDocument doc = DotEnv.Parse("A=\"x\\ry\"");

        var text = DotEnv.Format(doc);

        Assert.Contains("\\r", text);
    }
}
