// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that a message view refuses every member once its session is disposed, even after its properties,
    /// recipients, and attachments were materialized and cached.
    /// </summary>
    [TestMethod]
    public void Subject_WhenSessionDisposedAfterDecode_ShouldThrowObjectDisposedException()
    {
        OutlookMailStore store = OpenSynthetic();
        OutlookMailMessage message = GetFullMessage(store);
        _ = message.Subject;
        _ = message.Recipients;
        _ = message.Attachments;

        store.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = message.Subject;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = message.Recipients;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = message.Attachments;
        });
    }
}
