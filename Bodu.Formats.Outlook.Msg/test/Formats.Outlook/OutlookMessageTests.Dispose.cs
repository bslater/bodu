// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that disposing twice is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        using MemoryStream container = CreateMinimalContainer();
        var message = OutlookMessage.OpenRead(container, leaveOpen: true);

        message.Dispose();
        message.Dispose();
    }

    /// <summary>
    /// Verifies that accessing the property surface after dispose throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenPropertiesAccessedAfter_ShouldThrowObjectDisposedException()
    {
        using MemoryStream container = CreateMinimalContainer();
        var message = OutlookMessage.OpenRead(container, leaveOpen: true);
        message.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = message.Properties;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = message.Subject;
        });
    }

    /// <summary>
    /// Verifies that disposing the root session invalidates a nested message obtained from an attachment, even when
    /// the nested session has already decoded its properties: the nested view shares the root's container and must
    /// not keep answering from its cache once that container is gone.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenRootDisposed_ShouldInvalidateNestedMessage()
    {
        using MemoryStream container = Bodu.Formats.Outlook.Msg.MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded.AddUnicode(MapiPropertyIds.Subject, "Inner")))
            .Build();

        var message = OutlookMessage.OpenRead(container, leaveOpen: true);
        OutlookMessage nested = message.Attachments[0].OpenMessage();
        Assert.AreEqual("Inner", nested.Subject);

        message.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = nested.Subject;
        });
    }

    /// <summary>
    /// Verifies that disposing a message opened from a path, after it has read its subject, leaves the file unlocked.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenOpenedFromPath_ShouldReleaseTheFile()
    {
        using MemoryStream container = new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.Subject, "Hello").Build();
        string path = WriteTemporaryMessage(container);

        try
        {
            var message = OutlookMessage.OpenRead(path);
            Assert.AreEqual("Hello", message.Subject);

            message.Dispose();

            AssertFileIsReleased(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that disposing a message opened from a path, after reading its attachments failed because the
    /// attachment storage's <c>__properties_version1.0</c> stream is too short to hold its header (a failure at every
    /// validation level), leaves the file unlocked.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenAttachmentsFailToLoad_ShouldReleaseTheFile()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Hello")
            .AddStorage(
                MsgStreamNames.AttachmentStoragePrefix + "00000000",
                new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment)
                    .OmitPropertiesStream()
                    .AddRawStream(MsgStreamNames.PropertiesStreamName, [1, 2, 3]))
            .Build();
        string path = WriteTemporaryMessage(container);

        try
        {
            var message = OutlookMessage.OpenRead(path);
            _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
            {
                _ = message.Attachments;
            });

            message.Dispose();

            AssertFileIsReleased(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that disposing only the root of a message opened from a path, while a nested message opened from one of
    /// its attachments is still in use, leaves the file unlocked and makes the nested message throw
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenNestedMessagesAreOpen_ShouldReleaseTheFile()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Outer")
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded.AddUnicode(MapiPropertyIds.Subject, "Inner")))
            .Build();
        string path = WriteTemporaryMessage(container);

        try
        {
            var message = OutlookMessage.OpenRead(path);
            OutlookMessage nested = message.Attachments[0].OpenMessage();
            Assert.AreEqual("Inner", nested.Subject);

            message.Dispose();

            AssertFileIsReleased(path);
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = nested.Subject;
            });
        }
        finally
        {
            File.Delete(path);
        }
    }
}
