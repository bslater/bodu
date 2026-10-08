// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Inspection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Verifies the reader's inspection surface: the token's raw bytes, the position and line it came from, whether it
/// carried an <c>export</c> prefix, and the allocation-free comparison overloads.
/// </summary>
/// <remarks>
/// The token transcript tests drive the reader through <c>GetString</c>, which allocates. The rest of the surface
/// exists so a caller can inspect a token without that cost - comparing a property name against an expected key, or
/// recording where a value came from for a diagnostic - and none of it was covered. The comparisons are asserted to
/// agree with <c>GetString</c> rather than merely to return something, since a fast path that disagrees with the slow
/// one is worse than no fast path.
/// </remarks>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Advances a reader to the first token of the requested kind.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="tokenType">The token kind to stop at.</param>
    private static void AdvanceTo(ref Utf8DotEnvReader reader, DotEnvTokenType tokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == tokenType)
                return;
        }

        Assert.Fail($"No {tokenType} token was produced.");
    }

    /// <summary>
    /// Verifies that a token's raw span carries the same text <c>GetString</c> decodes, so a caller can compare
    /// bytes without decoding first.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenTokenIsText_ShouldMatchTheDecodedString()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(reader.GetString(), Encoding.UTF8.GetString(reader.ValueSpan));

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.AreEqual(reader.GetString(), Encoding.UTF8.GetString(reader.ValueSpan));
    }

    /// <summary>
    /// Verifies that a token carrying no text exposes an empty span rather than the previous token's bytes.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenTokenIsNotText_ShouldBeEmpty()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(DotEnvTokenType.StartObject, reader.TokenType);
        Assert.IsTrue(reader.ValueSpan.IsEmpty);
    }

    /// <summary>
    /// Verifies that reading text from a token that carries none is rejected, rather than returning the previous
    /// token's value.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsNotText_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.IsTrue(reader.Read());

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var inner = new Utf8DotEnvReader(bytes);
            _ = inner.Read();
            _ = inner.GetString();
        });
    }

    /// <summary>
    /// Verifies that the UTF-8 comparison agrees with the decoded string, and rejects a near-miss rather than
    /// matching on a prefix.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparingUtf8_ShouldAgreeWithGetString()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);

        Assert.IsTrue(reader.ValueTextEquals("HOST"u8));
        Assert.IsFalse(reader.ValueTextEquals("HOS"u8));
        Assert.IsFalse(reader.ValueTextEquals("HOSTX"u8));
        Assert.IsFalse(reader.ValueTextEquals("host"u8));
    }

    /// <summary>
    /// Verifies that the character comparison agrees with the UTF-8 one, including for a value whose UTF-8 encoding
    /// is longer than its character count - where comparing lengths naively would report a false mismatch.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparingChars_ShouldAgreeWithTheUtf8Overload()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=café\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.String);

        Assert.IsTrue(reader.ValueTextEquals("café".AsSpan()));
        Assert.IsTrue(reader.ValueTextEquals("café"));
        Assert.IsFalse(reader.ValueTextEquals("cafe".AsSpan()));
        Assert.IsFalse(reader.ValueTextEquals("caf".AsSpan()));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> string compares equal only to an empty value, matching how an empty
    /// span behaves.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTextIsNull_ShouldMatchOnlyAnEmptyValue()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("EMPTY=\nHOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.IsTrue(reader.ValueTextEquals((string?)null));

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.IsFalse(reader.ValueTextEquals((string?)null));
    }

    /// <summary>
    /// Verifies that every comparison overload rejects a token that carries no text, rather than silently reporting
    /// a mismatch that a caller would read as "the value differs".
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTokenIsNotText_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8DotEnvReader(bytes);
            _ = reader.Read();
            _ = reader.ValueTextEquals("HOST"u8);
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8DotEnvReader(bytes);
            _ = reader.Read();
            _ = reader.ValueTextEquals("HOST".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that the line number advances by one per entry when the entries sit on consecutive lines.
    /// </summary>
    /// <remarks>
    /// Each property name reports the line on which its entry begins, so the line advances by one from each entry to
    /// the next. <c>LineNumber_WhenTokenIsRead_ShouldReportTheLineOnWhichItBegins</c> pins the absolute line of every
    /// kind of token.
    /// </remarks>
    [TestMethod]
    public void LineNumber_ShouldAdvanceOncePerEntry()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\nB=2\nC=3\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        int first = reader.LineNumber;

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(first + 1, reader.LineNumber);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(first + 2, reader.LineNumber);
    }

    /// <summary>
    /// Verifies that every token reports the line on which it begins, whether the lines end in a LF, a CR LF or a lone
    /// CR and whether or not the last line ends in one: a comment its own line, an entry's property name and string
    /// value the line of the entry, and the closing object the line at the end of the input.
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
        string source = string.Join(lineEnding, "# first", "A=1", string.Empty, "export B='two'", "  C = \"three\" # note", "# last");
        if (finalLineEnding)
            source += lineEnding;

        List<string> lines = ReadTokenLines(source);

        var expected = new List<string>
        {
            "StartObject:1", "Comment:1", "PropertyName:2", "String:2", "PropertyName:4", "String:4", "PropertyName:5",
            "String:5", "Comment:6", $"EndObject:{(finalLineEnding ? 7 : 6)}",
        };
        CollectionAssert.AreEqual(expected, lines, string.Join(" | ", lines));
    }

    /// <summary>
    /// Verifies that a double-quoted value spanning lines reports the line of its opening quote, and that the entry
    /// after it reports its own line, whatever the line endings inside and after the value.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="lineEnding">The line ending after each line, and inside the value.</param>
    [TestMethod]
    [DataRow("LF", "\n")]
    [DataRow("CR LF", "\r\n")]
    [DataRow("lone CR", "\r")]
    public void LineNumber_WhenAValueSpansLines_ShouldReportTheLineOfItsOpeningQuote(string testName, string lineEnding)
    {
        _ = testName;
        string source = string.Join(lineEnding, "A=1", "B=\"x", "y", "z\"", "C=3") + lineEnding;

        List<string> lines = ReadTokenLines(source);

        var expected = new List<string>
        {
            "StartObject:1", "PropertyName:1", "String:1", "PropertyName:2", "String:2", "PropertyName:5", "String:5",
            "EndObject:6",
        };
        CollectionAssert.AreEqual(expected, lines, string.Join(" | ", lines));
    }

    /// <summary>
    /// Reads every token from the supplied source and returns each token's kind and line number.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <returns>A <c>Kind:Line</c> entry for each token, in order.</returns>
    private static List<string> ReadTokenLines(string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DotEnvReader(bytes);
        var lines = new List<string>();

        while (reader.Read())
            lines.Add($"{reader.TokenType}:{reader.LineNumber}");

        return lines;
    }

    /// <summary>
    /// Verifies that the consumed-byte count advances with the read and reaches the end of the input, so a caller
    /// resuming from a buffer knows where the reader stopped.
    /// </summary>
    [TestMethod]
    public void BytesConsumed_ShouldAdvanceAndReachTheEndOfInput()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\nB=2\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.AreEqual(0, reader.BytesConsumed);

        int previous = 0;
        while (reader.Read())
        {
            Assert.IsGreaterThanOrEqualTo(previous, reader.BytesConsumed);
            previous = reader.BytesConsumed;
        }

        Assert.AreEqual(bytes.Length, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the depth reports zero outside the document body and one inside it, so a caller can tell a
    /// structural token from an entry.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_ShouldBeOneInsideTheDocumentBody()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.AreEqual(0, reader.CurrentDepth);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(1, reader.CurrentDepth);
    }

    /// <summary>
    /// Verifies that an <c>export</c> prefix is reported per entry, so a writer can reproduce it only where the
    /// source had it.
    /// </summary>
    [TestMethod]
    public void CurrentIsExport_ShouldReportThePrefixPerEntry()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("export A=1\nB=2\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.IsTrue(reader.CurrentIsExport);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.IsFalse(reader.CurrentIsExport);
    }
}
