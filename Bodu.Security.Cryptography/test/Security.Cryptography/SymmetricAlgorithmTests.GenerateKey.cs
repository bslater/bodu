// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.GenerateKey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.GenerateKey" /> creates a key of expected length.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldInitializeKeyCorrectly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalKeySizes[0].MinSize;
        algorithm.KeySize = size;
        algorithm.GenerateKey();

        Assert.AreEqual(size / 8, algorithm.Key.Length);
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.GenerateKey" /> after the algorithm has been disposed throws
    /// an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.GenerateKey();
        });
    }
}
