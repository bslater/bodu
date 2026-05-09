// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class WhirlpoolTests
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="Whirlpool" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// touching the cleared internal state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new Whirlpool();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
