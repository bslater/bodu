// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalogTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Test.Corpus;

/// <summary>
/// Verifies <see cref="ReleaseNoteCatalog" />: reading a catalogue's header and rows, and reporting every problem in a
/// malformed one.
/// </summary>
[TestClass]
public sealed partial class ReleaseNoteCatalogTests
{
    /// <summary>The three required header lines of a catalogue for the <c>sample</c> library.</summary>
    private const string Header =
        "# library: sample -- https://example.invalid/sample\n# releases: synthetic\n# licence: MIT\n";

    /// <summary>A well-formed runnable row of the <c>sample</c> library.</summary>
    private const string ValidRow =
        "sample,1.0.0,#1,Reject a truncated document,applies,,reject,,abc,FormatException,upstream,a reason";

    /// <summary>
    /// Builds a catalogue for the <c>sample</c> library from rows, after the required header and the column line.
    /// </summary>
    /// <param name="rows">The rows, each without its line feed.</param>
    /// <returns>The catalogue's bytes.</returns>
    private static byte[] Catalog(params string[] rows) =>
        Encoding.ASCII.GetBytes(Header + ReleaseNoteCatalog.ColumnLine + "\n" + string.Concat(rows.Select(row => row + "\n")));

    /// <summary>
    /// Asserts that a catalogue reports exactly one problem, mentioning the given text.
    /// </summary>
    /// <param name="catalog">The loaded catalogue.</param>
    /// <param name="fragment">Text the problem must contain.</param>
    private static void AssertSingleProblem(ReleaseNoteCatalog catalog, string fragment)
    {
        Assert.AreEqual(1, catalog.Problems.Count, string.Join(Environment.NewLine, catalog.Problems));
        Assert.IsTrue(catalog.Problems[0].Contains(fragment, StringComparison.Ordinal), catalog.Problems[0]);
    }
}
