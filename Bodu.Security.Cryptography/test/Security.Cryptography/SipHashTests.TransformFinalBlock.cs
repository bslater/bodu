// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.TransformFinalBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.TransformFinalBlock" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than producing output from cleared state.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.TransformFinalBlock(new byte[1], 0, 1);
        });
    }
}
