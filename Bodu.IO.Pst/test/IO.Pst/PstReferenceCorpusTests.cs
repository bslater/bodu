// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReferenceCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the embedded reference corpus against its seed manifest: the bytes shipped are the bytes provenance was
/// recorded for.
/// </summary>
[TestClass]
public class PstReferenceCorpusTests
{
    /// <summary>
    /// Gets one manifest row per corpus fixture.
    /// </summary>
    /// <value>One row per fixture.</value>
    public static IEnumerable<object[]> AllFixtureRows =>
        PstReferenceFixtures.Manifest.Fixtures.Select(f => new object[] { f });

    /// <summary>
    /// Verifies that each embedded fixture's size and SHA-256 match the values the seed manifest recorded at
    /// acquisition, so the corpus cannot drift from its provenance record.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(AllFixtureRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Fixture_WhenHashed_ShouldMatchManifestProvenance(PstReferenceFixture fixture)
    {
        byte[] bytes;
        using (MemoryStream stream = PstReferenceFixtures.OpenStream(fixture.File))
            bytes = stream.ToArray();

        Assert.AreEqual(fixture.SizeBytes, bytes.LongLength);
        Assert.AreEqual(fixture.Sha256, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }
}
