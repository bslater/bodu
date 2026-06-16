// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Clear" /> disposes the algorithm and further usage throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldDisposeAndThrowOnFurtherUse()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Clear();

        byte[] buffer = new byte[1];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.ComputeHash(buffer);
        });
    }
}
