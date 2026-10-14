// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationSnippetCompileTests.FindMarkedSnippetFiles.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class DocumentationSnippetCompileTests
{
    /// <summary>
    /// Verifies that the files whose opted-in snippets are compiled include the Outlook landing pages, whose
    /// <c>getting-started.md</c> and <c>index.md</c> mark snippets for compilation against both readers.
    /// </summary>
    /// <param name="fileName">The landing page's file name in <c>docs/docs/outlook</c>.</param>
    [TestMethod]
    [DataRow("getting-started.md")]
    [DataRow("index.md")]
    public void FindMarkedSnippetFiles_WhenRepositoryIsFound_ShouldIncludeOutlookLandingPage(string fileName)
    {
        IReadOnlyList<string>? files = FindMarkedSnippetFiles();
        if (files is null)
        {
            Assert.Inconclusive("The repository root was not found from the test base directory.");
            return;
        }

        string expected = Path.Combine("docs", "docs", "outlook", fileName);

        Assert.IsTrue(
            files.Any(file => file.EndsWith(Path.DirectorySeparatorChar + expected, StringComparison.Ordinal)),
            $"{expected} is not among the files whose marked snippets are compiled.");
    }

    /// <summary>
    /// Verifies that the PST container pages, which this test has always compiled, stay among the files whose
    /// opted-in snippets are compiled.
    /// </summary>
    [TestMethod]
    public void FindMarkedSnippetFiles_WhenRepositoryIsFound_ShouldIncludeIoPstPages()
    {
        IReadOnlyList<string>? files = FindMarkedSnippetFiles();
        if (files is null)
        {
            Assert.Inconclusive("The repository root was not found from the test base directory.");
            return;
        }

        string expected = Path.Combine("docs", "docs", "io-pst", "getting-started.md");

        Assert.IsTrue(
            files.Any(file => file.EndsWith(Path.DirectorySeparatorChar + expected, StringComparison.Ordinal)),
            $"{expected} is not among the files whose marked snippets are compiled.");
    }
}
