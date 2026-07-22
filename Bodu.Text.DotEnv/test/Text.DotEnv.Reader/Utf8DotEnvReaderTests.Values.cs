// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Values.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains value-semantics robustness tests for <see cref="Utf8DotEnvReader" />, pinning the divergences that differ
/// across popular .env loaders (whitespace around the assignment, embedded <c>=</c>, quoted <c>#</c>, multiline values,
/// the <c>export</c> prefix boundary, and the deliberate absence of variable interpolation).
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Verifies that optional whitespace around the assignment operator is tolerated.
    /// </summary>
    [TestMethod]
    public void Read_WhenWhitespaceAroundEquals_ShouldReadKeyAndValue()
    {
        List<string> tokens = Transcribe("KEY = value\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", "String:value", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that only the first <c>=</c> splits the entry, so a value may itself contain <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenValueContainsEquals_ShouldSplitOnFirstOnly()
    {
        List<string> tokens = Transcribe("CONN=a=b=c\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:CONN", "String:a=b=c", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a <c>#</c> inside a quoted value is literal, not an inline comment.
    /// </summary>
    [TestMethod]
    public void Read_WhenHashInsideQuotes_ShouldKeepItLiteral()
    {
        List<string> tokens = Transcribe("K=\"a # b\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", "String:a # b", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a double-quoted value may span physical lines, preserving the literal embedded newline.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineDoubleQuotedValue_ShouldPreserveNewline()
    {
        List<string> tokens = Transcribe("K=\"line1\nline2\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", "String:line1\nline2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that <c>export</c> is only a prefix when followed by whitespace, so <c>exportFOO</c> is a key.
    /// </summary>
    [TestMethod]
    public void Read_WhenExportNotFollowedByWhitespace_ShouldTreatAsKey()
    {
        List<string> tokens = Transcribe("exportFOO=1\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:exportFOO", "String:1", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that variable interpolation is deliberately not performed, so <c>${VAR}</c> is preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Read_WhenValueLooksLikeInterpolation_ShouldPreserveVerbatim()
    {
        List<string> tokens = Transcribe("PATH_LIKE=${HOME}/bin\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:PATH_LIKE", "String:${HOME}/bin", "EndObject" },
            tokens);
    }
}
