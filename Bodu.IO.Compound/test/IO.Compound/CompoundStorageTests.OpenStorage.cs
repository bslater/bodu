// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.OpenStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStorage(string)" /> throws <see cref="ObjectDisposedException" />
    /// once the writable file has been disposed.
    /// </summary>
    [TestMethod]
    public void OpenStorage_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = CreateDisposedWritableRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.OpenStorage("S");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStorage(string)" /> throws <see cref="ObjectDisposedException" />
    /// once the read-only file has been disposed.
    /// </summary>
    [TestMethod]
    public void OpenStorage_WhenReadOnlyFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = OpenDisposedReadOnlyRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.OpenStorage("Storage 1");
        });
    }
}
