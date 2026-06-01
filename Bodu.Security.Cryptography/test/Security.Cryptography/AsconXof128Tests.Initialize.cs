// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.Initialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that calling <see cref="AsconXof{T}.Initialize" /> on a disposed
    /// <see cref="AsconXof128" /> instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Initialize();
        });
    }
}
