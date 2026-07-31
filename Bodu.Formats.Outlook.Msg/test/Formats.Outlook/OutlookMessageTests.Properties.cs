// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that every scalar convenience surfaces its underlying property.
    /// </summary>
    [TestMethod]
    public void Properties_WhenConvenienceFieldsPresent_ShouldSurfaceValues()
    {
        var sent = new DateTimeOffset(2026, 7, 30, 8, 0, 0, TimeSpan.Zero);
        var received = new DateTimeOffset(2026, 7, 30, 8, 1, 30, TimeSpan.Zero);
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Quarterly report")
            .AddUnicode(MapiPropertyIds.SenderName, "Alex Doe")
            .AddUnicode(MapiPropertyIds.SenderEmailAddress, "alex@example.com")
            .AddUnicode(MapiPropertyIds.InternetMessageId, "<id@example.com>")
            .AddUnicode(MapiPropertyIds.TransportMessageHeaders, "Received: by example")
            .AddFixedEntry(0x00390040, (ulong)sent.ToFileTime())
            .AddFixedEntry(0x0E060040, (ulong)received.ToFileTime())
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("Quarterly report", message.Subject);
        Assert.AreEqual("Alex Doe", message.SenderName);
        Assert.AreEqual("alex@example.com", message.SenderEmailAddress);
        Assert.AreEqual("<id@example.com>", message.InternetMessageId);
        Assert.AreEqual("Received: by example", message.TransportMessageHeaders);
        Assert.AreEqual(sent, message.SentTime);
        Assert.AreEqual(received, message.ReceivedTime);
    }

    /// <summary>
    /// Verifies that absent convenience fields surface as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Properties_WhenConvenienceFieldsAbsent_ShouldReturnNull()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Only a subject")
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.IsNull(message.SenderName);
        Assert.IsNull(message.SenderEmailAddress);
        Assert.IsNull(message.InternetMessageId);
        Assert.IsNull(message.TransportMessageHeaders);
        Assert.IsNull(message.SentTime);
        Assert.IsNull(message.ReceivedTime);
    }

    /// <summary>
    /// Verifies that the raw property collection exposes every decoded property.
    /// </summary>
    [TestMethod]
    public void Properties_WhenAccessed_ShouldExposeDecodedCollection()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(2, message.Properties.Count);
        Assert.IsTrue(message.Properties.Contains(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode)));
    }
}
