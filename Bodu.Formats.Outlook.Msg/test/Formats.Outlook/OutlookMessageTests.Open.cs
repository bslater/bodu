// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that a message read from a <see cref="MemoryStream" /> over a segment of a larger array, between filler
    /// bytes, is read from the segment's start rather than from the start of the array.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsAnArraySegment_ShouldReadFromItsStart()
    {
        using MemoryStream container = new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.Subject, "Segment").Build();
        byte[] message = container.ToArray();
        var buffer = new byte[message.Length + 2048];
        Array.Fill(buffer, (byte)0xA5);
        message.CopyTo(buffer, 1024);

        using var segment = new MemoryStream(buffer, 1024, message.Length, writable: false, publiclyVisible: true);
        using var opened = OutlookMessage.Open(segment, new OutlookMessageReaderOptions());

        Assert.AreEqual("Segment", opened.Subject);
    }
}
