// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailFolderTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailFolderTests
{
    /// <summary>
    /// Verifies that a code page declared on a folder is inherited by the messages it contains when they declare
    /// none of their own — the encoding chain runs store → folder → message → attachment → embedded message.
    /// </summary>
    [TestMethod]
    public void Properties_WhenFolderDeclaresCodePage_ShouldBeInheritedByItsMessages()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b =>
        {
            b.InboxCodePage = PstMessagingFixtureBuilder.MessageCodePage;
            b.MessageDeclaresCodePage = false;
        });

        OutlookMailFolder inbox = store.RootFolder.EnumerateSubfolders().Single();
        OutlookMailMessage message = inbox.EnumerateMessages()
            .Single(static m => m.Subject == PstMessagingFixtureBuilder.NormalizedSubject);

        Assert.AreEqual(PstMessagingFixtureBuilder.MessageCodePage, inbox.Properties.GetInt32(MapiPropertyIds.MessageCodepage));
        Assert.AreEqual(
            PstMessagingFixtureBuilder.BodyText,
            message.BodyText,
            "A message without its own code page must decode under the folder's declaration.");
    }
}
