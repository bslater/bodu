// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreReferenceCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Full-decode sweeps over the real reference corpus: every folder, message, recipient, attachment, and body of every
/// Unicode fixture must decode — under the tolerant and strict levels alike — exercising the complete messaging
/// surface against writer-produced files.
/// </summary>
[TestClass]
public sealed class OutlookMailStoreReferenceCorpusTests
{
    /// <summary>The Unicode reference fixtures the sweeps cover.</summary>
    private static readonly string[] s_unicodeFixtures =
        [OutlookMailStoreTests.Sample1, "unicode/test_unicode.pst"];

    /// <summary>
    /// Verifies that every object of every Unicode fixture decodes fully at the given validation level, with no
    /// exception escaping any part of the messaging surface.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    public void Open_WhenUnicodeFixture_ShouldDecodeEveryObject(string testName, PstValidationLevel level)
    {
        _ = testName;

        foreach (string fixture in s_unicodeFixtures)
        {
            using OutlookMailStore store = OutlookMailStore.Open(
                PstReferenceFixtures.OpenStream(fixture),
                new OutlookMailStoreReaderOptions { ValidationLevel = level });

            _ = store.Properties;
            _ = store.DisplayName;

            int messages = 0;
            foreach (OutlookMailFolder folder in OutlookMailStoreTests.Walk(store.RootFolder))
            {
                _ = folder.Properties;
                _ = folder.DisplayName;
                _ = folder.ContainerClass;
                _ = folder.MessageCount;
                _ = folder.UnreadCount;
                _ = folder.HasSubfolders;

                foreach (OutlookMailMessage message in folder.EnumerateMessages().Concat(folder.EnumerateAssociatedMessages()))
                {
                    DecodeMessage(message, depth: 0);
                    messages++;
                }
            }

            Assert.IsTrue(messages > 0, $"Fixture '{fixture}' must yield at least one message.");
        }
    }

    /// <summary>
    /// Decodes one message's complete surface — properties, conveniences, bodies, recipients, and attachments —
    /// recursing into embedded messages.
    /// </summary>
    /// <param name="message">The message to decode.</param>
    /// <param name="depth">The embedded-message recursion depth, capped defensively.</param>
    private static void DecodeMessage(OutlookMailMessage message, int depth)
    {
        foreach (MapiProperty property in message.Properties)
            _ = property.Value;

        _ = message.Subject;
        _ = message.SenderName;
        _ = message.SenderEmailAddress;
        _ = message.MessageClass;
        _ = message.InternetMessageId;
        _ = message.TransportMessageHeaders;
        _ = message.SentTime;
        _ = message.ReceivedTime;
        _ = message.BodyText;
        _ = message.BodyHtml;
        _ = message.BodyRtf;

        foreach (OutlookRecipient recipient in message.Recipients)
        {
            foreach (MapiProperty property in recipient.Properties)
                _ = property.Value;

            _ = recipient.RecipientType;
            _ = recipient.DisplayName;
            _ = recipient.EmailAddress;
            _ = recipient.AddressType;
        }

        foreach (OutlookMailAttachment attachment in message.Attachments)
        {
            foreach (MapiProperty property in attachment.Properties)
                _ = property.Value;

            _ = attachment.FileName;
            _ = attachment.ContentId;
            _ = attachment.MimeTag;
            _ = attachment.Size;

            if (attachment.Method == OutlookAttachmentMethod.ByValue)
            {
                using Stream content = attachment.OpenContentStream();
                content.CopyTo(Stream.Null);
            }
            else if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage && depth < 8)
            {
                DecodeMessage(attachment.OpenMessage(), depth + 1);
            }
        }
    }
}
