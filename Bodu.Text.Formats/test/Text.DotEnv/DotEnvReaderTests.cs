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
    {
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
    };

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
}
