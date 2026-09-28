// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2MatrixTests
{
    /// <summary>
    /// Verifies that disposing a matrix twice returns its buffer to the pool once.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldReturnTheBufferOnce()
    {
        NativeBufferPool pool = CreatePool();
        Argon2Matrix matrix = Argon2Matrix.Rent(16, pool);

        matrix.Dispose();
        matrix.Dispose();

        Assert.AreEqual(1, pool.RetainedCount);
    }
}
