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
}
