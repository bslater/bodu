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

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="AsconHash{T}.AlgorithmName" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the stored algorithm name
    /// from a disposed instance.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new AsconHash256();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }
}
