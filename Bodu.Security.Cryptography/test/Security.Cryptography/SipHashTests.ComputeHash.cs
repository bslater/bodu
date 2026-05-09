// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale digest or accessing
    /// the cleared key buffer.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHash(new byte[8]);
        });
    }
}
