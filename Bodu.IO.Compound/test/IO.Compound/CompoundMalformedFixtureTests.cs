// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundMalformedFixtureTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// Validates the reader against an independent corpus of deliberately malformed compound files, proving that broken
/// input is always handled safely.
/// </summary>
[TestClass]
public class CompoundMalformedFixtureTests
{
    /// <summary>
    /// Verifies that each structurally invalid fixture is rejected with a <see cref="CompoundFileFormatException" /> of
    /// the expected stable category.
    /// </summary>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.RejectedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenFixtureIsStructurallyInvalid_ShouldThrowWithExpectedCategory(CompoundMalformedKat kat)
    {
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
            using var file = CompoundFile.Open(source);
            _ = Enumerate(file.RootStorage);
        });

        Assert.AreEqual(kat.Category, ex.Category);
    }

    /// <summary>
    /// Verifies that each tolerated (recoverable) malformed fixture opens and enumerates safely without throwing.
    /// </summary>
    /// <param name="kat">The malformed fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundMalformedFixtures.ToleratedFixtures), typeof(CompoundMalformedFixtures),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenFixtureIsRecoverable_ShouldOpenAndEnumerateSafely(CompoundMalformedKat kat)
    {
        using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
        using var file = CompoundFile.Open(source);

        int count = Enumerate(file.RootStorage);

        Assert.IsGreaterThanOrEqualTo(0, count);
    }

    /// <summary>
    /// Recursively enumerates a storage hierarchy, returning the total entry count.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>The number of entries visited.</returns>
    private static int Enumerate(CompoundStorage storage)
    {
        int count = 0;
        foreach (CompoundEntryInfo _ in storage.EnumerateEntries())
            count++;

        foreach (CompoundStorage child in storage.EnumerateStorages())
            count += Enumerate(child);

        return count;
    }
}
