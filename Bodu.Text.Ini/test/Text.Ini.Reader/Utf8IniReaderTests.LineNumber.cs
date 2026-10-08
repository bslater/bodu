// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.LineNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniReader.LineNumber" />, verifying the line each token reports.
/// </summary>
public partial class Utf8IniReaderTests
{
    /// <summary>
    /// Verifies that every token reports the line on which it begins, whether the lines end in a LF, a CR LF or a lone
    /// CR and whether or not the last line ends in one: a comment and a section header their own line, and an entry's
    /// property name and string value the line of the entry.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="lineEnding">The line ending after each line.</param>
    /// <param name="finalLineEnding">Whether the last line ends in a line ending.</param>
    [TestMethod]
    [DataRow("LF", "\n", true)]
    [DataRow("LF without a final line ending", "\n", false)]
    [DataRow("CR LF", "\r\n", true)]
    [DataRow("CR LF without a final line ending", "\r\n", false)]
    [DataRow("lone CR", "\r", true)]
    [DataRow("lone CR without a final line ending", "\r", false)]
    public void LineNumber_WhenTokenIsRead_ShouldReportTheLineOnWhichItBegins(string testName, string lineEnding, bool finalLineEnding)
    {
        _ = testName;
        string source = BuildLineNumberSource(lineEnding, finalLineEnding);

        List<string> lines = ReadTokenLines(source);

        var expected = new List<string>
        {
            "Comment:1", "PropertyName:2", "String:2", "SectionHeader:3", "PropertyName:5", "String:5", "Comment:6",
            "SectionHeader:7", "PropertyName:8", "String:8",
        };
        CollectionAssert.AreEqual(expected, lines, string.Join(" | ", lines));
    }

    /// <summary>
    /// Verifies that skipping comments leaves every other token reporting the line on which it begins.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="lineEnding">The line ending after each line.</param>
    [TestMethod]
    [DataRow("LF", "\n")]
    [DataRow("CR LF", "\r\n")]
    [DataRow("lone CR", "\r")]
    public void LineNumber_WhenCommentsAreSkipped_ShouldReportTheLineOnWhichEachTokenBegins(string testName, string lineEnding)
    {
        _ = testName;
        string source = BuildLineNumberSource(lineEnding, finalLineEnding: true);

        List<string> lines = ReadTokenLines(source, new IniReaderOptions { SkipComments = true });

        var expected = new List<string>
        {
            "PropertyName:2", "String:2", "SectionHeader:3", "PropertyName:5", "String:5", "SectionHeader:7",
            "PropertyName:8", "String:8",
        };
        CollectionAssert.AreEqual(expected, lines, string.Join(" | ", lines));
    }

    /// <summary>
    /// Verifies that once <see cref="Utf8IniReader.Read" /> returns <see langword="false" />, the line number is the
    /// line at the end of the input.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The INI source text.</param>
    /// <param name="line">The line at the end of the input.</param>
    [TestMethod]
    [DataRow("a final line ending", "[s]\na=1\n", 3)]
    [DataRow("no final line ending", "[s]\na=1", 2)]
    [DataRow("an empty source", "", 1)]
    public void LineNumber_WhenReadReturnsFalse_ShouldReportTheLineAtTheEndOfTheInput(string testName, string source, int line)
    {
        _ = testName;
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8IniReader(bytes);

        while (reader.Read())
        {
        }

        Assert.AreEqual(line, reader.LineNumber);
    }

    /// <summary>
    /// Builds the source the line-number tests read: a comment, a global entry, a section, a blank line, an indented
    /// entry, a <c>#</c> comment, a section header followed by a comment, and a last entry, one to a line.
    /// </summary>
    /// <param name="lineEnding">The line ending after each line.</param>
    /// <param name="finalLineEnding">Whether the last line ends in a line ending.</param>
    /// <returns>The INI source text.</returns>
    private static string BuildLineNumberSource(string lineEnding, bool finalLineEnding)
    {
        string source = string.Join(lineEnding, "; first", "g=0", "[s]", string.Empty, "  a = 1", "# hash", "[t] ; note", "b=2");
        return finalLineEnding ? source + lineEnding : source;
    }

    /// <summary>
    /// Reads every token from the supplied source and returns each token's kind and line number.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>A <c>Kind:Line</c> entry for each token, in order.</returns>
    private static List<string> ReadTokenLines(string source, IniReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8IniReader(bytes, options);
        var lines = new List<string>();

        while (reader.Read())
            lines.Add($"{reader.TokenType}:{reader.LineNumber}");

        return lines;
    }
}
