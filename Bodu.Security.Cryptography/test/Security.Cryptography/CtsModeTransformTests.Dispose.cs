// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtsModeTransformTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CtsModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CtsModeTransform.Dispose" /> can be invoked without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledOnce_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);
        byte[] iv = new byte[cipher.BlockSize / 8];
        var transform = new CtsModeTransform(cipher, iv);

        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="CtsModeTransform.Dispose" /> is idempotent and the second call is a no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);
        byte[] iv = new byte[cipher.BlockSize / 8];
        var transform = new CtsModeTransform(cipher, iv);

        transform.Dispose();
        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="CtsModeTransform.Transform" /> throws <see cref="ObjectDisposedException" /> after
    /// disposal, so a disposed transform cannot silently continue from its zeroed chaining state.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);
        byte[] iv = new byte[cipher.BlockSize / 8];
        var transform = new CtsModeTransform(cipher, iv);
        transform.Dispose();

        // CTS requires at least two full blocks of input.
        byte[] input = new byte[(cipher.BlockSize / 8) * 2];
        byte[] output = new byte[(cipher.BlockSize / 8) * 2];

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            transform.Transform(input, output, encrypt: true);
        });
    }
}
