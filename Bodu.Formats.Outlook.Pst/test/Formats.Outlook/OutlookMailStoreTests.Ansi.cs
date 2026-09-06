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
    /// Verifies that the second ANSI corpus store exposes the folder the <c>lspst</c> oracle lists and that every
    /// message it holds decodes; the oracle counted no e-mail items, but the store carries message nodes of other
    /// classes, so only the folder is pinned.
    /// </summary>
    [TestMethod]
    public void RootFolder_WhenSecondAnsiCorpusStore_ShouldExposeOracleFolder()
    {
        using OutlookMailStore store = OutlookMailStore.OpenRead(PstReferenceFixtures.OpenStream(TestAnsi));

        var folders = Walk(store.RootFolder).ToList();

        Assert.IsTrue(folders.Any(static f => f.DisplayName == "Folder"), $"Folders: {string.Join(", ", folders.Select(static f => f.DisplayName))}");
        foreach (OutlookMailMessage message in folders.SelectMany(static f => f.EnumerateMessages()))
        {
            _ = message.Subject;
            _ = message.Properties;
        }
    }
}
