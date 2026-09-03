// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailFolderTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class OutlookMailFolderTests
{
    /// <summary>
    /// Verifies that a folder view refuses every member once its session is disposed, even after its properties were
    /// decoded and cached — the documented <see cref="ObjectDisposedException" /> guarantee holds on every access,
    /// not only the first.
    /// </summary>
    [TestMethod]
    public void DisplayName_WhenSessionDisposedAfterDecode_ShouldThrowObjectDisposedException()
    {
        OutlookMailStore store = OutlookMailStoreTests.OpenSample1();
        OutlookMailFolder root = store.RootFolder;
        _ = root.DisplayName;

        store.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.DisplayName;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.Properties;
        });
    }
}
