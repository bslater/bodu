// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalogTests.CountByClass.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteCatalogTests
{
    /// <summary>
    /// Verifies that the rows are counted in each class, in the order of <see cref="ReleaseNoteCatalog.Classes" />.
    /// </summary>
    [TestMethod]
    public void CountByClass_WhenCatalogHoldsEveryClass_ShouldCountEach()
    {
        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.LoadEmbedded(typeof(ReleaseNoteCatalogTests).Assembly)[0];

        IReadOnlyDictionary<string, int> counts = catalog.CountByClass();

        CollectionAssert.AreEqual(new[] { "applies", "dialect", "n/a", "unknown" }, counts.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { 5, 1, 1, 1 }, counts.Values.ToArray());
    }

    /// <summary>
    /// Verifies that a class with no rows counts zero.
    /// </summary>
    [TestMethod]
    public void CountByClass_WhenAClassHasNoRows_ShouldCountZero()
    {
        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", Catalog(ValidRow));

        IReadOnlyDictionary<string, int> counts = catalog.CountByClass();

        Assert.AreEqual(1, counts["applies"]);
        Assert.AreEqual(0, counts["dialect"]);
        Assert.AreEqual(0, counts["n/a"]);
        Assert.AreEqual(0, counts["unknown"]);
    }
}
