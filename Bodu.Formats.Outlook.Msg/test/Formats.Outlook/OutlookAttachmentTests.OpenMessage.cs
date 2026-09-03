// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.OpenMessage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookAttachmentTests
{
    /// <summary>
    /// Verifies that an embedded message opens with its own properties, recipients, and attachments.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenEmbeddedMessage_ShouldOpenNestedSession()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded
                    .AddUnicode(MapiPropertyIds.Subject, "Inner subject")
                    .AddRecipient(recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Nested Recipient"))))
            .Build();

        using var message = OutlookMessage.OpenRead(container);
        OutlookMessage nested = message.Attachments[0].OpenMessage();

        Assert.AreEqual(OutlookAttachmentMethod.EmbeddedMessage, message.Attachments[0].Method);
        Assert.AreEqual("Inner subject", nested.Subject);
        Assert.AreEqual(1, nested.Recipients.Count);
        Assert.AreEqual("Nested Recipient", nested.Recipients[0].DisplayName);
    }

    /// <summary>
    /// Verifies that messages nest recursively to depth two.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestedTwice_ShouldOpenInnerMostMessage()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(outer => outer
                .AddEmbeddedMessage(middle => middle
                    .AddUnicode(MapiPropertyIds.Subject, "Middle")
                    .AddAttachment(inner => inner
                        .AddEmbeddedMessage(innermost => innermost
                            .AddUnicode(MapiPropertyIds.Subject, "Innermost")))))
            .Build();

        using var message = OutlookMessage.OpenRead(container);
        OutlookMessage middleMessage = message.Attachments[0].OpenMessage();
        OutlookMessage innermostMessage = middleMessage.Attachments[0].OpenMessage();

        Assert.AreEqual("Middle", middleMessage.Subject);
        Assert.AreEqual("Innermost", innermostMessage.Subject);
    }

    /// <summary>
    /// Verifies that disposing a nested message is a no-op: the root and even the nested session's decoded properties
    /// remain readable.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestedDisposed_ShouldNotAffectRootOrNestedProperties()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded.AddUnicode(MapiPropertyIds.Subject, "Inner")))
            .Build();

        using var message = OutlookMessage.OpenRead(container, leaveOpen: true);
        OutlookMessage nested = message.Attachments[0].OpenMessage();

        nested.Dispose();

        Assert.AreEqual("Test subject", message.Subject);
        Assert.AreEqual("Inner", nested.Subject);
    }

    /// <summary>
    /// Verifies that opening a message on a non-embedded attachment throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNotEmbeddedMessage_ShouldThrowNotSupportedException()
    {
        byte[] payload = { 1, 2, 3 };
        using MemoryStream container = CreateWithByValueAttachment(payload);

        using var message = OutlookMessage.OpenRead(container);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = message.Attachments[0].OpenMessage();
        });
    }

    /// <summary>
    /// Builds a message whose attachments nest embedded messages to the requested depth.
    /// </summary>
    /// <param name="depth">The number of nested levels beneath the root.</param>
    /// <returns>The container stream.</returns>
    private static MemoryStream CreateNested(int depth)
    {
        MsgFixtureBuilder root = MsgFixtureBuilder.CreateMinimal();
        Nest(root, depth);
        return root.Build();

        static void Nest(MsgFixtureBuilder parent, int remaining)
        {
            if (remaining == 0)
                return;

            parent.AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded =>
                {
                    embedded.AddUnicode(MapiPropertyIds.Subject, $"Level {remaining}");
                    Nest(embedded, remaining - 1);
                }));
        }
    }

    /// <summary>
    /// Verifies that opening an embedded message deeper than <see cref="OutlookMessageReaderOptions.MaxEmbeddedMessageDepth" />
    /// throws <see cref="OutlookMsgFormatException" /> at the boundary, while every level within the limit opens.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestingExceedsMaxEmbeddedMessageDepth_ShouldThrowOutlookMsgFormatException()
    {
        using MemoryStream container = CreateNested(3);
        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { MaxEmbeddedMessageDepth = 2 });

        OutlookMessage first = message.Attachments[0].OpenMessage();
        OutlookMessage second = first.Attachments[0].OpenMessage();
        Assert.AreEqual("Level 2", second.Subject);

        OutlookAttachment third = second.Attachments[0];
        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = third.OpenMessage();
        });
    }

    /// <summary>
    /// Verifies that the default nesting limit admits a realistically deep chain.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestingWithinDefaultLimit_ShouldOpenEveryLevel()
    {
        using MemoryStream container = CreateNested(4);
        using var message = OutlookMessage.OpenRead(container);

        OutlookMessage current = message;
        for (int level = 4; level >= 1; level--)
        {
            current = current.Attachments[0].OpenMessage();
            Assert.AreEqual($"Level {level}", current.Subject);
        }
    }
}
