// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that reading <see cref="AsconXof{T}.AlgorithmName" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.AlgorithmName;
        });
    }
}
