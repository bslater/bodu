// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.GetHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.GetHash" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than producing output from cleared state.
    /// </summary>
    [TestMethod]
    public void GetHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.GetHash(32);
        });
    }
}
