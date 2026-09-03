// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that a disposed session refuses further access and that disposing twice is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldInvalidateSession()
    {
        OutlookMailStore store = OpenSample1();
        store.Dispose();
        store.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = store.Properties;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = store.RootFolder;
        });
    }

    /// <summary>
    /// Verifies that a folder view obtained before disposal cannot read after the session is disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenViewsOutliveSession_ShouldInvalidateViews()
    {
        OutlookMailStore store = OpenSample1();
        OutlookMailFolder root = store.RootFolder;
        _ = store.Properties;
        store.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.EnumerateSubfolders().ToList();
        });
    }
}
