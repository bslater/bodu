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
}
