// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Contains the <see cref="Utf8DotEnvWriter.WriteString(string)" /> tests that hold the writer's quoting to what the
/// reader trims and treats as an inline comment.
/// </summary>
public partial class Utf8DotEnvWriterTests
{
    /// <summary>
    /// Verifies that a value whose whitespace the reader would trim from an end, or would read as starting an inline
    /// comment, is written double-quoted and reads back unchanged, for whitespace other than space and tab.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="value">The value to write.</param>
    [TestMethod]
    [DataRow("a form feed first", "\fvalue")]
    [DataRow("a vertical tab last", "value\v")]
    [DataRow("a no-break space first", " value")]
    [DataRow("an ideographic space last", "value　")]
    [DataRow("a no-break space before a hash", "a #b")]
    public void WriteString_WhenWhitespaceWouldBeTrimmedOrStartAComment_ShouldQuoteTheValueSoItReadsBack(string testName, string value)
    {
        _ = testName;

        string text = Write(("KEY", value));

        var reader = new Utf8DotEnvReader(Encoding.UTF8.GetBytes(text));
        string? readBack = null;
        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.String)
                readBack = reader.GetString();
        }

        Assert.IsTrue(text.StartsWith("KEY=\"", StringComparison.Ordinal), text);
        Assert.AreEqual(value, readBack);
    }
}
