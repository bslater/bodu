// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalogTests.LoadEmbedded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteCatalogTests
{
    /// <summary>
    /// Verifies that the catalogue this assembly embeds under <c>Fixtures/ReleaseNotes/</c> is found, named by its file
    /// name, and read with no problems.
    /// </summary>
    [TestMethod]
    public void LoadEmbedded_WhenAssemblyEmbedsACatalogue_ShouldLoadItByFileName()
    {
        IReadOnlyList<ReleaseNoteCatalog> catalogs = ReleaseNoteCatalog.LoadEmbedded(typeof(ReleaseNoteCatalogTests).Assembly);

        Assert.AreEqual(1, catalogs.Count);
        Assert.AreEqual("sample-fixes.csv", catalogs[0].FileName);
        Assert.AreEqual(0, catalogs[0].Problems.Count, string.Join(Environment.NewLine, catalogs[0].Problems));
        Assert.AreEqual(8, catalogs[0].Rows.Count);
    }

    /// <summary>
    /// Verifies that loading from a <see langword="null" /> assembly throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void LoadEmbedded_WhenAssemblyIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ReleaseNoteCatalog.LoadEmbedded(null!);
        });

        Assert.AreEqual("assembly", ex.ParamName);
    }
}
