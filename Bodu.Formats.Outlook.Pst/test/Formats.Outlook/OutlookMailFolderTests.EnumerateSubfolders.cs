// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailFolderTests.EnumerateSubfolders.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailFolderTests
{
    /// <summary>
    /// Verifies that walking the hierarchy from the root reaches every oracle folder name recorded in the reference
    /// manifest, under the tolerant and strict validation levels alike.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    public void EnumerateSubfolders_WhenWalkedFromRoot_ShouldReachOracleFolders(string testName, PstValidationLevel level)
    {
        _ = testName;

        PstReferenceFixture fixture = PstReferenceFixtures.Manifest.Fixtures
            .Single(f => f.File == OutlookMailStoreTests.Sample1);
        string[] oracleFolders = [.. fixture.LspstListing
            .Where(line => line.StartsWith("Folder", StringComparison.Ordinal))
            .Select(line => line.Split('"')[1])];
        Assert.IsTrue(oracleFolders.Length > 0, "The manifest must carry at least one oracle folder name.");

        using OutlookMailStore store = OutlookMailStore.Open(
            PstReferenceFixtures.OpenStream(OutlookMailStoreTests.Sample1),
            new OutlookMailStoreReaderOptions { ValidationLevel = level });

        var walked = OutlookMailStoreTests.Walk(store.RootFolder)
            .Select(f => f.DisplayName)
            .Where(name => name is not null)
            .ToList();

        foreach (string oracle in oracleFolders)
            CollectionAssert.Contains(walked, oracle, $"The walk must reach oracle folder '{oracle}'.");
    }

    /// <summary>
    /// Verifies that the folder conveniences surface the folder's declared counts and flags consistently with its
    /// enumerations for the oracle folder.
    /// </summary>
    [TestMethod]
    public void EnumerateSubfolders_WhenOracleFolder_ShouldAgreeWithDeclaredCounts()
    {
        using OutlookMailStore store = OutlookMailStoreTests.OpenSample1();

        OutlookMailFolder sample = OutlookMailStoreTests.Walk(store.RootFolder)
            .First(f => f.DisplayName == "Sample1");

        int messageCount = sample.EnumerateMessages().Count();
        if (sample.MessageCount is int declared)
            Assert.AreEqual(declared, messageCount, "The declared content count must match the contents table.");

        Assert.IsTrue(messageCount >= 1, "The oracle folder holds at least the oracle message.");
        Assert.AreEqual(sample.EnumerateSubfolders().Any(), sample.HasSubfolders);
    }
}
