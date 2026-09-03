// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
