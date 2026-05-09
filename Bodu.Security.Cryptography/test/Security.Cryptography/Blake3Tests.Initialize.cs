// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialize" /> on a disposed <see cref="Blake3" />
    /// instance throws <see cref="ObjectDisposedException" /> rather than touching cleared
    /// internal state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new Blake3();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
