// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.TryOpenPropertySet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.TryOpenPropertySet(string, out OlePropertySet)" /> throws
    /// <see cref="ObjectDisposedException" /> once the writable file has been disposed, rather than reading the staged
    /// stream.
    /// </summary>
    [TestMethod]
    public void TryOpenPropertySet_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        var file = CompoundFile.Create(new MemoryStream());
        file.RootStorage.WritePropertySet("Custom", CreatePropertySet("x"));
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.RootStorage.TryOpenPropertySet("Custom", out _);
        });
    }
}
