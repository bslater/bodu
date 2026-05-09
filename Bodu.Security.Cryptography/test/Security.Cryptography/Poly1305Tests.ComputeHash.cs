// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed
    /// <see cref="Poly1305" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// producing output from cleared key material.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var poly = new Poly1305();
        poly.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = poly.ComputeHash(new byte[8]);
        });
    }
}
