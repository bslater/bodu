// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.TryOpenStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.TryOpenStorage(string, out CompoundStorage)" /> throws
    /// <see cref="ObjectDisposedException" /> once the writable file has been disposed.
    /// </summary>
    [TestMethod]
    public void TryOpenStorage_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = CreateDisposedWritableRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.TryOpenStorage("S", out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.TryOpenStorage(string, out CompoundStorage)" /> throws
    /// <see cref="ObjectDisposedException" /> once the read-only file has been disposed.
    /// </summary>
    [TestMethod]
    public void TryOpenStorage_WhenReadOnlyFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = OpenDisposedReadOnlyRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.TryOpenStorage("Storage 1", out _);
        });
    }
}
