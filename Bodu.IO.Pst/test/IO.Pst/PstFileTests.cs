// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstFile" />, the PST reading session.
/// </summary>
[TestClass]
public partial class PstFileTests
{
    /// <summary>The manifest-relative path of the primary Unicode fixture.</summary>
    internal const string Sample1 = "unicode/sample1.pst";

    /// <summary>The manifest-relative path of the secondary Unicode fixture.</summary>
    internal const string TestUnicode = "unicode/test_unicode.pst";

    /// <summary>The ANSI corpus store with one folder and one message.</summary>
    internal const string Sample2Ansi = "ansi/sample2.pst";

    /// <summary>The ANSI corpus store with one empty folder.</summary>
    internal const string TestAnsi = "ansi/test_ansi.pst";

    /// <summary>
    /// Gets the node-census rows: the node-directory facts of each Unicode fixture, verified against the file
    /// contents during the P0 spike and cross-checked against the <c>lspst</c> seed manifest.
    /// </summary>
    /// <value>One row per Unicode fixture.</value>
    public static IEnumerable<object[]> NodeCensusRows =>
        new[]
        {
            new object[] { new PstNodeCensusKat(Sample1, NodeCount: 52, FolderNodeCount: 5, MessageNodeCount: 1) },
            new object[] { new PstNodeCensusKat(TestUnicode, NodeCount: 48, FolderNodeCount: 5, MessageNodeCount: 2) },
        };

    /// <summary>
    /// Gets the manifest rows for the ANSI-format fixtures, which the reader recognizes but rejects.
    /// </summary>
    /// <value>One row per ANSI fixture.</value>
    public static IEnumerable<object[]> AnsiFixtureRows =>
        PstReferenceFixtures.Manifest.Fixtures
            .Where(f => f.Format == "ansi")
            .Select(f => new object[] { f });

    /// <summary>
    /// Opens the primary Unicode fixture with default options.
    /// </summary>
    /// <returns>The open session, owning the fixture stream.</returns>
    internal static PstFile OpenSample1() =>
        PstReferenceFixtures.OpenFile(Sample1);
}

/// <summary>
/// Represents one Unicode fixture's expected node-directory census. Implements <see cref="IKat" /> so data-driven
/// rows display the fixture path in failure diagnostics.
/// </summary>
/// <param name="File">The manifest-relative fixture path.</param>
/// <param name="NodeCount">The expected total node count in the node B-tree.</param>
/// <param name="FolderNodeCount">The expected count of <see cref="PstNodeType.NormalFolder" /> nodes.</param>
/// <param name="MessageNodeCount">The expected count of <see cref="PstNodeType.NormalMessage" /> nodes.</param>
public sealed record PstNodeCensusKat(
    string File,
    int NodeCount,
    int FolderNodeCount,
    int MessageNodeCount) : IKat
{
    /// <summary>
    /// Gets the display label for data-driven rows.
    /// </summary>
    /// <value>The fixture path.</value>
    public string Name =>
        File;
}
