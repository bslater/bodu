// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatCensusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Drives <see cref="KatCensus" /> over the test assembly and refreshes the committed <c>kat-census.txt</c> artifact —
/// the go-forward inventory of every known-answer vector's inputs and outputs.
/// </summary>
/// <remarks>
/// The driver is tagged <c>Census</c> so it runs only on demand, not as part of the default build verification. The
/// generated file is committed; a future change that alters any vector's inputs or outputs shows up as a diff against it.
/// </remarks>
[TestClass]
public sealed class KatCensusTests
{
    /// <summary>
    /// The families the census must contain for coverage to be considered complete.
    /// </summary>
    private static readonly string[] ExpectedFamilies =
    [
        "digest",
        "block-cipher",
        "stream-cipher",
        "aead",
        "kdf",
        "key-agreement",
        "signature",
        "kem",
    ];

    /// <summary>
    /// Verifies that the census enumerates every expected KAT family and regenerates the committed census artifact.
    /// </summary>
    [TestMethod]
    [TestCategory("Census")]
    public void Collect_ShouldCoverEveryFamily_AndWriteCensusArtifact()
    {
        IReadOnlyList<KatCensus.CensusEntry> entries = KatCensus.Collect(typeof(KatCensusTests).Assembly);

        Assert.IsTrue(entries.Count > 0, "The census must contain at least one vector.");

        var families = entries.Select(e => e.Family).ToHashSet(StringComparer.Ordinal);
        foreach (string family in ExpectedFamilies)
            Assert.IsTrue(families.Contains(family), $"The census is missing the '{family}' family.");

        string census = KatCensus.Render(entries);
        File.WriteAllText(CensusArtifactPath(), census);
    }

    /// <summary>
    /// Resolves the path of the committed census artifact, relative to this source file at the test-project root.
    /// </summary>
    /// <param name="thisFile">The compiler-supplied path of this source file.</param>
    /// <returns>The absolute path of <c>kat-census.txt</c> at the test-project root.</returns>
    private static string CensusArtifactPath([CallerFilePath] string thisFile = "")
    {
        // thisFile lives in <project>/test/Security.Cryptography.Infrastructure/; the artifact sits at <project>/test/.
        string infrastructureDirectory = Path.GetDirectoryName(thisFile)!;
        string testRoot = Path.GetDirectoryName(infrastructureDirectory)!;
        return Path.Combine(testRoot, "kat-census.txt");
    }
}
