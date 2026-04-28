// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256Tests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconHash256Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconHash256.AlgorithmName" /> returns the canonical identifier defined in NIST SP 800-232.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_ShouldReturnAsconHash256()
    {
        using var algorithm = new AsconHash256();

        Assert.AreEqual("ASCON-HASH256", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="AsconHash256.AlgorithmName" /> throws <see cref="ObjectDisposedException" /> when the instance has
    /// been disposed.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var algorithm = new AsconHash256();
        algorithm.Dispose();

        var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });

        Assert.IsNotNull(ex);
    }
}
