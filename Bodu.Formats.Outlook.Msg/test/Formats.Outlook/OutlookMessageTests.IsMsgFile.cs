// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.IsMsgFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that a well-formed message stream is recognized and its position restored.
    /// </summary>
    [TestMethod]
    public void IsMsgFile_WhenMessageStream_ShouldReturnTrueAndRestorePosition()
    {
        using MemoryStream container = CreateMinimalContainer();

        Assert.IsTrue(OutlookMessage.IsMsgFile(container));
        Assert.AreEqual(0, container.Position);
    }

    /// <summary>
    /// Verifies that a non-compound stream is rejected.
    /// </summary>
    [TestMethod]
    public void IsMsgFile_WhenNotCompoundFile_ShouldReturnFalse()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.IsFalse(OutlookMessage.IsMsgFile(stream));
    }

    /// <summary>
    /// Verifies that a compound file without the message property stream is rejected.
    /// </summary>
    [TestMethod]
    public void IsMsgFile_WhenCompoundFileIsNotMessage_ShouldReturnFalse()
    {
        using MemoryStream container = new MsgFixtureBuilder().OmitPropertiesStream().Build();

        Assert.IsFalse(OutlookMessage.IsMsgFile(container));
    }
}
