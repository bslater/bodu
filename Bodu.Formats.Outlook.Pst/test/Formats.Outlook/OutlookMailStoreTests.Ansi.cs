// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that the ANSI corpus store walks to the folder and message the <c>lspst</c> oracle lists, with the
    /// code-page strings decoded.
    /// </summary>
    [TestMethod]
    [DataRow(PstValidationLevel.Compatible)]
    [DataRow(PstValidationLevel.Strict)]
    public void RootFolder_WhenAnsiCorpusStore_ShouldWalkToOracleMessage(PstValidationLevel validationLevel)
    {
        using OutlookMailStore store = OutlookMailStore.Open(
            PstReferenceFixtures.OpenStream(Sample2Ansi),
            new OutlookMailStoreReaderOptions { ValidationLevel = validationLevel });

        var folders = Walk(store.RootFolder).ToList();
        OutlookMailFolder sample = folders.Single(static f => f.DisplayName == "Sample2");
        OutlookMailMessage message = sample.EnumerateMessages().Single();

        Assert.AreEqual("Here is a sample message", message.Subject);
        Assert.AreEqual("Terry Mahaffey", message.SenderName);
        Assert.IsNotNull(message.BodyText);
    }

    /// <summary>
    /// Verifies that the empty ANSI corpus store exposes its single folder and no messages.
    /// </summary>
    [TestMethod]
    public void RootFolder_WhenEmptyAnsiCorpusStore_ShouldExposeSingleFolder()
    {
        using OutlookMailStore store = OutlookMailStore.OpenRead(PstReferenceFixtures.OpenStream(TestAnsi));

        var folders = Walk(store.RootFolder).ToList();

        Assert.IsTrue(folders.Any(static f => f.DisplayName == "Folder"), $"Folders: {string.Join(", ", folders.Select(static f => f.DisplayName))}");
        Assert.AreEqual(0, folders.Sum(static f => f.EnumerateMessages().Count()));
    }
}
