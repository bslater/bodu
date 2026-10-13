// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.Delete.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Delete(string)" /> throws <see cref="ObjectDisposedException" /> once
    /// the writable file has been disposed, and leaves the child in the staging tree.
    /// </summary>
    [TestMethod]
    public void Delete_WhenFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = CreateDisposedWritableRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.Delete("A");
        });

        AssertDisposedRootUnchanged(root);
    }
}
