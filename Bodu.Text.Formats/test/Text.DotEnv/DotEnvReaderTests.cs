// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

[TestClass]
public sealed class DotEnvReaderTests
{
    /// <summary>
    /// A representative set of duplicate-free DotEnv sources covering blank lines, comments, single- and double-quoted
    /// values, escape sequences, multi-line quoted values, line continuations, CRLF endings, and a missing trailing
    /// newline. Each is used to assert reader/parser parity.
    /// </summary>
    private static readonly string[] ParitySources =
    [
        "A=1\nB=2\n",
        "A=1\nB=2",
        "# comment\nKEY=value\n\n# trailing\nOTHER=x\n",
        "NAME=hello world \n",
        "Q='literal value'\n",
        "D=\"escaped \\\"quote\\\" and \\n newline\"\n",
        "ML=\"line one\nline two\"\nNEXT=after\n",
        "CONT=\"a\\\nb\"\n",
        "CRLF=1\r\nNEXT=2\r\n",
        "EMPTY=\nFILLED=x\n",
        "INLINE=value # trailing comment\n",
        "DOLLAR=\"price \\$5\"\n",
    ];

    /// <summary>
    /// Enumerates each parity source paired with a range of buffer sizes (including buffers far smaller than any
    /// construct) so the streaming reader's cross-boundary refill logic is exercised against the reference parser.
    /// </summary>
    public static IEnumerable<object[]> ParityData()
    {
        foreach (var source in ParitySources)
            foreach (var bufferSize in new[] { 1, 2, 3, 7, 64, 4096 })
                yield return new object[] { source, bufferSize };
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvReader" /> yields exactly the same (key, value) sequence as
    /// <see cref="DotEnv.Parse(System.ReadOnlySpan{char})" /> for every source, at every buffer size — confirming the
    /// incremental reader matches the reference parser across buffer boundaries.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="bufferSize">The reader buffer size in characters.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ParityData), DynamicDataSourceType.Method)]
    public void Read_ShouldMatchParse_AcrossBufferSizes(string source, int bufferSize)
    {
        DotEnvDocument expected = DotEnv.Parse(source);
        List<(string, string)> expectedPairs = new();
        foreach (DotEnvEntry e in expected.Entries)
            expectedPairs.Add((e.Key, e.Value));

        using DotEnvReader reader = new(new StringReader(source), DotEnvParseOptions.Default, bufferSize);
        List<(string, string)> actualPairs = new();
        while (reader.Read())
            actualPairs.Add((reader.Key, reader.Value));

        CollectionAssert.AreEqual(
            expectedPairs,
            actualPairs,
            $"Mismatch for source '{source.Replace("\n", "\\n").Replace("\r", "\\r")}' at buffer size {bufferSize}.");
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvReader.ReadAsync" /> yields the same entries as the synchronous path, including a
    /// multi-line double-quoted value, at a tiny buffer size.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_ShouldMatchParse()
    {
        const string Source = "A=1\nML=\"line one\nline two\"\nB=2\n";

        DotEnvDocument expected = DotEnv.Parse(Source);
        List<(string, string)> expectedPairs = new();
        foreach (DotEnvEntry e in expected.Entries)
            expectedPairs.Add((e.Key, e.Value));

        using DotEnvReader reader = new(new StringReader(Source), DotEnvParseOptions.Default, bufferSize: 2);
        List<(string, string)> actualPairs = new();
        while (await reader.ReadAsync())
            actualPairs.Add((reader.Key, reader.Value));

        CollectionAssert.AreEqual(expectedPairs, actualPairs);
    }

    /// <summary>
    /// Verifies that the reader reports the line number of the current entry's key.
    /// </summary>
    [TestMethod]
    public void Read_ShouldReportEntryLineNumber()
    {
        using DotEnvReader reader = new(new StringReader("# c\nA=1\n\nB=2\n"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("A", reader.Key);
        Assert.AreEqual(2, reader.LineNumber);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("B", reader.Key);
        Assert.AreEqual(4, reader.LineNumber);
    }

    /// <summary>
    /// Verifies that an unterminated double-quoted value throws <see cref="DotEnvFormatException" /> even when the
    /// buffer is too small to hold the whole value at once.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuoteUnterminated_ShouldThrowExactly()
    {
        using DotEnvReader reader = new(new StringReader("K=\"unterminated"), DotEnvParseOptions.Default, bufferSize: 2);

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a malformed entry line (no <c>=</c>) throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenEntryMalformed_ShouldThrowExactly()
    {
        using DotEnvReader reader = new(new StringReader("NOEQUALS\n"));

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> reader.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenReaderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DotEnvReader(null!);
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a buffer size below one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBufferSizeIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DotEnvReader(new StringReader(string.Empty), DotEnvParseOptions.Default, 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="DotEnvReader.Read" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDisposed_ShouldThrowExactly()
    {
        DotEnvReader reader = new(new StringReader("A=1\n"));
        reader.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = reader.Read();
        });
    }

    // ------------------------------------------- streaming edge coverage ------------------------------------------

    /// <summary>
    /// Streams every entry from <paramref name="source" /> at the supplied buffer size.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">The parse options, or <see langword="null" /> for the defaults.</param>
    /// <param name="bufferSize">The reader buffer size in characters.</param>
    /// <returns>The (key, value) pairs in source order.</returns>
    private static List<(string Key, string Value)> StreamAll(string source, DotEnvParseOptions? options = null, int bufferSize = 1)
    {
        using DotEnvReader reader = new(new StringReader(source), options ?? DotEnvParseOptions.Default, bufferSize);
        List<(string, string)> list = new();
        while (reader.Read())
            list.Add((reader.Key, reader.Value));

        return list;
    }

    /// <summary>
    /// Verifies that disposing a <see cref="DotEnvReader" /> twice is a no-op on the second call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        DotEnvReader reader = new(new StringReader("A=1\n"));

        reader.Dispose();
        reader.Dispose();
    }

    /// <summary>
    /// Verifies that leading whitespace, blank CRLF lines, and a CRLF-terminated comment are skipped before the entry,
    /// even one character at a time.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingWhitespaceBlankCrlfAndCrlfComment_ShouldSkipToEntry()
    {
        List<(string, string)> result = StreamAll("\r\n  \t# c\r\n\r\n   KEY=value\r\n");

        CollectionAssert.AreEqual(new[] { ("KEY", "value") }, result);
    }

    /// <summary>
    /// Verifies that the legacy <c>export </c> prefix is stripped before the key, even one character at a time.
    /// </summary>
    [TestMethod]
    public void Read_WhenExportPrefix_ShouldStripIt()
    {
        List<(string, string)> result = StreamAll("export KEY=value\n");

        CollectionAssert.AreEqual(new[] { ("KEY", "value") }, result);
    }

    /// <summary>
    /// Verifies that a key beginning with an invalid character throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenKeyStartsWithInvalidChar_ShouldThrow()
    {
        using DotEnvReader reader = new(new StringReader("1KEY=value\n"));

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a key with no <c>=</c> at end of input throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenKeyHasNoEqualsAtEof_ShouldThrow()
    {
        using DotEnvReader reader = new(new StringReader("KEY"));

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that characters trailing a closing double quote are consumed up to the line terminator.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailingCharsAfterQuotedValue_ShouldConsumeToLineEnd()
    {
        List<(string, string)> result = StreamAll("A=\"x\" junk\nB=2\n");

        CollectionAssert.AreEqual(new[] { ("A", "x"), ("B", "2") }, result);
    }

    /// <summary>
    /// Verifies that the double-quoted escape grammar resolves backslash, tab, carriage return, dollar, and unknown
    /// escapes, even one character at a time.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedHasVariedEscapes_ShouldResolveThem()
    {
        var source = "A=\"x" + "\\\\" + "\\t" + "\\r" + "\\$" + "\\q" + "\"\n";

        var result = StreamAll(source);

        Assert.AreEqual("x\\\t\r$\\q", result[0].Value);
    }

    /// <summary>
    /// Verifies that a backslash-CRLF inside a double-quoted value acts as a line continuation, joining the lines.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedCarriageReturnContinuation_ShouldJoinLines()
    {
        var result = StreamAll("A=\"a\\\r\nb\"\nB=2\n");

        Assert.AreEqual("ab", result[0].Value);
    }

    /// <summary>
    /// Verifies that a double-quoted value ending with a dangling backslash at end of input throws
    /// <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedEndsWithBackslashAtEof_ShouldThrow()
    {
        using DotEnvReader reader = new(new StringReader("A=\"x\\"), DotEnvParseOptions.Default, 2);

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a newline inside a single-quoted value throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleQuoteContainsNewline_ShouldThrow()
    {
        using DotEnvReader reader = new(new StringReader("A='x\ny'\n"));

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that an unterminated single-quoted value at end of input throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleQuoteUnterminatedAtEof_ShouldThrow()
    {
        using DotEnvReader reader = new(new StringReader("A='x"), DotEnvParseOptions.Default, 2);

        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = reader.Read();
        });
    }
}
