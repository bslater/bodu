// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailFolderTests.EnumerateMessages.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailFolderTests
{
    /// <summary>
    /// Verifies that enumerating a folder's messages after an earlier enumeration stopped at the first message returns
    /// every message in order, so a partly read contents table is never taken for the whole of it.
    /// </summary>
    [TestMethod]
    public void EnumerateMessages_WhenEnumeratedPartlyThenFully_ShouldReturnEveryMessage()
    {
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput("gen:Mail(messages=3)");
        using OutlookMailStore store = OutlookMailStore.OpenRead(new MemoryStream(input, writable: false));
        OutlookMailFolder inbox = store.RootFolder.EnumerateSubfolders().Single(folder => folder.DisplayName == "Inbox");
        OutlookMailMessage first = inbox.EnumerateMessages().First();

        string?[] subjects = [.. inbox.EnumerateMessages().Select(message => message.Subject)];

        Assert.AreEqual("Message 1", first.Subject);
        CollectionAssert.AreEqual(new[] { "Message 1", "Message 2", "Message 3" }, subjects);
    }

    /// <summary>
    /// Verifies that enumerating the messages of a folder whose contents table has a corrupt heap throws
    /// <see cref="OutlookPstFormatException" /> as the enumeration runs, keeping the container's
    /// <see cref="PstFileFormatException" /> as the inner exception.
    /// </summary>
    [TestMethod]
    public void EnumerateMessages_WhenContentsTableHeapIsCorrupt_ShouldThrowOutlookPstFormatException()
    {
        // 0x802E is the Inbox's (0x8022) contents table.
        byte[] input = OutlookMailStoreTests.GenerateWithCorruptHeap("gen:Mail(messages=2)", 0x802E);
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), new OutlookMailStoreReaderOptions());
        OutlookMailFolder inbox = store.RootFolder.EnumerateSubfolders().Single();

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = inbox.EnumerateMessages().ToList();
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
        Assert.AreEqual(PstFileError.InvalidHeap, ((PstFileFormatException)ex.InnerException).Error);
    }
}
