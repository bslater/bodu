// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests{T,T}.GenerateIV.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.GenerateIV" /> creates a key of expected length.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenCalled_ShouldInitializeKeyCorrectly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int size = algorithm.BlockSize;
        algorithm.GenerateIV();

        Assert.HasCount(size / 8, algorithm.IV);
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.GenerateIV" /> after the algorithm has been disposed throws
    /// an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenSetAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.GenerateIV();
        });
    }
}
