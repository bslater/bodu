// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

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

    /// <summary>
    /// Verifies that the message-class convenience surfaces <c>PidTagMessageClass</c>, matching the PST reader's
    /// view.
    /// </summary>
    [TestMethod]
    public void MessageClass_WhenPresent_ShouldReturnValue()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddUnicode(MapiPropertyIds.MessageClass, "IPM.Note")
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("IPM.Note", message.MessageClass);
    }

    /// <summary>
    /// Verifies that a property record whose 16 bytes are all zero is padding: at every validation level it is skipped,
    /// and the properties hold only the subject recorded before it.
    /// </summary>
    /// <param name="level">The validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Properties_WhenRecordIsAllZero_ShouldSkipIt(CompoundValidationLevel level)
    {
        MsgFixtureBuilder builder = new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.Subject, "Short");
        byte[] properties = [.. builder.BuildPropertiesPayload(), .. new byte[16]];
        using MemoryStream container = builder
            .OmitPropertiesStream()
            .AddRawStream(MsgStreamNames.PropertiesStreamName, properties)
            .Build();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        Assert.AreEqual(1, message.Properties.Count);
        Assert.AreEqual("Short", message.Subject);
        Assert.IsFalse(message.Properties.Contains(new MapiPropertyTag(0u)));
    }

    /// <summary>
    /// Verifies that a record with tag 0 and nonzero flags is not padding: it is surfaced as property
    /// <c>0x00000000</c>, as before.
    /// </summary>
    [TestMethod]
    public void Properties_WhenTagIsZeroAndFlagsAreNot_ShouldKeepTheRecord()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Short")
            .AddFixedEntry(0x00000000, 0)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(2, message.Properties.Count);
        Assert.IsTrue(message.Properties.Contains(new MapiPropertyTag(0u)));
    }

    /// <summary>
    /// Verifies that a string property whose value stream holds bytes after its NUL terminator ends at the terminator,
    /// at every validation level: a <c>Unicode</c> subject and a <c>String8</c> normalized subject, each stored as
    /// <c>Hello</c>, NUL, <c>junk</c> under a record size that matches its stream, both read as <c>Hello</c>.
    /// </summary>
    /// <param name="level">The validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Properties_WhenStringHoldsBytesAfterItsTerminator_ShouldEndAtTheTerminator(CompoundValidationLevel level)
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x0037001F, 20)
            .AddRawStream(MsgStreamNames.GetSubstgStreamName(0x0037001F), System.Text.Encoding.Unicode.GetBytes("Hello\0junk"))
            .AddEntryWithoutStream(0x0E1D001E, 10)
            .AddRawStream(MsgStreamNames.GetSubstgStreamName(0x0E1D001E), "Hello\0junk"u8.ToArray())
            .Build();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        Assert.AreEqual("Hello", message.Properties.GetString(MapiPropertyIds.Subject));
        Assert.AreEqual("Hello", message.Properties.GetString(0x0E1D));
    }

    /// <summary>
    /// Verifies that each element of a multi-valued string property ends at its NUL terminator.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMultiValuedStringElementHoldsBytesAfterItsTerminator_ShouldEndAtTheTerminator()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddMultiValueUnicode(0x6844, "Alpha\0junk", "Beta")
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        string[]? values = message.Properties.GetStringArray(0x6844);
        Assert.IsNotNull(values);
        Assert.AreEqual(2, values.Length);
        Assert.AreEqual("Alpha", values[0]);
        Assert.AreEqual("Beta", values[1]);
    }

    /// <summary>
    /// Verifies that a multi-valued <c>AppTime</c> property (<c>PtypMultipleFloatingTime</c>) decodes to an array of its
    /// OLE Automation dates at every validation level, as a single-valued <c>AppTime</c> decodes to its value.
    /// </summary>
    /// <param name="level">The validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Properties_WhenMultiValuedAppTime_ShouldDecodeEveryElement(CompoundValidationLevel level)
    {
        var packed = new byte[16];
        BinaryPrimitives.WriteDoubleLittleEndian(packed, 45000.5);
        BinaryPrimitives.WriteDoubleLittleEndian(packed.AsSpan(8), -1.25);
        using MemoryStream container = new MsgFixtureBuilder()
            .AddPackedMultiValue(0x6002, MapiPropertyType.AppTime, packed)
            .Build();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        Assert.IsTrue(message.Properties.TryGetValue(new MapiPropertyTag(0x60021007u), out MapiProperty? property));
        CollectionAssert.AreEqual(new[] { 45000.5, -1.25 }, (double[])property.Value!);
    }

    /// <summary>
    /// Verifies that a message declaring code page 65000 decodes its <c>String8</c> properties as UTF-7 (RFC 2152): a
    /// subject stored as <c>Gr+APYA3w-e</c> reads as <c>Größe</c> rather than as its Windows-1252 characters.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMessageCodePageIsUtf7_ShouldDecodeString8AsUtf7()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x3FFD0003, 65000)
            .AddString8(MapiPropertyIds.Subject, "Gr+APYA3w-e", 1252)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("Größe", message.Properties.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that a <c>String8</c> value of a message declaring a UTF-32 code page, which cannot describe a
    /// code-page string, decodes through the Windows-1252 fallback: the bytes <c>48 69</c> read as <c>Hi</c> rather
    /// than as a replacement character.
    /// </summary>
    /// <param name="testName">The declared code page, by name.</param>
    /// <param name="codePage">The declared message code page.</param>
    [TestMethod]
    [DataRow("Utf32LE", 12000)]
    [DataRow("Utf32BE", 12001)]
    public void Properties_WhenMessageCodePageIsUtf32_ShouldDecodeString8ThroughTheFallback(
        string testName,
        int codePage)
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x3FFD0003, (ulong)codePage)
            .AddString8(MapiPropertyIds.Subject, "Hi", 1252)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("Hi", message.Properties.GetString(MapiPropertyIds.Subject), testName);
    }
}
