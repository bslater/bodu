// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Document;

/// <summary>
/// Contains the member tests for
/// <see cref="IniDocument.Parse(ReadOnlySpan{byte}, IniReaderOptions, IniDocumentOptions)" /> that concern the position
/// of a duplicate-policy violation.
/// </summary>
public partial class IniDocumentTests
{
    /// <summary>
    /// Verifies that a duplicate section or key the document options reject is reported at the offending header or key:
    /// its line, and the offset of its first byte, the <c>[</c> of a header or the first byte of a key after any
    /// leading whitespace, whatever the line endings and whether comments are kept.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The INI source, its lines separated by LF.</param>
    /// <param name="lineEnding">The line ending written in place of each LF.</param>
    /// <param name="skipComments">Whether the reader skips comments.</param>
    /// <param name="line">The expected 1-based line of the offending header or key.</param>
    /// <param name="offset">The expected zero-based byte offset of its first byte.</param>
    [TestMethod]
    [DataRow("duplicate section, LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\n", false, 5, 26)]
    [DataRow("duplicate section, CR LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r\n", false, 5, 30)]
    [DataRow("duplicate section, lone CR", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r", false, 5, 26)]
    [DataRow("duplicate section, CR LF, comments skipped", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r\n", true, 5, 30)]
    [DataRow("duplicate key in a section, LF", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\n", false, 4, 17)]
    [DataRow("duplicate key in a section, CR LF", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r\n", false, 4, 20)]
    [DataRow("duplicate key in a section, lone CR", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r", false, 4, 17)]
    [DataRow("duplicate key in a section, CR LF, comments skipped", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r\n", true, 4, 20)]
    [DataRow("duplicate global key, LF", "; top\ng=1\ng=2\n[s]\nk=1\n", "\n", false, 3, 10)]
    [DataRow("duplicate global key, CR LF", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r\n", false, 3, 12)]
    [DataRow("duplicate global key, lone CR", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r", false, 3, 10)]
    [DataRow("duplicate global key, CR LF, comments skipped", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r\n", true, 3, 12)]
    public void Parse_WhenAPolicyRejectsASectionOrKey_ShouldReportTheOffendingHeaderOrKey(string testName, string source, string lineEnding, bool skipComments, int line, int offset)
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
            using IniDocument document = IniDocument.Parse(bytes, readerOptions, documentOptions);
        });

        Assert.AreEqual(line, ex.LineNumber, ex.Message);
        Assert.AreEqual(offset, ex.Offset, ex.Message);
    }
}
