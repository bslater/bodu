// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than touching the cleared key buffer or
    /// invoking <c>OnKeyChanged</c> against null state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
