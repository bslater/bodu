// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.GenerateIV.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.GenerateIV" /> creates a key of expected length.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenCalled_ShouldInitializeKeyCorrectly()
    {
        using var algorithm = CreateAlgorithm();
        int size = algorithm.BlockSize;
        algorithm.GenerateIV();

        Assert.AreEqual(size / 8, algorithm.IV.Length);
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.GenerateIV" /> after the algorithm has been disposed throws
    /// an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.GenerateIV();
        });
    }
}
