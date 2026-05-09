// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256Tests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconHashA256Tests
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="AsconHashA256" /> instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new AsconHashA256();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
