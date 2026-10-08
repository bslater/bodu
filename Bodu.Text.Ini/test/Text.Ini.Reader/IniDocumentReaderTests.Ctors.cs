// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentReaderTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains the member tests for the <see cref="IniDocumentReader" /> constructors that concern the position of a
/// duplicate-policy violation, which the constructor reports as it normalizes the document.
/// </summary>
public partial class IniDocumentReaderTests
{
    /// <summary>
    /// Verifies that a duplicate section or key the document options reject is reported at the offending header or key:
    /// its line, and the offset of its first byte, the <c>[</c> of a header or the first byte of a key after any
    /// leading whitespace.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The INI source, its lines separated by LF.</param>
    /// <param name="lineEnding">The line ending written in place of each LF.</param>
    /// <param name="skipComments">Whether the reader skips comments.</param>
    /// <param name="line">The expected 1-based line of the offending header or key.</param>
    /// <param name="offset">The expected zero-based byte offset of its first byte.</param>
    [TestMethod]
    [DataRow("duplicate section, LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\n", false, 5, 26)]
    [DataRow("duplicate key in a section, CR LF, comments skipped", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r\n", true, 4, 20)]
    [DataRow("duplicate global key, lone CR", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r", false, 3, 10)]
    public void Ctor_WhenAPolicyRejectsASectionOrKey_ShouldReportTheOffendingHeaderOrKey(string testName, string source, string lineEnding, bool skipComments, int line, int offset)
    {
        _ = testName;
        byte[] bytes = Encoding.UTF8.GetBytes(source.Replace("\n", lineEnding, StringComparison.Ordinal));
        var readerOptions = new IniReaderOptions { SkipComments = skipComments };
        var documentOptions = new IniDocumentOptions
        {
            DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed,
            DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed,
        };

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = new IniDocumentReader(bytes, readerOptions, documentOptions);
        });

        Assert.AreEqual(line, ex.LineNumber, ex.Message);
        Assert.AreEqual(offset, ex.Offset, ex.Message);
    }
}
